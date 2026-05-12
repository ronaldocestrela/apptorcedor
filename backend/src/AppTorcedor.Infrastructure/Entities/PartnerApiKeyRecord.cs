namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Chave de API para acesso de parceiros externos ao endpoint de lookup de sócio.</summary>
public sealed class PartnerApiKeyRecord
{
    public Guid Id { get; set; }

    /// <summary>Nome descritivo do parceiro (ex.: "Loja XYZ").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>SHA-256 da chave em hex lowercase (64 caracteres). Nunca armazena a chave em texto claro.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Primeiros 12 caracteres da chave raw para exibição mascarada na UI.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    /// <summary>Atualizado de forma fire-and-forget na validação; permite auditoria de uso.</summary>
    public DateTimeOffset? LastUsedAtUtc { get; set; }
}
