using Ecunexo.Billing.Api.Contracts.Catalogs;
using Ecunexo.Billing.Core.TaxCatalog.Ports;
using Ecunexo.Billing.Core.TaxRules;
using Ecunexo.Billing.Core.TaxRules.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Ecunexo.Billing.Api.Controllers;

[ApiController]
[Route("api/v1/catalogs")]
public sealed class CatalogsController(
    ITaxRateRepository taxRateRepository,
    IWithholdingRateRepository withholdingRateRepository,
    ITaxCatalogWriter taxCatalogWriter,
    ITaxRuleRepository taxRuleRepository) : ControllerBase
{
    [HttpPost("tax-rules")]
    public ActionResult<CatalogSeedResponse> CreateTaxRule([FromBody] CreateTaxRuleRequest request)
    {
        var result = taxCatalogWriter.AddSriOnlineVoidRule(new SriOnlineVoidRuleSeedItem(
            request.Description,
            request.ValidFrom,
            request.DeadlineDayOfFollowingMonth,
            request.ExtendToNextWeekday,
            request.ConsumerFinalCannotVoid,
            request.ConsumerFinalCannotCreditNote));

        return ToActionResult(result);
    }

    [HttpPost("tax-rates")]
    public ActionResult<CatalogSeedResponse> CreateTaxRate([FromBody] CreateTaxRateRequest request)
    {
        var result = taxCatalogWriter.AddTaxRate(new TaxRateSeedItem(
            request.TaxCode,
            request.RateCode,
            request.Description,
            request.Rate,
            request.ValidFrom,
            request.ValidTo));

        return ToActionResult(result);
    }

    [HttpPost("withholding-rates")]
    public ActionResult<CatalogSeedResponse> CreateWithholdingRate(
        [FromBody] CreateWithholdingRateRequest request)
    {
        var result = taxCatalogWriter.AddWithholdingRate(new WithholdingRateSeedItem(
            request.TaxType,
            request.RetentionCode,
            request.Description,
            request.Percentage,
            request.ValidFrom,
            request.ValidTo));

        return ToActionResult(result);
    }

    [HttpGet("tax-rates")]
    public ActionResult<IReadOnlyList<TaxRateResponse>> GetTaxRates(
        [FromQuery] string taxCode,
        [FromQuery] DateOnly? date,
        [FromQuery] string? rateCode = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.IsNullOrWhiteSpace(taxCode))
            return BadRequest("taxCode es obligatorio.");

        if (!string.IsNullOrWhiteSpace(rateCode))
        {
            var single = taxRateRepository.GetByRateCodes(taxCode, rateCode, targetDate);
            if (single is null)
                return Ok(Array.Empty<TaxRateResponse>());

            return Ok(new[]
            {
                new TaxRateResponse(
                    single.TaxCode,
                    single.RateCode,
                    single.Description,
                    single.Rate,
                    single.ValidFrom,
                    single.ValidTo)
            });
        }

        var rates = taxRateRepository.GetActiveByTaxCode(taxCode, targetDate)
            .Select(x => new TaxRateResponse(
                x.TaxCode,
                x.RateCode,
                x.Description,
                x.Rate,
                x.ValidFrom,
                x.ValidTo))
            .ToList();

        return Ok(rates);
    }

    [HttpGet("withholding-rates")]
    public ActionResult<IReadOnlyList<WithholdingRateResponse>> GetWithholdingRates(
        [FromQuery] string taxType,
        [FromQuery] DateOnly? date,
        [FromQuery] string? retentionCode = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (string.IsNullOrWhiteSpace(taxType))
            return BadRequest("taxType es obligatorio.");

        if (!string.IsNullOrWhiteSpace(retentionCode))
        {
            var single = withholdingRateRepository.GetByRetentionCode(taxType, retentionCode, targetDate);
            if (single is null)
                return Ok(Array.Empty<WithholdingRateResponse>());

            return Ok(new[]
            {
                new WithholdingRateResponse(
                    single.TaxType,
                    single.RetentionCode,
                    single.Description,
                    single.Percentage,
                    single.ValidFrom,
                    single.ValidTo)
            });
        }

        var rates = withholdingRateRepository.GetActiveByType(taxType, targetDate)
            .Select(x => new WithholdingRateResponse(
                x.TaxType,
                x.RetentionCode,
                x.Description,
                x.Percentage,
                x.ValidFrom,
                x.ValidTo))
            .ToList();

        return Ok(rates);
    }

    [HttpGet("tax-rules")]
    public ActionResult<IReadOnlyList<TaxRuleResponse>> GetTaxRules(
        [FromQuery] string? code = null,
        [FromQuery] DateOnly? date = null)
    {
        if (!string.IsNullOrWhiteSpace(code) && date is not null)
        {
            var active = taxRuleRepository.GetActive(code, date.Value);
            if (active is null)
                return Ok(Array.Empty<TaxRuleResponse>());

            return Ok(new[]
            {
                new TaxRuleResponse(
                    active.Code,
                    active.Description,
                    active.PayloadJson,
                    active.ValidFrom,
                    active.ValidTo),
            });
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            return Ok(taxRuleRepository.ListByCode(code)
                .Select(ToResponse)
                .ToList());
        }

        var rows = date is null
            ? taxRuleRepository.ListAll()
            : taxRuleRepository.ListAll().Where(x => x.IsActiveOn(date.Value));

        return Ok(rows.Select(ToResponse).ToList());
    }

    private static TaxRuleResponse ToResponse(TaxRule rule) =>
        new(rule.Code, rule.Description, rule.PayloadJson, rule.ValidFrom, rule.ValidTo);

    private ActionResult<CatalogSeedResponse> ToActionResult(CatalogWriteResult result)
    {
        if (result.Errors.Count > 0)
            return BadRequest(new CatalogSeedResponse(result.Created, result.Skipped, result.Errors));

        return Ok(new CatalogSeedResponse(result.Created, result.Skipped, result.Errors));
    }
}
