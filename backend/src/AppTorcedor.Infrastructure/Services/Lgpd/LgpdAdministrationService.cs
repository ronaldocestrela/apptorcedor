using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfraDocType = AppTorcedor.Infrastructure.Entities.LegalDocumentType;
using AppDocType = AppTorcedor.Application.Modules.Lgpd.LegalDocumentType;

namespace AppTorcedor.Infrastructure.Services.Lgpd;

public sealed class LgpdAdministrationService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IRefreshTokenStore refreshTokens) : ILgpdAdministrationPort
{
    public async Task<IReadOnlyList<LegalDocumentListItemDto>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var docs = await db.LegalDocuments.AsNoTracking().OrderBy(d => d.Type).ToListAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<LegalDocumentListItemDto>();
        foreach (var d in docs)
        {
            var published = await db.LegalDocumentVersions.AsNoTracking()
                .Where(v => v.LegalDocumentId == d.Id && v.Status == LegalDocumentVersionStatus.Published)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            result.Add(
                new LegalDocumentListItemDto(
                    d.Id,
                    ToApp(d.Type),
                    d.Title,
                    d.CreatedAt,
                    published?.VersionNumber,
                    published?.Id));
        }

        return result;
    }

    public async Task<LegalDocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var d = await db.LegalDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (d is null)
            return null;
        var versions = await db.LegalDocumentVersions.AsNoTracking()
            .Where(v => v.LegalDocumentId == id)
            .OrderBy(v => v.VersionNumber)
            .Select(v => new LegalDocumentVersionDetailDto(
                v.Id,
                v.LegalDocumentId,
                v.VersionNumber,
                v.Content,
                v.Status.ToString(),
                v.PublishedAt,
                v.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new LegalDocumentDetailDto(d.Id, ToApp(d.Type), d.Title, d.CreatedAt, versions);
    }

    public async Task<LegalDocumentDetailDto> CreateDocumentAsync(AppDocType type, string title, CancellationToken cancellationToken = default)
    {
        var infraType = ToInfra(type);
        if (await db.LegalDocuments.AnyAsync(d => d.Type == infraType, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException($"Já existe documento para o tipo {type}.");

        var entity = new LegalDocumentRecord
        {
            Id = Guid.NewGuid(),
            Type = infraType,
            Title = title.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.LegalDocuments.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (await GetDocumentAsync(entity.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<LegalDocumentVersionDetailDto> AddVersionAsync(Guid documentId, string content, CancellationToken cancellationToken = default)
    {
        var exists = await db.LegalDocuments.AnyAsync(d => d.Id == documentId, cancellationToken).ConfigureAwait(false);
        if (!exists)
            throw new InvalidOperationException("Documento não encontrado.");

        var max = await db.LegalDocumentVersions.Where(v => v.LegalDocumentId == documentId).Select(v => (int?)v.VersionNumber).MaxAsync(cancellationToken).ConfigureAwait(false) ?? 0;
        var version = new LegalDocumentVersionRecord
        {
            Id = Guid.NewGuid(),
            LegalDocumentId = documentId,
            VersionNumber = max + 1,
            Content = content,
            Status = LegalDocumentVersionStatus.Draft,
            PublishedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.LegalDocumentVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new LegalDocumentVersionDetailDto(
            version.Id,
            version.LegalDocumentId,
            version.VersionNumber,
            version.Content,
            version.Status.ToString(),
            version.PublishedAt,
            version.CreatedAt);
    }

    public async Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await db.LegalDocumentVersions.FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken).ConfigureAwait(false);
        if (version is null)
            throw new InvalidOperationException("Versão não encontrada.");

        var siblings = await db.LegalDocumentVersions.Where(v => v.LegalDocumentId == version.LegalDocumentId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        foreach (var v in siblings)
        {
            v.Status = LegalDocumentVersionStatus.Draft;
            v.PublishedAt = null;
        }

        var target = siblings.First(v => v.Id == versionId);
        target.Status = LegalDocumentVersionStatus.Published;
        target.PublishedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserConsentRowDto>> ListConsentsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.UserConsents.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Join(
                db.LegalDocumentVersions.AsNoTracking(),
                c => c.LegalDocumentVersionId,
                v => v.Id,
                (c, v) => new { c, v })
            .Join(
                db.LegalDocuments.AsNoTracking(),
                x => x.v.LegalDocumentId,
                d => d.Id,
                (x, d) => new UserConsentRowDto(
                    x.c.Id,
                    x.c.UserId,
                    x.c.LegalDocumentVersionId,
                    x.v.VersionNumber,
                    ToApp(d.Type),
                    d.Title,
                    x.c.AcceptedAt,
                    x.c.ClientIp))
            .OrderByDescending(x => x.AcceptedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RecordConsentAsync(Guid userId, Guid documentVersionId, string? clientIp, CancellationToken cancellationToken = default)
    {
        var version = await db.LegalDocumentVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == documentVersionId, cancellationToken).ConfigureAwait(false);
        if (version is null)
            throw new InvalidOperationException("Versão não encontrada.");
        if (version.Status != LegalDocumentVersionStatus.Published)
            throw new InvalidOperationException("Somente versões publicadas podem receber consentimento.");

        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Usuário não encontrado.");

        if (await db.UserConsents.AnyAsync(c => c.UserId == userId && c.LegalDocumentVersionId == documentVersionId, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Consentimento já registrado para esta versão.");

        db.UserConsents.Add(
            new UserConsentRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LegalDocumentVersionId = documentVersionId,
                AcceptedAt = DateTimeOffset.UtcNow,
                ClientIp = string.IsNullOrWhiteSpace(clientIp) ? null : clientIp.Trim(),
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PrivacyOperationResultDto> ExportUserDataAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var req = new PrivacyRequestRecord
        {
            Id = requestId,
            Kind = PrivacyRequestKind.Export,
            SubjectUserId = subjectUserId,
            RequestedByUserId = requestedByUserId,
            Status = PrivacyRequestStatus.Pending,
            CreatedAt = createdAt,
        };
        db.PrivacyRequests.Add(req);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == subjectUserId, cancellationToken).ConfigureAwait(false);
            if (user is null)
                throw new InvalidOperationException("Usuário não encontrado.");

            var consents = await ListConsentsForUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);
            var membership = await db.Memberships.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == subjectUserId, cancellationToken).ConfigureAwait(false);
            var paymentsCount = await db.Payments.CountAsync(p => p.UserId == subjectUserId, cancellationToken).ConfigureAwait(false);

            var payload = new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.Name,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt,
                consents,
                membership = membership is null
                    ? null
                    : new
                    {
                        membership.Id,
                        membership.Status,
                        membership.PlanId,
                        membership.StartDate,
                        membership.EndDate,
                        membership.NextDueDate,
                    },
                paymentsCount,
            };
            var json = JsonSerializer.Serialize(payload);

            var tracked = await db.PrivacyRequests.FirstAsync(r => r.Id == requestId, cancellationToken).ConfigureAwait(false);
            tracked.Status = PrivacyRequestStatus.Completed;
            tracked.CompletedAt = DateTimeOffset.UtcNow;
            tracked.ResultJson = json;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PrivacyOperationResultDto(requestId, PrivacyRequestKind.Export.ToString(), PrivacyRequestStatus.Completed.ToString(), json, null, createdAt, tracked.CompletedAt);
        }
        catch (Exception ex)
        {
            var tracked = await db.PrivacyRequests.FirstAsync(r => r.Id == requestId, cancellationToken).ConfigureAwait(false);
            tracked.Status = PrivacyRequestStatus.Failed;
            tracked.CompletedAt = DateTimeOffset.UtcNow;
            tracked.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new PrivacyOperationResultDto(requestId, PrivacyRequestKind.Export.ToString(), PrivacyRequestStatus.Failed.ToString(), null, ex.Message, createdAt, tracked.CompletedAt);
        }
    }

    public async Task<PrivacyOperationResultDto> AnonymizeUserAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var req = new PrivacyRequestRecord
        {
            Id = requestId,
            Kind = PrivacyRequestKind.Anonymize,
            SubjectUserId = subjectUserId,
            RequestedByUserId = requestedByUserId,
            Status = PrivacyRequestStatus.Pending,
            CreatedAt = createdAt,
        };
        db.PrivacyRequests.Add(req);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var user = await userManager.FindByIdAsync(subjectUserId.ToString()).ConfigureAwait(false);
            if (user is null)
                throw new InvalidOperationException("Usuário não encontrado.");

            var newEmail = $"anon-{subjectUserId:N}@removed.local";
            user.Email = newEmail;
            user.UserName = newEmail;
            user.NormalizedEmail = userManager.NormalizeEmail(newEmail);
            user.NormalizedUserName = userManager.NormalizeName(newEmail);
            user.Name = "Usuário removido";
            user.PhoneNumber = null;
            user.IsActive = false;

            var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!update.Succeeded)
                throw new InvalidOperationException(string.Join("; ", update.Errors.Select(e => e.Description)));

            await refreshTokens.RevokeAllForUserAsync(subjectUserId, cancellationToken).ConfigureAwait(false);

            var summary = JsonSerializer.Serialize(new { subjectUserId, anonymizedEmail = newEmail, refreshTokensRevoked = true });
            var tracked = await db.PrivacyRequests.FirstAsync(r => r.Id == requestId, cancellationToken).ConfigureAwait(false);
            tracked.Status = PrivacyRequestStatus.Completed;
            tracked.CompletedAt = DateTimeOffset.UtcNow;
            tracked.ResultJson = summary;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PrivacyOperationResultDto(requestId, PrivacyRequestKind.Anonymize.ToString(), PrivacyRequestStatus.Completed.ToString(), summary, null, createdAt, tracked.CompletedAt);
        }
        catch (Exception ex)
        {
            var tracked = await db.PrivacyRequests.FirstAsync(r => r.Id == requestId, cancellationToken).ConfigureAwait(false);
            tracked.Status = PrivacyRequestStatus.Failed;
            tracked.CompletedAt = DateTimeOffset.UtcNow;
            tracked.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new PrivacyOperationResultDto(requestId, PrivacyRequestKind.Anonymize.ToString(), PrivacyRequestStatus.Failed.ToString(), null, ex.Message, createdAt, tracked.CompletedAt);
        }
    }

    private static AppDocType ToApp(InfraDocType t) =>
        t switch
        {
            InfraDocType.TermsOfUse => AppDocType.TermsOfUse,
            InfraDocType.PrivacyPolicy => AppDocType.PrivacyPolicy,
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };

    private static InfraDocType ToInfra(AppDocType t) =>
        t switch
        {
            AppDocType.TermsOfUse => InfraDocType.TermsOfUse,
            AppDocType.PrivacyPolicy => InfraDocType.PrivacyPolicy,
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, null),
        };
}
