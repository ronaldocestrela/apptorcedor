using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Rules;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Tests.Entities;

public sealed class MemberPlanTests
{
    [Fact]
    public void Create_sets_fields_and_vantagens()
    {
        var v1 = Vantagem.Create("Brinde");
        var v2 = Vantagem.Create("Estacionamento");

        var plan = MemberPlan.Create(
            "Ouro",
            "Plano ouro",
            99.90m,
            new List<Vantagem> { v1, v2 },
            () => false);

        plan.Nome.Should().Be("Ouro");
        plan.Descricao.Should().Be("Plano ouro");
        plan.Preco.Should().Be(99.90m);
        plan.IsActive.Should().BeTrue();
        plan.Vantagens.Should().HaveCount(2);
        plan.Vantagens[0].Descricao.Should().Be("Brinde");
        plan.Vantagens[1].Descricao.Should().Be("Estacionamento");
    }

    [Fact]
    public void Create_throws_when_name_not_unique()
    {
        var act = () => MemberPlan.Create("Dup", null, 10m, null, () => true);
        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Message.Should().Contain("plan with this name");
    }

    [Fact]
    public void Create_throws_when_name_empty()
    {
        var act = () => MemberPlan.Create("  ", null, 10m, null, () => false);
        act.Should().Throw<ArgumentException>().WithParameterName("nome");
    }

    [Fact]
    public void Create_throws_when_preco_negative()
    {
        var act = () => MemberPlan.Create("X", null, -1m, null, () => false);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("preco");
    }

    [Fact]
    public void Update_changes_fields()
    {
        var plan = MemberPlan.Create("A", "d1", 10m, null, () => false);
        plan.Update("B", "d2", 20m, () => false);
        plan.Nome.Should().Be("B");
        plan.Descricao.Should().Be("d2");
        plan.Preco.Should().Be(20m);
    }

    [Fact]
    public void Update_throws_when_name_conflict()
    {
        var plan = MemberPlan.Create("A", null, 10m, null, () => false);
        var act = () => plan.Update("Taken", null, 10m, () => true);
        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void ReplaceVantagens_clears_and_sets()
    {
        var plan = MemberPlan.Create(
            "P",
            null,
            1m,
            new List<Vantagem> { Vantagem.Create("Old") },
            () => false);

        plan.ReplaceVantagens(new List<Vantagem> { Vantagem.Create("N1"), Vantagem.Create("N2") });
        plan.Vantagens.Should().HaveCount(2);
        plan.Vantagens.Select(v => v.Descricao).Should().Equal("N1", "N2");

        plan.ReplaceVantagens(null);
        plan.Vantagens.Should().BeEmpty();
    }

    [Fact]
    public void ToggleActive_flips_flag()
    {
        var plan = MemberPlan.Create("P", null, 1m, null, () => false);
        plan.IsActive.Should().BeTrue();
        plan.ToggleActive();
        plan.IsActive.Should().BeFalse();
        plan.ToggleActive();
        plan.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PlanNameMustBeUniqueRule_message()
    {
        var rule = new PlanNameMustBeUniqueRule(() => true);
        rule.IsBroken().Should().BeTrue();
        rule.Message.Should().NotBeNullOrWhiteSpace();
    }
}
