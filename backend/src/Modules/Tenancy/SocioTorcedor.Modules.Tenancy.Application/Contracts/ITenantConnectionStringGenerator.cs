namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantConnectionStringGenerator
{
    string Generate(string slug);
}
