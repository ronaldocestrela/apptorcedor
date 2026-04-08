using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.BuildingBlocks.Domain.Tests.Abstractions;

public class BusinessRuleValidationExceptionTests
{
    private sealed class BrokenRule : IBusinessRule
    {
        public string Message => "Rule broken";

        public bool IsBroken() => true;
    }

    [Fact]
    public void Message_matches_broken_rule()
    {
        var rule = new BrokenRule();

        var ex = new BusinessRuleValidationException(rule);

        ex.Message.Should().Be("Rule broken");
        ex.BrokenRule.Should().BeSameAs(rule);
    }
}
