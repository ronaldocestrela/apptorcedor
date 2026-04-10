using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberPlan;

public sealed class UpdateMemberPlanHandler(
    IMemberPlanRepository repository,
    ICurrentTenantContext tenantContext) : ICommandHandler<UpdateMemberPlanCommand, MemberPlanDto>
{
    public async Task<Result<MemberPlanDto>> Handle(
        UpdateMemberPlanCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberPlanDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var plan = await repository.GetTrackedByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result<MemberPlanDto>.Fail(
                Error.NotFound("Membership.PlanNotFound", "Member plan was not found."));

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
        var nameTaken = await repository.ExistsByNameAsync(trimmedName, plan.Id, cancellationToken);

        try
        {
            plan.Update(
                command.Nome,
                command.Descricao,
                command.Preco,
                () => nameTaken);
            plan.ReplaceVantagens(vantagens);
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

        await repository.SaveChangesAsync(cancellationToken);

        return Result<MemberPlanDto>.Ok(plan.ToDto());
    }
}
