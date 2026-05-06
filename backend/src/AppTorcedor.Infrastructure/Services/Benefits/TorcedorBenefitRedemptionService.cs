using System.Linq;
using System.Text.RegularExpressions;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class TorcedorBenefitRedemptionService(AppDbContext db) : ITorcedorBenefitRedemptionPort
{
    private static readonly Regex s_shirtNumberRegex = new("^(?:[0-9]|[1-9][0-9])$", RegexOptions.Compiled);
    private static readonly Regex s_shirtNameRegex = new("^[\\p{L}0-9'\\- ]{1,10}$", RegexOptions.Compiled);
    private static readonly Regex s_ufRegex = new("^[A-Z]{2}$", RegexOptions.Compiled);

    public async Task<TorcedorRedemptionResult> RedeemOfferAsync(
        Guid offerId,
        Guid userId,
        TorcedorShirtRedemptionRequest? shirt,
        CancellationToken cancellationToken = default)
    {
        var row = await (
                from o in db.BenefitOffers.AsNoTracking()
                join p in db.BenefitPartners.AsNoTracking() on o.PartnerId equals p.Id
                where o.Id == offerId
                select new { Offer = o, Partner = p })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotFound);

        if (!row.Partner.IsActive)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        var now = DateTimeOffset.UtcNow;
        if (!row.Offer.IsActive || now < row.Offer.StartAt || now > row.Offer.EndAt)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        var userExists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (!userExists)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotFound);

        var planRows = await db.BenefitOfferPlanEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.PlanId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statusRows = await db.BenefitOfferMembershipStatusEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var membership = await db.Memberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        var snapshot = membership is null
            ? null
            : new MembershipRecordSnapshot(membership.PlanId, membership.Status);

        if (!BenefitOfferEligibility.MatchesPlanAndStatus(planRows, statusRows, snapshot))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        if (await HasBlockingRedemptionAsync(offerId, userId, cancellationToken).ConfigureAwait(false))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.AlreadyRedeemed);

        if (row.Offer.IsShirtCustomizationOffer)
        {
            if (shirt is null)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

            var validation = await ValidateShirtPayloadAsync(offerId, shirt, cancellationToken).ConfigureAwait(false);
            if (validation is not null)
                return validation;

            var redemptionId = Guid.NewGuid();
            var method = (shirt.ShippingMethod ?? "").Trim().ToLowerInvariant();
            NormalizedDelivery? d = null;
            if (method == TorcedorBenefitShippingMethods.Carrier)
                d = NormalizeDelivery(shirt);

            db.BenefitRedemptions.Add(
                new BenefitRedemptionRecord
                {
                    Id = redemptionId,
                    OfferId = offerId,
                    UserId = userId,
                    ActorUserId = null,
                    Notes = null,
                    CreatedAt = now,
                    Status = BenefitRedemptionStatus.Pending,
                    ShirtSize = shirt.ShirtSize.Trim(),
                    ShirtModel = shirt.ShirtModel.Trim(),
                    ShirtNumber = shirt.ShirtNumber.Trim(),
                    ShirtDisplayName = shirt.ShirtDisplayName.Trim(),
                    DeliveryCep = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.Cep,
                    DeliveryNeighborhood = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.Neighborhood,
                    DeliveryStreet = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.Street,
                    DeliveryNumber = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.Number,
                    DeliveryCity = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.City,
                    DeliveryState = method == TorcedorBenefitShippingMethods.Pickup ? null : d!.State,
                    ShippingMethod = method,
                    ShippingCarrierId =
                        method == TorcedorBenefitShippingMethods.Pickup ? null : shirt.ShippingCarrierId,
                    ShippingCarrierName =
                        method == TorcedorBenefitShippingMethods.Pickup
                            ? null
                            : (shirt.ShippingCarrierName ?? "").Trim(),
                    ShippingServiceName =
                        method == TorcedorBenefitShippingMethods.Pickup
                            ? null
                            : (shirt.ShippingServiceName ?? "").Trim(),
                    ShippingPrice =
                        method == TorcedorBenefitShippingMethods.Pickup ? null : shirt.ShippingPrice,
                    ShippingDeliveryDays =
                        method == TorcedorBenefitShippingMethods.Pickup ? null : shirt.ShippingDeliveryDays,
                });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return TorcedorRedemptionResult.Success(redemptionId);
        }

        if (shirt is not null && HasAnyShirtOrDeliveryFields(shirt))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        var instantId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = instantId,
                OfferId = offerId,
                UserId = userId,
                ActorUserId = null,
                Notes = null,
                CreatedAt = now,
                Status = BenefitRedemptionStatus.Approved,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TorcedorRedemptionResult.Success(instantId);
    }

    private static bool HasAnyShirtOrDeliveryFields(TorcedorShirtRedemptionRequest shirt) =>
        !string.IsNullOrWhiteSpace(shirt.ShirtSize)
        || !string.IsNullOrWhiteSpace(shirt.ShirtModel)
        || !string.IsNullOrWhiteSpace(shirt.ShirtNumber)
        || !string.IsNullOrWhiteSpace(shirt.ShirtDisplayName)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryCep)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryNeighborhood)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryStreet)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryNumber)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryCity)
        || !string.IsNullOrWhiteSpace(shirt.DeliveryState)
        || !string.IsNullOrWhiteSpace(shirt.ShippingMethod)
        || shirt.ShippingCarrierId is not null
        || !string.IsNullOrWhiteSpace(shirt.ShippingCarrierName)
        || !string.IsNullOrWhiteSpace(shirt.ShippingServiceName)
        || shirt.ShippingPrice is not null
        || shirt.ShippingDeliveryDays is not null;

    private Task<bool> HasBlockingRedemptionAsync(
        Guid offerId,
        Guid userId,
        CancellationToken cancellationToken) =>
        db.BenefitRedemptions.AsNoTracking()
            .AnyAsync(
                r => r.OfferId == offerId
                     && r.UserId == userId
                     && (r.Status == BenefitRedemptionStatus.Pending || r.Status == BenefitRedemptionStatus.Approved),
                cancellationToken);

    private sealed record NormalizedDelivery(
        string Cep,
        string Neighborhood,
        string Street,
        string Number,
        string City,
        string State);

    private static NormalizedDelivery NormalizeDelivery(TorcedorShirtRedemptionRequest shirt)
    {
        static string T(string? s) => (s ?? "").Trim();
        var cepDigits = new string(T(shirt.DeliveryCep).Where(char.IsDigit).ToArray());
        return new NormalizedDelivery(
            cepDigits,
            T(shirt.DeliveryNeighborhood),
            T(shirt.DeliveryStreet),
            T(shirt.DeliveryNumber),
            T(shirt.DeliveryCity),
            T(shirt.DeliveryState).ToUpperInvariant());
    }

    private async Task<TorcedorRedemptionResult?> ValidateShirtPayloadAsync(
        Guid offerId,
        TorcedorShirtRedemptionRequest shirt,
        CancellationToken cancellationToken)
    {
        var size = shirt.ShirtSize?.Trim() ?? "";
        var model = shirt.ShirtModel?.Trim() ?? "";
        var number = shirt.ShirtNumber?.Trim() ?? "";
        var displayName = shirt.ShirtDisplayName?.Trim() ?? "";

        if (string.IsNullOrEmpty(size) || string.IsNullOrEmpty(model) || string.IsNullOrEmpty(number) || string.IsNullOrEmpty(displayName))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        if (!s_shirtNumberRegex.IsMatch(number) || !s_shirtNameRegex.IsMatch(displayName))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        var method = (shirt.ShippingMethod ?? "").Trim().ToLowerInvariant();
        if (method is not TorcedorBenefitShippingMethods.Pickup and not TorcedorBenefitShippingMethods.Carrier)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        if (method == TorcedorBenefitShippingMethods.Carrier)
        {
            var d = NormalizeDelivery(shirt);
            if (d.Cep.Length != 8 || !d.Cep.All(char.IsDigit))
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (string.IsNullOrEmpty(d.Neighborhood) || d.Neighborhood.Length > 120)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (string.IsNullOrEmpty(d.Street) || d.Street.Length > 200)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (string.IsNullOrEmpty(d.Number) || d.Number.Length > 20)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (string.IsNullOrEmpty(d.City) || d.City.Length > 120)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (!s_ufRegex.IsMatch(d.State))
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

            if (shirt.ShippingCarrierId is not { } cid || cid <= 0)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            var carrierName = shirt.ShippingCarrierName?.Trim() ?? "";
            var serviceName = shirt.ShippingServiceName?.Trim() ?? "";
            if (string.IsNullOrEmpty(carrierName) || carrierName.Length > 80)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (string.IsNullOrEmpty(serviceName) || serviceName.Length > 80)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (shirt.ShippingPrice is not { } price || price < 0m)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
            if (shirt.ShippingDeliveryDays is not { } days || days < 0)
                return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);
        }

        var allowedSizes = await db.BenefitShirtCatalogOptions.AsNoTracking()
            .Where(x => x.OfferId == offerId && x.Kind == BenefitShirtCatalogOptionKind.Size)
            .Select(x => x.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var allowedModels = await db.BenefitShirtCatalogOptions.AsNoTracking()
            .Where(x => x.OfferId == offerId && x.Kind == BenefitShirtCatalogOptionKind.Model)
            .Select(x => x.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (allowedSizes.Count == 0 || allowedModels.Count == 0)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        if (!allowedSizes.Contains(size) || !allowedModels.Contains(model))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.Validation);

        return null;
    }
}
