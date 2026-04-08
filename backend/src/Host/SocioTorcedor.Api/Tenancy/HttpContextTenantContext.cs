using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Tenancy;

public sealed class HttpContextTenantContext(IHttpContextAccessor httpContextAccessor) : ICurrentTenantContext
{
    public const string TenantContextItemKey = "TenantContext";

    private TenantContext? Context =>
        httpContextAccessor.HttpContext?.Items.TryGetValue(TenantContextItemKey, out var v) == true
            ? v as TenantContext
            : null;

    public bool IsResolved => Context is not null;

    public Guid TenantId =>
        Context?.TenantId ?? throw new InvalidOperationException("Tenant context is not resolved for this request.");

    public string TenantConnectionString =>
        Context?.ConnectionString ?? throw new InvalidOperationException("Tenant context is not resolved for this request.");
}
