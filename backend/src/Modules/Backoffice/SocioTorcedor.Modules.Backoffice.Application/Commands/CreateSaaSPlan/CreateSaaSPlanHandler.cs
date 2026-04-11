using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;

public sealed class CreateSaaSPlanHandler(ISaaSPlanRepository repository)
    : ICommandHandler<CreateSaaSPlanCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSaaSPlanCommand command, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<(string Key, string? Description, string? Value)>? features = command.Features?
                .Select(f => (f.Key, f.Description, f.Value))
                .ToList();

            var plan = SaaSPlan.Create(
                command.Name,
                command.Description,
                command.MonthlyPrice,
                command.YearlyPrice,
                command.MaxMembers,
                command.StripePriceMonthlyId,
                command.StripePriceYearlyId,
                features);

            await repository.AddAsync(plan, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Ok(plan.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Fail(Error.Validation("SaaSPlan.Invalid", ex.Message));
        }
    }
}
