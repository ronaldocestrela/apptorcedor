using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Partner;

public sealed class PartnerLookupService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<PartnerLookupService> logger) : IPartnerLookupPort
{
    public async Task<PartnerLookupResultDto> LookupByPhoneAsync(string rawPhone, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePhone(rawPhone);
        if (string.IsNullOrEmpty(normalized))
            return new PartnerLookupResultDto(false, false);

        var candidatePhones = BuildCandidatePhones(normalized);
        if (candidatePhones.Count == 0)
            return new PartnerLookupResultDto(false, false);

        // Evita expressões SQL com múltiplos Replace em coluna para reduzir risco de erro de tradução/runtime.
        // Normalizamos em memória com projeção mínima para manter o endpoint estável.
        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.PhoneNumber != null)
            .Select(u => new { u.Id, u.PhoneNumber })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var matchedUserId = users
            .Where(u => u.PhoneNumber is not null)
            .Select(u => new { u.Id, NormalizedPhone = NormalizePhone(u.PhoneNumber!) })
            .FirstOrDefault(u => candidatePhones.Contains(u.NormalizedPhone))
            ?.Id;

        if (matchedUserId is null)
            return new PartnerLookupResultDto(false, false);

        var now = DateTimeOffset.UtcNow;
        bool isActiveMember;
        try
        {
            isActiveMember = await db.Memberships
                .AsNoTracking()
                .AnyAsync(
                    m => m.UserId == matchedUserId.Value
                      && m.Status == MembershipStatus.Ativo
                      && (m.EndDate == null || m.EndDate > now),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Partner lookup failed while reading membership status for user {UserId}.", matchedUserId.Value);
            throw;
        }

        return new PartnerLookupResultDto(true, isActiveMember);
    }

    private static HashSet<string> BuildCandidatePhones(string normalized)
    {
        var set = new HashSet<string>(StringComparer.Ordinal)
        {
            normalized,
            StripCountryCode(normalized),
        };

        return set.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Normaliza o número de telefone removendo todos os caracteres não numéricos.
    /// Remove o DDI 55 do Brasil se presente (número com 13 dígitos → 11 dígitos).
    /// </summary>
    internal static string NormalizePhone(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return string.Empty;

        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        return digits;
    }

    /// <summary>Remove o prefixo de país 55 (Brasil) se o número tiver 12 ou 13 dígitos.</summary>
    internal static string StripCountryCode(string digits)
    {
        if ((digits.Length == 12 || digits.Length == 13) && digits.StartsWith("55", StringComparison.Ordinal))
            return digits[2..];
        return digits;
    }
}
