using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Partner;

public sealed class PartnerLookupService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : IPartnerLookupPort
{
    public async Task<PartnerLookupResultDto> LookupByPhoneAsync(string rawPhone, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePhone(rawPhone);
        if (string.IsNullOrEmpty(normalized))
            return new PartnerLookupResultDto(false, false);

        // Busca usuário ativo com o telefone normalizado.
        // PhoneNumber é armazenado como digitado pelo usuário; normaliza nos dois lados para compatibilidade.
        var user = await userManager.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.PhoneNumber != null)
            .FirstOrDefaultAsync(
                u => u.PhoneNumber!.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("+", "") == normalized
                  || u.PhoneNumber!.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("+", "") == StripCountryCode(normalized),
                cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return new PartnerLookupResultDto(false, false);

        var now = DateTimeOffset.UtcNow;
        var isActiveMember = await db.Memberships
            .AsNoTracking()
            .AnyAsync(
                m => m.UserId == user.Id
                  && m.Status == MembershipStatus.Ativo
                  && (m.EndDate == null || m.EndDate > now),
                cancellationToken)
            .ConfigureAwait(false);

        return new PartnerLookupResultDto(true, isActiveMember);
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
