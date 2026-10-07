namespace CreazioneListe.Services;

public static class TenantConfiguration
{
    public static string GetConnectionString(IConfiguration configuration, string tenant)
    {
        if (tenant != "TENANT_A" && tenant != "TENANT_B")
            throw new ArgumentException("Tenant non valido.", nameof(tenant));
        var key = $"DefaultConnection_{tenant}";
        var value = configuration.GetConnectionString(key);
        return !string.IsNullOrWhiteSpace(value) ? value
            : throw new InvalidOperationException($"Configurare ConnectionStrings:{key} tramite configurazione privata.");
    }
}
