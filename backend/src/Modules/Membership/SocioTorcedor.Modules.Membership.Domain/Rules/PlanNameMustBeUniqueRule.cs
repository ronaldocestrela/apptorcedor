using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Membership.Domain.Rules;

public sealed class PlanNameMustBeUniqueRule : IBusinessRule
{
    private readonly Func<bool> _nameAlreadyExists;

    public PlanNameMustBeUniqueRule(Func<bool> nameAlreadyExists)
    {
        _nameAlreadyExists = nameAlreadyExists;
    }

    public string Message => "A plan with this name already exists.";

    public bool IsBroken() => _nameAlreadyExists();
}
