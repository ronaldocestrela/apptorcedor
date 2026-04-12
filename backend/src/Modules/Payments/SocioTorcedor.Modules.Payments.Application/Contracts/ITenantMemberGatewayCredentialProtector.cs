namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Criptografia de credenciais de gateway do tenant (repouso).
/// </summary>
public interface ITenantMemberGatewayCredentialProtector
{
    string Protect(string plainJson);

    string Unprotect(string protectedPayload);
}
