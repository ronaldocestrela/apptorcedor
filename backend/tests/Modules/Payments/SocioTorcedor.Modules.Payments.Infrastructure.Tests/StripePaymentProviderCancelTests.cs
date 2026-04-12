using System.Net;
using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using SocioTorcedor.Modules.Payments.Infrastructure.Services;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Tests;

public sealed class StripePaymentProviderCancelTests
{
    private static StripePaymentProvider CreateProvider(string stripeSecretKey = "sk_test_fake_for_unit_tests")
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new PaymentsOptions { StripeSecretKey = stripeSecretKey });
        return new StripePaymentProvider(options);
    }

    [Fact]
    public async Task CancelAsync_with_empty_id_does_not_throw()
    {
        var sut = CreateProvider();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.Member,
            string.Empty,
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelAsync_with_whitespace_id_does_not_throw()
    {
        var sut = CreateProvider();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.Member,
            "   ",
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelAsync_with_mem_sub_legacy_stub_id_does_not_throw_and_skips_stripe()
    {
        var sut = CreateProvider();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.Member,
            "mem_sub_0123456789abcdef0123456789abcdef",
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancelAsync_with_saas_sub_legacy_stub_id_does_not_throw()
    {
        var sut = CreateProvider();

        var act = async () => await sut.CancelAsync(
            PaymentProviderContext.SaaS,
            "saas_sub_0123456789abcdef0123456789abcdef",
            cancellationToken: CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_false_for_null_or_whitespace()
    {
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel(null).Should().BeFalse();
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel("").Should().BeFalse();
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel(" \t ").Should().BeFalse();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_false_for_legacy_stub_style_ids()
    {
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel("mem_sub_abc").Should().BeFalse();
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel("saas_sub_xyz").Should().BeFalse();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_is_true_for_sub_prefix()
    {
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel("sub_123").Should().BeTrue();
    }

    [Fact]
    public void ShouldInvokeStripeSubscriptionCancel_trims_whitespace_before_sub_prefix_check()
    {
        StripePaymentProvider.ShouldInvokeStripeSubscriptionCancel("  sub_123  ").Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_true_for_resource_missing_code()
    {
        var err = new StripeError { Code = "resource_missing" };
        var ex = new StripeException(HttpStatusCode.BadRequest, err, "No such subscription: 'sub_abc'");

        StripePaymentProvider.IsMissingSubscriptionStripeError(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_true_when_message_contains_No_such_subscription()
    {
        var ex = new StripeException("No such subscription: 'sub_abc'");

        StripePaymentProvider.IsMissingSubscriptionStripeError(ex).Should().BeTrue();
    }

    [Fact]
    public void IsMissingSubscriptionStripeError_false_for_unrelated_error()
    {
        var err = new StripeError { Code = "card_declined" };
        var ex = new StripeException(HttpStatusCode.PaymentRequired, err, "Your card was declined.");

        StripePaymentProvider.IsMissingSubscriptionStripeError(ex).Should().BeFalse();
    }
}
