using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberPlan;

public sealed class CreateMemberPlanHandler(
    IMemberPlanRepository repository,
    ICurrentTenantContext tenantContext) : ICommandHandler<CreateMemberPlanCommand, MemberPlanDto>
{
    public async Task<Result<MemberPlanDto>> Handle(
        CreateMemberPlanCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberPlanDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        IReadOnlyList<Vantagem>? vantagens = null;
        if (command.Vantagens is { Count: > 0 })
        {
            try
            {
                vantagens = command.Vantagens.Select(Vantagem.Create).ToList();
            }
            catch (ArgumentException ex)
            {
                return Result<MemberPlanDto>.Fail(Error.Validation("Membership.InvalidInput", ex.Message));
            }
        }

        var trimmedName = command.Nome.Trim();
        var nameTaken = await repository.ExistsByNameAsync(trimmedName, excludingId: null, cancellationToken);

        MemberPlan plan;
        try
        {
            plan = MemberPlan.Create(
                command.Nome,
                command.Descricao,
                command.Preco,
                vantagens,
                () => nameTaken);
        }
        catch (BusinessRuleValidationException)
        {
            return Result<MemberPlanDto>.Fail(
                Error.Conflict("Membership.PlanNameConflict", "A plan with this name already exists."));
        }
        catch (ArgumentException ex)
        {
            return Result<MemberPlanDto>.Fail(Error.Validation("Membership.InvalidInput", ex.Message));
        }

        await repository.AddAsync(plan, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<MemberPlanDto>.Ok(plan.ToDto());
    }
}
