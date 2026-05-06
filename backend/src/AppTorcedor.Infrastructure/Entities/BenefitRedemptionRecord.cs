using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class BenefitRedemptionRecord
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public BenefitRedemptionStatus Status { get; set; } = BenefitRedemptionStatus.Approved;

    public string? ShirtSize { get; set; }
    public string? ShirtModel { get; set; }
    public string? ShirtNumber { get; set; }
    public string? ShirtDisplayName { get; set; }

    /// <summary>8 digits, no mask.</summary>
    public string? DeliveryCep { get; set; }

    public string? DeliveryNeighborhood { get; set; }
    public string? DeliveryStreet { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryCity { get; set; }

    /// <summary>UF uppercase, 2 letters.</summary>
    public string? DeliveryState { get; set; }

    /// <summary><c>pickup</c> (retirada na loja) ou <c>carrier</c> (envio via Melhor Envio).</summary>
    public string? ShippingMethod { get; set; }

    /// <summary>Serviço retornado pela API Melhor Envio (<c>id</c> do item da cotação).</summary>
    public int? ShippingCarrierId { get; set; }

    public string? ShippingCarrierName { get; set; }

    public string? ShippingServiceName { get; set; }

    public decimal? ShippingPrice { get; set; }

    /// <summary>Prazo em dias corridos/uso do trecho máximo disponível pela API quando aplicável.</summary>
    public int? ShippingDeliveryDays { get; set; }

    /// <summary>One-off Stripe/Mock card charge for Melhor Envio freight; null for pickup.</summary>
    public Guid? ShippingPaymentId { get; set; }

    public DateTimeOffset? ShippingPaidAtUtc { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }
}
