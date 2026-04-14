using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.DigitalCard;
using AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.DigitalCards;

public sealed class DigitalCardTorcedorReadService(AppDbContext db) : IDigitalCardTorcedorPort
{
    private static readonly TimeSpan ClientCacheTtl = TimeSpan.FromMinutes(5);

    public async Task<MyDigitalCardViewDto> GetMyDigitalCardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
            return MyDigitalCardViewFactory.NoMembershipRow();

        if (membership.Status == MembershipStatus.NaoAssociado)
            return MyDigitalCardViewFactory.NotAssociated(membership.Id);

        if (membership.Status != MembershipStatus.Ativo)
            return MyDigitalCardViewFactory.InactiveMembership(membership.Status, membership.Id);

        var activeCard = await db.DigitalCards.AsNoTracking()
            .Where(c => c.MembershipId == membership.Id && c.Status == DigitalCardStatus.Active)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        var holderName = user?.Name ?? string.Empty;

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        var docMasked = DigitalCardTemplatePreview.MaskDocument(profile?.Document);

        string? planName = null;
        if (membership.PlanId is { } planId)
        {
            planName = await db.MembershipPlans.AsNoTracking()
                .Where(p => p.Id == planId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (activeCard is null)
            return MyDigitalCardViewFactory.AwaitingIssuance(membership.Id);

        return MyDigitalCardViewFactory.Active(
            membership.Id,
            activeCard.Id,
            activeCard.Version,
            activeCard.IssuedAt,
            activeCard.Token,
            holderName,
            membership.Status.ToString(),
            planName,
            docMasked,
            activeCard.Status.ToString(),
            ClientCacheTtl);
    }
}
