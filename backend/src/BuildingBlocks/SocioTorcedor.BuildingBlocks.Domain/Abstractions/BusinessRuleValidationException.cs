namespace SocioTorcedor.BuildingBlocks.Domain.Abstractions;

public sealed class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }

    public IBusinessRule BrokenRule { get; }
}
