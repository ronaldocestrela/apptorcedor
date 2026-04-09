using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;

public sealed class CreateTenantHandler(
    ITenantRepository repository,
    ITenantConnectionStringGenerator connectionStringGenerator,
    ITenantDatabaseProvisioner databaseProvisioner)
    : ICommandHandler<CreateTenantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (await repository.SlugExistsAsync(command.Slug, cancellationToken))
                return Result<Guid>.Fail(Error.Conflict("Tenant.SlugExists", "A tenant with this slug already exists."));

            var slug = command.Slug.Trim();
            var connectionString = connectionStringGenerator.Generate(slug);

            var tenant = Tenant.Create(
                command.Name.Trim(),
                slug,
                connectionString,
                slugAlreadyExists: () => false);

            await repository.AddAsync(tenant, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            try
            {
                await databaseProvisioner.ProvisionAsync(connectionString, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Fail(Error.Failure("Tenant.ProvisioningFailed", ex.Message));
            }

            return Result<Guid>.Ok(tenant.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Fail(Error.Validation("Tenant.Invalid", ex.Message));
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<Guid>.Fail(Error.Validation("Tenant.Rule", ex.Message));
        }
    }
}
