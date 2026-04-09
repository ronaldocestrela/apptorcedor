using SocioTorcedor.BuildingBlocks.Shared.Tenancy;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Support;

internal sealed class ResolvedTenant(Guid tenantId) : ICurrentTenantContext
{
    public bool IsResolved => true;

    public Guid TenantId { get; } = tenantId;

    public string TenantConnectionString => "Server=test;Database=test;Trusted_Connection=True;";
}

internal sealed class UnresolvedTenant : ICurrentTenantContext
{
    public bool IsResolved => false;

    public Guid TenantId => throw new InvalidOperationException();

    public string TenantConnectionString => throw new InvalidOperationException();
}
