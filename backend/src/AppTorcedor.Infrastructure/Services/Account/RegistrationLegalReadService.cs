using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Account;

public sealed class RegistrationLegalReadService(AppDbContext db) : IRegistrationLegalReadPort
{
    public async Task<RegistrationLegalRequirementsDto?> GetRequiredPublishedVersionsAsync(CancellationToken cancellationToken = default)
    {
        var termsDoc = await db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Type == LegalDocumentType.TermsOfUse, cancellationToken)
            .ConfigureAwait(false);
        var privacyDoc = await db.LegalDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Type == LegalDocumentType.PrivacyPolicy, cancellationToken)
            .ConfigureAwait(false);
        if (termsDoc is null || privacyDoc is null)
            return null;

        var termsVersion = await LatestPublishedVersionAsync(termsDoc.Id, cancellationToken).ConfigureAwait(false);
        var privacyVersion = await LatestPublishedVersionAsync(privacyDoc.Id, cancellationToken).ConfigureAwait(false);
        if (termsVersion is null || privacyVersion is null)
            return null;

        return new RegistrationLegalRequirementsDto(
            termsVersion.Value.Id,
            privacyVersion.Value.Id,
            termsDoc.Title,
            privacyDoc.Title);
    }

    private async Task<(Guid Id, int Version)?> LatestPublishedVersionAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var v = await db.LegalDocumentVersions.AsNoTracking()
            .Where(x => x.LegalDocumentId == documentId && x.Status == LegalDocumentVersionStatus.Published)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return v is null ? null : (v.Id, v.VersionNumber);
    }
}
