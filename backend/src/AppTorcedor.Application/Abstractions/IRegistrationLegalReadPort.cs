using AppTorcedor.Application.Modules.Account;

namespace AppTorcedor.Application.Abstractions;

/// <summary>Published legal document versions required for public registration (LGPD).</summary>
public interface IRegistrationLegalReadPort
{
    /// <summary>Returns null when terms or privacy policy are not configured with a published version.</summary>
    Task<RegistrationLegalRequirementsDto?> GetRequiredPublishedVersionsAsync(CancellationToken cancellationToken = default);
}
