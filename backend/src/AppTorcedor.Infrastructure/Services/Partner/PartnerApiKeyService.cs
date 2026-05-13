using System.Security.Cryptography;
using System.Text;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Partner;

public sealed class PartnerApiKeyService(AppDbContext db, ILogger<PartnerApiKeyService> logger) : IPartnerApiKeyPort
{
    private const string KeyPrefix = "sk_partner_";

    public async Task<PartnerApiKeyCreatedDto> CreateAsync(string name, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        // Gera 32 bytes de entropia criptograficamente seguros e codifica em Base64Url (sem padding).
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawSuffix = Convert.ToBase64String(rawBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var plaintextKey = KeyPrefix + rawSuffix;

        var keyHash = ComputeHash(plaintextKey);
        var keyPrefixDisplay = plaintextKey[..Math.Min(12, plaintextKey.Length)];
        var now = DateTimeOffset.UtcNow;

        var record = new PartnerApiKeyRecord
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            KeyHash = keyHash,
            KeyPrefix = keyPrefixDisplay,
            IsActive = true,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
        };

        db.PartnerApiKeys.Add(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Partner API key created: Id={Id}, Name={Name}", record.Id, record.Name);

        return new PartnerApiKeyCreatedDto(record.Id, record.Name, record.KeyPrefix, plaintextKey, record.CreatedAt);
    }

    public async Task<IReadOnlyList<PartnerApiKeyListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.PartnerApiKeys
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(r => new PartnerApiKeyListItemDto(r.Id, r.Name, r.KeyPrefix, r.IsActive, r.CreatedAt, r.LastUsedAtUtc))
            .ToList();
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await db.PartnerApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (record is null)
            return false;

        record.IsActive = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Partner API key revoked: Id={Id}, Name={Name}", record.Id, record.Name);
        return true;
    }

    public async Task<ValidatedPartnerKeyDto?> ValidateAsync(string rawKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return null;

        var keyHash = ComputeHash(rawKey);
        var record = await db.PartnerApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash && x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
            return null;

        // Atualiza no mesmo fluxo para evitar corrida com DbContext scoped.
        await UpdateLastUsedAsync(record.Id, cancellationToken).ConfigureAwait(false);

        return new ValidatedPartnerKeyDto(record.Id, record.Name);
    }

    private async Task UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                await db.PartnerApiKeys
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.LastUsedAtUtc, _ => DateTimeOffset.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                await UpdateLastUsedWithTrackedEntityAsync(id, cancellationToken).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
                await UpdateLastUsedWithTrackedEntityAsync(id, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update LastUsedAtUtc for PartnerApiKey {Id}", id);
        }
    }

    private async Task UpdateLastUsedWithTrackedEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await db.PartnerApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (record is null)
            return;

        record.LastUsedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Computa SHA-256 da chave raw e retorna hex lowercase de 64 caracteres.</summary>
    internal static string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
