using System.Net;
using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using SocioTorcedor.Modules.Payments.Infrastructure.Services;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Tests;

public sealed class StripePaymentProviderCancelTests
{
    private static StripePaymentOperations CreateOperations(string stripeSecretKey = "sk_test_fake_for_unit_tests")
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new PaymentsOptions { StripeSecretKey = stripeSecretKey });
        var provider = new StripePaymentProvider(options);
        return provider.Operations;
    }

    [Fact]
    public async Task CancelAsync_with_empty_id_does_not_throw()
    {
        var sut = CreateOperations();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.Member,
            string.Empty,
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelAsync_with_whitespace_id_does_not_throw()
    {
        var sut = CreateOperations();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.Member,
            "   ",
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_false_for_null_or_whitespace()
    {
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel(null).Should().BeFalse();
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel("").Should().BeFalse();
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel(" \t ").Should().BeFalse();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_false_for_ids_not_starting_with_sub_prefix()
    {
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel("inv_abc").Should().BeFalse();
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel("pi_xyz").Should().BeFalse();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_true_for_sub_prefix()
    {
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel("sub_123").Should().BeTrue();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_trims_whitespace_before_sub_prefix_check()
    {
        StripePaymentOperations.ShouldInvokeStripeSubscriptionCancel("  sub_123  ").Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_true_for_resource_missing_code()
    {
        var err = new StripeError { Code = "resource_missing" };
        var ex = new StripeException(HttpStatusCode.BadRequest, err, "No such subscription: 'sub_abc'");

        StripePaymentOperations.IsMissingSubscriptionStripeError(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_true_when_message_contains_No_such_subscription()
    {
        var ex = new StripeException("No such subscription: 'sub_abc'");

        StripePaymentOperations.IsMissingSubscriptionStripeError(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_false_for_unrelated_error()
    {
        var err = new StripeError { Code = "card_declined" };
        var ex = new StripeException(HttpStatusCode.PaymentRequired, err, "Your card was declined.");

        StripePaymentOperations.IsMissingSubscriptionStripeError(ex).Should().BeFalse();
    }
}
