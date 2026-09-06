namespace Ecunexo.Billing.Infrastructure.Persistence;

public sealed class BillingDatabaseBootstrapOptions
{
    public const string SectionName = "Billing:DatabaseBootstrap";

    public bool Enabled { get; set; } = true;
    public bool Migrate { get; set; } = true;
    public bool SeedCatalogs { get; set; } = true;
}
