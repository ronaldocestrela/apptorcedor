using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Domain.Rules;

public sealed class TenantSubdomainMustBeUniqueRule : IBusinessRule
{
    private readonly Func<bool> _subdomainAlreadyExists;

    public TenantSubdomainMustBeUniqueRule(Func<bool> subdomainAlreadyExists)
    {
        _subdomainAlreadyExists = subdomainAlreadyExists;
    }

    public string Message => "Subdomain is already in use.";

    public bool IsBroken() => _subdomainAlreadyExists();
}
