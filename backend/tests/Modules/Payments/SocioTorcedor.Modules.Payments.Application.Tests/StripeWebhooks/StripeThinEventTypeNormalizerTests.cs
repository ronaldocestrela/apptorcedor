using FluentAssertions;
using SocioTorcedor.Modules.Payments.Application.StripeWebhooks;

namespace SocioTorcedor.Modules.Payments.Application.Tests.StripeWebhooks;

public sealed class StripeThinEventTypeNormalizerTests
{
    [Theory]
    [InlineData("v1.invoice.paid", "invoice.paid")]
    [InlineData("v1.customer.subscription.updated", "customer.subscription.updated")]
    [InlineData("invoice.paid", "invoice.paid")]
    public void Normalizes_v1_prefix(string input, string expected) =>
        StripeThinEventTypeNormalizer.Normalize(input).Should().Be(expected);
}
