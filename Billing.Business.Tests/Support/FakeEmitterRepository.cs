using Ecunexo.Billing.Core;
using Ecunexo.Billing.Core.Documents.Ports;
using EmitterAgg = Ecunexo.Billing.Core.Emitter.Emitter;

namespace Ecunexo.Billing.Business.Tests.Support;

internal sealed class FakeEmitterRepository : IEmitterRepository
{
    private readonly Dictionary<Guid, EmitterAgg> _emitters = new();
    private int _nextSequential = 1;

    public (string Establishment, string EmissionPoint) ResolvedPoint { get; set; } = ("001", "001");
    public Exception? ResolveException { get; set; }
    public List<(Guid EmitterId, string Establishment, string EmissionPoint, string DocumentType, string Address)> EnsuredPoints { get; } = [];

    public void Add(EmitterAgg emitter) => _emitters[emitter.Id] = emitter;

    public Task<EmitterAgg?> GetAsync(Guid emitterId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_emitters.TryGetValue(emitterId, out var emitter) ? emitter : null);

    public Task<(string Establishment, string EmissionPoint)> ResolveRegisteredFacturaPointAsync(
        Guid emitterId,
        string? requestedEstablishment,
        string? requestedEmissionPoint,
        CancellationToken cancellationToken = default)
    {
        if (ResolveException is not null)
            throw ResolveException;

        return Task.FromResult(ResolvedPoint);
    }

    public Task<SequentialNumber> AllocateNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string? requestedSequential = null,
        CancellationToken cancellationToken = default)
    {
        var value = requestedSequential ?? _nextSequential++.ToString().PadLeft(9, '0');
        return Task.FromResult(SequentialNumber.Create(value));
    }

    public Task EnsureEstablishmentPointAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string address,
        CancellationToken cancellationToken = default)
    {
        EnsuredPoints.Add((emitterId, establishmentCode, emissionPoint, documentType, address));
        return Task.CompletedTask;
    }

    public Task<Guid?> FindPreferredIdByRucAsync(
        string ruc,
        Guid? tenantId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(_emitters.Values.FirstOrDefault(e => e.Ruc.Value == ruc)?.Id);

    public Task SyncIdentityAsync(
        Guid emitterId,
        string businessName,
        string mainAddress,
        string? tradeName,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Guid> AddAsync(
        EmitterAgg emitter,
        Guid? tenantId,
        string defaultEstablishmentCode,
        string defaultEmissionPoint,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task SaveCertificateAsync(EmitterAgg emitter, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string> PeekNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<string> SetNextSequentialAsync(
        Guid emitterId,
        string establishmentCode,
        string emissionPoint,
        string documentType,
        string nextSequential,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<(string Code, string Address)>> ListEstablishmentsAsync(
        Guid emitterId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task AddEstablishmentAsync(
        Guid emitterId,
        string code,
        string address,
        string emissionPoint,
        string documentType,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
