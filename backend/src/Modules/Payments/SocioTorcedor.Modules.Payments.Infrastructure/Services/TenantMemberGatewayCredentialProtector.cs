using System.Text;
using Microsoft.AspNetCore.DataProtection;
using SocioTorcedor.Modules.Payments.Application.Contracts;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

public sealed class TenantMemberGatewayCredentialProtector(IDataProtectionProvider dataProtectionProvider)
    : ITenantMemberGatewayCredentialProtector
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "SocioTorcedor.Modules.Payments.TenantMemberGateway.v1");

    public string Protect(string plainJson)
    {
        ArgumentNullException.ThrowIfNull(plainJson);
        var bytes = Encoding.UTF8.GetBytes(plainJson);
        return Convert.ToBase64String(_protector.Protect(bytes));
    }

    public string Unprotect(string protectedPayload)
    {
        ArgumentNullException.ThrowIfNull(protectedPayload);
        var bytes = _protector.Unprotect(Convert.FromBase64String(protectedPayload));
        return Encoding.UTF8.GetString(bytes);
    }
}
