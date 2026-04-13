using System.Security.Cryptography;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.DigitalCards;

public sealed class DigitalCardAdministrationService(AppDbContext db) : IDigitalCardAdministrationPort
{
    private const int MaxReasonLength = 2000;

    public async Task<AdminDigitalCardListPageDto> ListDigitalCardsAsync(
        Guid? userId,
        Guid? membershipId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        DigitalCardStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<DigitalCardStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var query =
            from c in db.DigitalCards.AsNoTracking()
            join u in db.Users.AsNoTracking() on c.UserId equals u.Id
            join m in db.Memberships.AsNoTracking() on c.MembershipId equals m.Id
            select new { c, u, m };

        if (userId is { } uid)
            query = query.Where(x => x.c.UserId == uid);
        if (membershipId is { } mid)
            query = query.Where(x => x.c.MembershipId == mid);
        if (statusFilter is { } sf)
            query = query.Where(x => x.c.Status == sf);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.c.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(x => new AdminDigitalCardListItemDto(
                x.c.Id,
                x.c.UserId,
                x.c.MembershipId,
                x.c.Version,
                x.c.Status.ToString(),
                x.c.IssuedAt,
                x.c.InvalidatedAt,
                x.u.Email ?? string.Empty,
                x.m.Status.ToString()))
            .ToList();

        return new AdminDigitalCardListPageDto(total, items);
    }

    public async Task<AdminDigitalCardDetailDto?> GetDigitalCardByIdAsync(Guid digitalCardId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from c in db.DigitalCards.AsNoTracking()
                join u in db.Users.AsNoTracking() on c.UserId equals u.Id
                join m in db.Memberships.AsNoTracking() on c.MembershipId equals m.Id
                join p in db.MembershipPlans.AsNoTracking() on m.PlanId equals p.Id into planJoin
                from p in planJoin.DefaultIfEmpty()
                join prof in db.UserProfiles.AsNoTracking() on c.UserId equals prof.UserId into profJoin
                from prof in profJoin.DefaultIfEmpty()
                where c.Id == digitalCardId
                select new { c, u, m, p, prof })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return null;

        var planName = row.p?.Name;
        var docMasked = MaskDocument(row.prof?.Document);

        var preview = BuildTemplatePreviewLines(
            row.u.Name,
            row.c.Version,
            row.m.Status.ToString(),
            planName,
            docMasked,
            row.c.Status.ToString());

        return new AdminDigitalCardDetailDto(
            row.c.Id,
            row.c.UserId,
            row.c.MembershipId,
            row.c.Version,
            row.c.Status.ToString(),
            row.c.Token,
            row.c.IssuedAt,
            row.c.InvalidatedAt,
            row.c.InvalidationReason,
            row.u.Email ?? string.Empty,
            row.u.Name,
            row.m.Status.ToString(),
            row.m.PlanId,
            planName,
            docMasked,
            preview);
    }

    public async Task<DigitalCardMutationResult> IssueDigitalCardAsync(
        Guid membershipId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        _ = actorUserId;
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.NotFound);

        if (membership.Status != MembershipStatus.Ativo)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.MembershipNotEligible);

        var hasActive = await db.DigitalCards.AnyAsync(
                c => c.MembershipId == membershipId && c.Status == DigitalCardStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (hasActive)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.AlreadyHasActiveCard);

        var nextVersion = await NextVersionAsync(membershipId, cancellationToken).ConfigureAwait(false);
        var utc = DateTimeOffset.UtcNow;

        db.DigitalCards.Add(
            new DigitalCardRecord
            {
                Id = Guid.NewGuid(),
                UserId = membership.UserId,
                MembershipId = membership.Id,
                Version = nextVersion,
                Status = DigitalCardStatus.Active,
                Token = GenerateOpaqueToken(),
                IssuedAt = utc,
                InvalidatedAt = null,
                InvalidationReason = null,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new DigitalCardMutationResult(true, null);
    }

    public async Task<DigitalCardMutationResult> RegenerateDigitalCardAsync(
        Guid digitalCardId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        _ = actorUserId;
        var current = await db.DigitalCards.FirstOrDefaultAsync(c => c.Id == digitalCardId, cancellationToken).ConfigureAwait(false);
        if (current is null)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.NotFound);
        if (current.Status != DigitalCardStatus.Active)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.InvalidTransition);

        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == current.MembershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.NotFound);
        if (membership.Status != MembershipStatus.Ativo)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.MembershipNotEligible);

        var utc = DateTimeOffset.UtcNow;
        var resolvedReason = string.IsNullOrWhiteSpace(reason) ? "Regeneração administrativa" : reason.Trim();

        current.Status = DigitalCardStatus.Invalidated;
        current.InvalidatedAt = utc;
        current.InvalidationReason = resolvedReason;

        var nextVersion = await NextVersionAsync(current.MembershipId, cancellationToken).ConfigureAwait(false);
        db.DigitalCards.Add(
            new DigitalCardRecord
            {
                Id = Guid.NewGuid(),
                UserId = current.UserId,
                MembershipId = current.MembershipId,
                Version = nextVersion,
                Status = DigitalCardStatus.Active,
                Token = GenerateOpaqueToken(),
                IssuedAt = utc,
                InvalidatedAt = null,
                InvalidationReason = null,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new DigitalCardMutationResult(true, null);
    }

    public async Task<DigitalCardMutationResult> InvalidateDigitalCardAsync(
        Guid digitalCardId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        _ = actorUserId;
        if (string.IsNullOrWhiteSpace(reason))
            return new DigitalCardMutationResult(false, DigitalCardMutationError.ReasonRequired);
        var trimmed = reason.Trim();
        if (trimmed.Length > MaxReasonLength)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.ReasonTooLong);

        var current = await db.DigitalCards.FirstOrDefaultAsync(c => c.Id == digitalCardId, cancellationToken).ConfigureAwait(false);
        if (current is null)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.NotFound);
        if (current.Status != DigitalCardStatus.Active)
            return new DigitalCardMutationResult(false, DigitalCardMutationError.InvalidTransition);

        var utc = DateTimeOffset.UtcNow;
        current.Status = DigitalCardStatus.Invalidated;
        current.InvalidatedAt = utc;
        current.InvalidationReason = trimmed;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new DigitalCardMutationResult(true, null);
    }

    private async Task<int> NextVersionAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        var max = await db.DigitalCards.AsNoTracking()
            .Where(c => c.MembershipId == membershipId)
            .Select(c => (int?)c.Version)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);
        return (max ?? 0) + 1;
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToHexString(bytes);
    }

    private static string? MaskDocument(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return null;
        var d = document.Trim();
        return d.Length <= 4 ? "****" : $"***{d[^4..]}";
    }

    private static IReadOnlyList<string> BuildTemplatePreviewLines(
        string holderName,
        int version,
        string membershipStatus,
        string? planName,
        string? documentMasked,
        string cardStatus)
    {
        var plan = string.IsNullOrWhiteSpace(planName) ? "Sem plano" : planName;
        var doc = documentMasked ?? "(documento não informado)";
        return
        [
            "Carteirinha digital — layout fixo (B.7)",
            $"Titular: {holderName}",
            $"Versão: {version}",
            $"Status da associação: {membershipStatus}",
            $"Plano: {plan}",
            $"Documento (mascarado): {doc}",
            $"Status da emissão: {cardStatus}",
        ];
    }
}
