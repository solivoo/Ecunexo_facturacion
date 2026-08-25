using System.Collections.Concurrent;
using Ecunexo.Billing.Domain;
using Ecunexo.Billing.Domain.Documents;
using Ecunexo.Billing.Domain.Documents.Ports;
using Ecunexo.Billing.Domain.Sri;
using Ecunexo.Billing.Domain.Sri.Policies;
using Ecunexo.Billing.Domain.Sri.Ports;
using Ecunexo.Billing.Infrastructure.Inventory;
using Ecunexo.Billing.Infrastructure.Sri;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecunexo.Billing.Infrastructure.Sri.Workers;

public sealed class SriOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SriOptions> options,
    SriCircuitBreaker circuitBreaker,
    ILogger<SriOutboxWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _emitterGates = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        var workers = Math.Clamp(opts.WorkerPoolSize, 1, 16);
        var tasks = Enumerable.Range(0, workers)
            .Select(_ => RunLoopAsync(stoppingToken))
            .ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                if (processed == 0)
                {
                    var delay = Math.Clamp(options.Value.OutboxPollSeconds, 1, 30);
                    await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en worker outbox SRI");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<ISriOutboxRepository>();
        var invoices = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var gateway = scope.ServiceProvider.GetRequiredService<ISriGateway>();
        var policy = scope.ServiceProvider.GetRequiredService<IOfflineEmissionPolicy>();
        var opts = options.Value;

        var batch = await outbox
            .ClaimPendingAsync(Math.Clamp(opts.OutboxBatchSize, 1, 50), cancellationToken)
            .ConfigureAwait(false);
        if (batch.Count == 0)
            return 0;

        foreach (var item in batch)
        {
            var gate = _emitterGates.GetOrAdd(
                item.EmitterId,
                _ => new SemaphoreSlim(Math.Max(1, opts.MaxConcurrentCallsPerEmitter)));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await ProcessItemAsync(
                        item,
                        outbox,
                        invoices,
                        gateway,
                        policy,
                        opts,
                        scope.ServiceProvider,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }

        return batch.Count;
    }

    private async Task ProcessItemAsync(
        SriOutboxWorkItem item,
        ISriOutboxRepository outbox,
        IInvoiceRepository invoices,
        ISriGateway gateway,
        IOfflineEmissionPolicy policy,
        SriOptions opts,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var env = Enum.TryParse<SriEnvironment>(item.Environment, true, out var parsed)
            ? parsed
            : SriEnvironment.Test;
        var endpoints = opts.ResolveEndpoints(env);
        var endpointKey = item.Operation.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            ? endpoints.AuthorizationUrl
            : endpoints.ReceptionUrl;

        if (circuitBreaker.IsOpen(endpointKey, opts))
        {
            await outbox.MarkRetryAsync(
                    item.OutboxId,
                    DateTimeOffset.UtcNow.AddSeconds(opts.CircuitBreakerBreakSeconds),
                    "Circuit breaker abierto para endpoint SRI.",
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            if (item.Operation.Equals("Reception", StringComparison.OrdinalIgnoreCase))
            {
                var signed = await invoices.GetSignedXmlAsync(item.InvoiceId, cancellationToken)
                    .ConfigureAwait(false);
                if (signed is null || signed.Length == 0)
                {
                    await outbox.MarkDeadAsync(item.OutboxId, "XML firmado no encontrado.", cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                var result = await gateway.SendReceptionAsync(env, signed, cancellationToken)
                    .ConfigureAwait(false);
                circuitBreaker.RecordSuccess(endpointKey);

                if (result.State is SriTransmissionState.Received)
                {
                    var domain = await invoices.GetDomainAsync(item.EmitterId, item.InvoiceId, cancellationToken)
                        .ConfigureAwait(false);
                    if (domain is not null)
                    {
                        domain.MarkReceived();
                        await invoices.UpdateStateAsync(item.InvoiceId, domain.State, result, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    await outbox.MarkCompletedAsync(item.OutboxId, cancellationToken).ConfigureAwait(false);
                    await outbox.EnqueueAsync(
                            item.InvoiceId,
                            item.EmitterId,
                            "Authorization",
                            env.ToString(),
                            result.AccessKey,
                            DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, opts.AuthorizationPollDelaySeconds)),
                            cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                if (result.State is SriTransmissionState.Returned)
                {
                    var pollCode = result.Messages
                        .Select(m => m.Identifier)
                        .FirstOrDefault(id => policy.ShouldPollOnly(id));

                    // 43 CLAVE ACCESO REGISTRADA / 70 → ya está en el SRI; autorizar.
                    if (pollCode is not null)
                    {
                        var accessKey = result.AccessKey ?? item.AccessKey;
                        if (string.IsNullOrWhiteSpace(accessKey))
                        {
                            var domain = await invoices.GetDomainAsync(item.EmitterId, item.InvoiceId, cancellationToken)
                                .ConfigureAwait(false);
                            accessKey = domain?.AccessKey?.Value;
                        }

                        if (string.IsNullOrWhiteSpace(accessKey))
                        {
                            await outbox.MarkDeadAsync(
                                    item.OutboxId,
                                    $"[{pollCode}] Clave registrada en SRI pero sin accessKey local.",
                                    cancellationToken)
                                .ConfigureAwait(false);
                            return;
                        }

                        await invoices.UpdateStateAsync(
                                item.InvoiceId,
                                SriDocumentState.Processing,
                                result,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        await outbox.MarkCompletedAsync(item.OutboxId, cancellationToken).ConfigureAwait(false);
                        await outbox.EnqueueAsync(
                                item.InvoiceId,
                                item.EmitterId,
                                "Authorization",
                                env.ToString(),
                                accessKey,
                                DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, opts.AuthorizationPollDelaySeconds)),
                                cancellationToken)
                            .ConfigureAwait(false);
                        logger.LogInformation(
                            "Recepción {Code} → Authorization encolada. Invoice {InvoiceId}",
                            pollCode,
                            item.InvoiceId);
                        return;
                    }

                    var code = result.Messages.FirstOrDefault()?.Identifier;
                    await invoices.UpdateStateAsync(
                            item.InvoiceId,
                            SriDocumentState.Returned,
                            result,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (!policy.CanRetryWithSameKey(SriDocumentState.Returned, code)
                        || item.AttemptCount >= opts.MaxOutboxAttempts)
                    {
                        await outbox.MarkDeadAsync(
                                item.OutboxId,
                                string.Join("; ", result.Messages.Select(m => $"[{m.Identifier}] {m.Text}")),
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await outbox.MarkRetryAsync(
                                item.OutboxId,
                                NextBackoff(opts, item.AttemptCount),
                                "DEVUELTA recuperable",
                                cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return;
                }

                // Transport / unknown
                if (item.AttemptCount >= opts.MaxOutboxAttempts)
                {
                    await outbox.MarkDeadAsync(item.OutboxId, "Agotados reintentos de recepción.", cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await outbox.MarkRetryAsync(
                            item.OutboxId,
                            NextBackoff(opts, item.AttemptCount),
                            string.Join("; ", result.Messages.Select(m => m.Text)),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return;
            }

            // Authorization
            if (string.IsNullOrWhiteSpace(item.AccessKey))
            {
                await outbox.MarkDeadAsync(item.OutboxId, "Clave de acceso ausente para autorización.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var domainInvoice = await invoices.GetDomainAsync(item.EmitterId, item.InvoiceId, cancellationToken)
                .ConfigureAwait(false);
            if (domainInvoice is null)
            {
                await outbox.MarkDeadAsync(item.OutboxId, "Factura no encontrada.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            // Desde Returned (p. ej. código 43) u otros estados previos, pasar a Processing vía persistencia.
            if (domainInvoice.State is not SriDocumentState.Processing
                and not SriDocumentState.Authorized
                and not SriDocumentState.NotAuthorized)
            {
                await invoices.UpdateStateAsync(
                        item.InvoiceId,
                        SriDocumentState.Processing,
                        null,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                domainInvoice = await invoices.GetDomainAsync(item.EmitterId, item.InvoiceId, cancellationToken)
                    .ConfigureAwait(false) ?? domainInvoice;
            }

            var auth = await gateway
                .QueryAuthorizationAsync(env, ClaveAcceso.FromExisting(item.AccessKey), cancellationToken)
                .ConfigureAwait(false);
            circuitBreaker.RecordSuccess(endpointKey);

            if (auth.State is SriTransmissionState.Authorized)
            {
                if (domainInvoice.State is SriDocumentState.Processing)
                    domainInvoice.MarkAuthorized();
                await invoices.UpdateStateAsync(
                        item.InvoiceId,
                        domainInvoice.State is SriDocumentState.Authorized
                            ? SriDocumentState.Authorized
                            : SriDocumentState.Processing,
                        auth,
                        auth.AuthorizedXml,
                        cancellationToken)
                    .ConfigureAwait(false);
                // Reload and force Authorized if still Processing after result
                if (auth.State is SriTransmissionState.Authorized)
                {
                    await invoices.UpdateStateAsync(
                            item.InvoiceId,
                            SriDocumentState.Authorized,
                            auth,
                            auth.AuthorizedXml,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                await outbox.MarkCompletedAsync(item.OutboxId, cancellationToken).ConfigureAwait(false);

                await TryNotifyInventoryEgressAsync(
                        services,
                        invoices,
                        item.EmitterId,
                        item.InvoiceId,
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (auth.State is SriTransmissionState.NotAuthorized)
            {
                await invoices.UpdateStateAsync(
                        item.InvoiceId,
                        SriDocumentState.NotAuthorized,
                        auth,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var code = auth.Messages.FirstOrDefault()?.Identifier;
                if (!policy.CanRetryWithSameKey(SriDocumentState.NotAuthorized, code)
                    || item.AttemptCount >= opts.MaxOutboxAttempts)
                {
                    await outbox.MarkDeadAsync(
                            item.OutboxId,
                            string.Join("; ", auth.Messages.Select(m => $"[{m.Identifier}] {m.Text}")),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await outbox.MarkRetryAsync(
                            item.OutboxId,
                            NextBackoff(opts, item.AttemptCount),
                            "NO AUTORIZADO recuperable",
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return;
            }

            // Processing / empty auth — seguir consultando
            await invoices.UpdateStateAsync(
                    item.InvoiceId,
                    SriDocumentState.Processing,
                    auth,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (policy.ShouldPollOnly(auth.Messages.FirstOrDefault()?.Identifier)
                || auth.State is SriTransmissionState.Processing)
            {
                if (item.AttemptCount >= opts.MaxOutboxAttempts)
                {
                    await outbox.MarkDeadAsync(item.OutboxId, "Autorización en procesamiento sin resolución.", cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await outbox.MarkRetryAsync(
                            item.OutboxId,
                            DateTimeOffset.UtcNow.AddSeconds(Math.Max(opts.AuthorizationPollDelaySeconds, 5)),
                            "EN PROCESAMIENTO — reconsultar",
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return;
            }

            if (item.AttemptCount >= opts.MaxOutboxAttempts)
            {
                await outbox.MarkDeadAsync(item.OutboxId, "Agotados reintentos de autorización.", cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await outbox.MarkRetryAsync(
                        item.OutboxId,
                        NextBackoff(opts, item.AttemptCount),
                        "Autorización pendiente de transporte",
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            circuitBreaker.RecordFailure(endpointKey, opts);
            logger.LogError(ex, "Fallo outbox {OutboxId} op {Operation}", item.OutboxId, item.Operation);
            if (item.AttemptCount >= opts.MaxOutboxAttempts)
            {
                await outbox.MarkDeadAsync(item.OutboxId, ex.Message, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await outbox.MarkRetryAsync(
                        item.OutboxId,
                        NextBackoff(opts, item.AttemptCount),
                        ex.Message,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task TryNotifyInventoryEgressAsync(
        IServiceProvider services,
        IInvoiceRepository invoices,
        Guid emitterId,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var notifier = services.GetService<IInventoryEgressNotifier>();
            if (notifier is null)
                return;

            var loaded = await invoices
                .GetDomainWithTenantAsync(emitterId, invoiceId, cancellationToken)
                .ConfigureAwait(false);
            if (loaded is null)
            {
                logger.LogWarning(
                    "No se pudo cargar factura {InvoiceId} para egreso inventario",
                    invoiceId);
                return;
            }

            var (document, tenantId) = loaded.Value;
            if (tenantId is null || tenantId == Guid.Empty)
            {
                logger.LogWarning(
                    "Factura {InvoiceId} sin TenantId; se omite egreso inventario",
                    invoiceId);
                return;
            }

            // Solo facturas (01); notas de crédito quedan fuera de este corte.
            if (document.DocumentType.Value != DocumentTypeCode.Factura.Value)
                return;

            var lines = document.Lines
                .Where(l => l.CatalogItemId is not null)
                .Select(l => new InventoryEgressLineDto(
                    l.CatalogItemId!.Value,
                    l.Quantity,
                    l.ItemKind,
                    l.Description))
                .ToList();

            if (lines.Count == 0)
                return;

            await notifier
                .NotifyAuthorizedAsync(tenantId.Value, invoiceId, lines, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Fallo no controlado al notificar egreso de {InvoiceId}", invoiceId);
        }
    }

    private static DateTimeOffset NextBackoff(SriOptions opts, int attemptCount)
    {
        var schedule = opts.RetryBackoffSeconds is { Length: > 0 }
            ? opts.RetryBackoffSeconds
            : [5, 15, 45, 120, 300];
        var idx = Math.Clamp(attemptCount - 1, 0, schedule.Length - 1);
        return DateTimeOffset.UtcNow.AddSeconds(schedule[idx]);
    }
}
