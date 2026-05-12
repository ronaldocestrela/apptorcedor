namespace AppTorcedor.Application.Abstractions;

/// <summary>DTO retornado somente na criação da API key — única vez que a chave em texto claro é exposta.</summary>
public sealed record PartnerApiKeyCreatedDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    string PlaintextKey,
    DateTimeOffset CreatedAt);

/// <summary>DTO para listagem — sem hash, sem chave em texto claro.</summary>
public sealed record PartnerApiKeyListItemDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAtUtc);

/// <summary>Representa uma API key válida e ativa para uso no handler de autenticação.</summary>
public sealed record ValidatedPartnerKeyDto(Guid Id, string Name);

/// <summary>Resultado do lookup de sócio por telefone.</summary>
public sealed record PartnerLookupResultDto(bool Exists, bool IsActiveMember);

/// <summary>Port de gerenciamento de API Keys de parceiros.</summary>
public interface IPartnerApiKeyPort
{
    /// <summary>Cria uma nova API key para o parceiro. Retorna a chave em texto claro — armazene-a agora.</summary>
    Task<PartnerApiKeyCreatedDto> CreateAsync(string name, Guid? createdByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartnerApiKeyListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Revoga (soft-delete) a key pelo Id. Retorna false se não encontrada.</summary>
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Valida uma chave raw (texto claro) e retorna o parceiro se ativa; null caso contrário.</summary>
    Task<ValidatedPartnerKeyDto?> ValidateAsync(string rawKey, CancellationToken cancellationToken = default);
}

/// <summary>Port de consulta de sócio por número de telefone, para uso de parceiros autenticados.</summary>
public interface IPartnerLookupPort
{
    /// <summary>
    /// Verifica se existe usuário com o telefone informado e se possui associação ativa.
    /// Nunca expõe dados pessoais além do resultado booleano (LGPD).
    /// </summary>
    Task<PartnerLookupResultDto> LookupByPhoneAsync(string rawPhone, CancellationToken cancellationToken = default);
}
