using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Domain.Rules;

public sealed class TenantSlugMustBeUniqueRule : IBusinessRule
{
    private readonly Func<bool> _slugAlreadyExists;

    public TenantSlugMustBeUniqueRule(Func<bool> slugAlreadyExists)
    {
        _slugAlreadyExists = slugAlreadyExists;
    }

    public string Message => "Tenant slug is already in use.";

    public bool IsBroken() => _slugAlreadyExists();
}
