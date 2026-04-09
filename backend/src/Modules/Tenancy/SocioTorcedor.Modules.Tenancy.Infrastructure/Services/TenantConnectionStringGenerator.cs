using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

public sealed class TenantConnectionStringGenerator(IConfiguration configuration) : ITenantConnectionStringGenerator
{
    public string Generate(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));

        var master = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'MasterDb' is not configured.");

        var builder = new SqlConnectionStringBuilder(master)
        {
            InitialCatalog = $"SocioTorcedor_Tenant_{slug.Trim()}"
        };

        return builder.ConnectionString;
    }
}
