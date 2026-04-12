using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Infrastructure.Services;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Tests;

public sealed class StripeSubscriptionCheckoutCustomerFactoryTests
{
    [Fact]
    public void BuildCustomerCreateOptions_copies_metadata_for_member_checkout()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var profileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var planId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var meta = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantId.ToString("D"),
            ["member_profile_id"] = profileId.ToString("D"),
            ["member_plan_id"] = planId.ToString("D"),
            ["recurring_amount_brl"] = "49.90",
        };
        var request = new CreateCheckoutSessionRequest(
            PaymentProviderContext.Member,
            Mode: "subscription",
            Amount: 49.90m,
            Currency: "BRL",
            ProductName: "Plano Ouro",
            BillingInterval: "month",
            SuccessUrl: "https://app.example/success",
            CancelUrl: "https://app.example/cancel",
            Metadata: meta,
            ConnectedAccountId: null,
            CustomerEmail: null,
            IdempotencyKey: "member-checkout:test");

        var options = StripeSubscriptionCheckoutCustomerFactory.BuildCustomerCreateOptions(request);

        options.Metadata.Should().NotBeNull();
        options.Metadata.Should().BeEquivalentTo(meta);
        options.Email.Should().BeNullOrEmpty();
    }

    [Fact]
    public void BuildCustomerCreateOptions_sets_email_when_present()
    {
        var meta = new Dictionary<string, string> { ["tenant_id"] = Guid.NewGuid().ToString("D") };
        var request = new CreateCheckoutSessionRequest(
            PaymentProviderContext.Member,
            Mode: "subscription",
            Amount: 10m,
            Currency: "BRL",
            ProductName: "Plano",
            BillingInterval: "month",
            SuccessUrl: "https://app.example/success",
            CancelUrl: "https://app.example/cancel",
            Metadata: meta,
            ConnectedAccountId: null,
            CustomerEmail: "  user@example.com  ",
            IdempotencyKey: "k");

        var options = StripeSubscriptionCheckoutCustomerFactory.BuildCustomerCreateOptions(request);

        options.Email.Should().Be("user@example.com");
    }
}
