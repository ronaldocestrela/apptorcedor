using System.Security.Cryptography;
using System.Text;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services;

public sealed class RefreshTokenStore(AppDbContext db) : IRefreshTokenStore
{
    public async Task<string> CreateAsync(Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var plain = GenerateSecureToken();
        var hash = Hash(plain);
        var now = DateTimeOffset.UtcNow;
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime),
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plain;
    }

    public async Task<(Guid UserId, string NewPlainRefreshToken)?> RotateAsync(
        string plainRefreshToken,
        TimeSpan newLifetime,
        CancellationToken cancellationToken = default)
    {
        var hash = Hash(plainRefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        existing.RevokedAt = DateTimeOffset.UtcNow;

        var newPlain = GenerateSecureToken();
        var newHash = Hash(newPlain);
        var now = DateTimeOffset.UtcNow;
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = newHash,
            CreatedAt = now,
            ExpiresAt = now.Add(newLifetime),
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (existing.UserId, newPlain);
    }

    public async Task RevokeAsync(string plainRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(plainRefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || existing.RevokedAt is not null)
            return;
        existing.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationUser?> FindUserByRefreshTokenAsync(
        string plainRefreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = Hash(plainRefreshToken);
        var existing = await db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == existing.UserId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var t in tokens)
            t.RevokedAt = now;
        if (tokens.Count > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToBase64String(bytes);
    }
}
