namespace SocioTorcedor.Modules.Payments.Application.DTOs;

public sealed record MemberStripeCheckoutSessionDto(string SessionId, string Url);
