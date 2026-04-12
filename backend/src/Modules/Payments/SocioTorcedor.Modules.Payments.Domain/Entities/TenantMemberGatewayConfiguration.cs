using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

/// <summary>
/// Configuração de gateway para cobrança de sócios (sem Stripe Connect): provedor escolhido no backoffice e credenciais informadas pelo admin do tenant.
/// </summary>
public sealed class TenantMemberGatewayConfiguration : AggregateRoot
{
    private TenantMemberGatewayConfiguration()
    {
    }

    public Guid TenantId { get; private set; }

    /// <summary>Definido pelo backoffice da plataforma.</summary>
    public MemberPaymentProviderKind SelectedProvider { get; private set; }

    /// <summary>Payload criptografado (JSON com segredos por provedor).</summary>
    public string ProtectedCredentials { get; private set; } = string.Empty;

    public MemberGatewayConfigurationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TenantMemberGatewayConfiguration Create(Guid tenantId, MemberPaymentProviderKind selectedProvider)
    {
        var now = DateTime.UtcNow;
        return new TenantMemberGatewayConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SelectedProvider = selectedProvider,
            ProtectedCredentials = string.Empty,
            Status = MemberGatewayConfigurationStatus.NotConfigured,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void SetSelectedProvider(MemberPaymentProviderKind provider)
    {
        SelectedProvider = provider;
        if (provider == MemberPaymentProviderKind.None)
        {
            ProtectedCredentials = string.Empty;
            Status = MemberGatewayConfigurationStatus.NotConfigured;
        }
        else
        {
            ProtectedCredentials = string.Empty;
            Status = MemberGatewayConfigurationStatus.NotConfigured;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetProtectedCredentials(string protectedJson, MemberGatewayConfigurationStatus status)
    {
        ProtectedCredentials = protectedJson ?? string.Empty;
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void TouchUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
