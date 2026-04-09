using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantDomain;

public sealed class AddTenantDomainHandler(
    ITenantRepository repository,
    ITenantSlugCacheInvalidator cacheInvalidator)
    : ICommandHandler<AddTenantDomainCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddTenantDomainCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result<Guid>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        var normalized = command.Origin.Trim().TrimEnd('/');
        if (tenant.Domains.Any(d =>
                string.Equals(d.Origin.TrimEnd('/'), normalized, StringComparison.OrdinalIgnoreCase)))
            return Result<Guid>.Fail(Error.Conflict("Tenant.DomainExists", "This allowed origin already exists for the tenant."));

        var existingIds = tenant.Domains.Select(d => d.Id).ToHashSet();
        tenant.AddAllowedOrigin(command.Origin.Trim());
        await repository.SaveChangesAsync(cancellationToken);
        cacheInvalidator.Invalidate(tenant.Slug);

        var added = tenant.Domains.First(d => !existingIds.Contains(d.Id));
        return Result<Guid>.Ok(added.Id);
    }
}
