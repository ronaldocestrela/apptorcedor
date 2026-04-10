using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;
using SocioTorcedor.Modules.Identity.Domain.Enums;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Repositories;

public sealed class LegalDocumentRepository(TenantIdentityDbContext db) : ILegalDocumentRepository
{
    public async Task<CurrentLegalDocumentsDto?> GetCurrentDocumentsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.LegalDocumentVersions.AsNoTracking()
            .Where(x => x.IsCurrent)
            .ToListAsync(cancellationToken);

        var terms = rows.SingleOrDefault(x => x.Kind == LegalDocumentKind.TermsOfUse);
        var privacy = rows.SingleOrDefault(x => x.Kind == LegalDocumentKind.PrivacyPolicy);
        if (terms is null || privacy is null)
            return null;

        return new CurrentLegalDocumentsDto(ToDto(terms), ToDto(privacy));
    }

    public async Task<Result> ValidateRegistrationAcceptancesAsync(
        Guid termsDocumentId,
        Guid privacyDocumentId,
        CancellationToken cancellationToken)
    {
        var terms = await db.LegalDocumentVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == termsDocumentId, cancellationToken);
        var privacy = await db.LegalDocumentVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == privacyDocumentId, cancellationToken);

        if (terms is null || !terms.IsCurrent || terms.Kind != LegalDocumentKind.TermsOfUse)
            return Result.Fail(
                Error.Validation(
                    "Identity.LegalAcceptanceInvalid",
                    "The accepted terms of use version is not the current published version."));

        if (privacy is null || !privacy.IsCurrent || privacy.Kind != LegalDocumentKind.PrivacyPolicy)
            return Result.Fail(
                Error.Validation(
                    "Identity.LegalAcceptanceInvalid",
                    "The accepted privacy policy version is not the current published version."));

        return Result.Ok();
    }

    public async Task PublishNewVersionAsync(LegalDocumentKind kind, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        var trimmed = content.Trim();
        var maxVersion = await db.LegalDocumentVersions.AsNoTracking()
            .Where(x => x.Kind == kind)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        await db.LegalDocumentVersions
            .Where(x => x.Kind == kind && x.IsCurrent)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsCurrent, false),
                cancellationToken);

        db.LegalDocumentVersions.Add(
            new LegalDocumentVersion
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                VersionNumber = maxVersion + 1,
                Content = trimmed,
                PublishedAtUtc = DateTime.UtcNow,
                IsCurrent = true
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveUserConsentsAsync(
        string userId,
        Guid termsDocumentId,
        Guid privacyDocumentId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var utc = DateTime.UtcNow;
        db.UserLegalConsents.AddRange(
            new UserLegalConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = LegalDocumentKind.TermsOfUse,
                LegalDocumentVersionId = termsDocumentId,
                AcceptedAtUtc = utc,
                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            new UserLegalConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = LegalDocumentKind.PrivacyPolicy,
                LegalDocumentVersionId = privacyDocumentId,
                AcceptedAtUtc = utc,
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static LegalDocumentVersionDto ToDto(LegalDocumentVersion v) =>
        new(v.Id, v.Kind, v.VersionNumber, v.Content, v.PublishedAtUtc);
}
