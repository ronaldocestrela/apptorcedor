using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Membership.Domain.Rules;

public sealed class CpfMustBeUniqueRule : IBusinessRule
{
    private readonly Func<bool> _cpfAlreadyExists;

    public CpfMustBeUniqueRule(Func<bool> cpfAlreadyExists)
    {
        _cpfAlreadyExists = cpfAlreadyExists;
    }

    public string Message => "This CPF is already registered for another member.";

    public bool IsBroken() => _cpfAlreadyExists();
}
