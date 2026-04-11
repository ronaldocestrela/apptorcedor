using FluentAssertions;
using SocioTorcedor.Modules.Payments.Application.StripeWebhooks;

namespace SocioTorcedor.Modules.Payments.Application.Tests.StripeWebhooks;

public sealed class StripeWebhookEnvelopeTests
{
    [Fact]
    public void Thin_event_payload_is_detected()
    {
        var json = """{"object":"v2.core.event","type":"v2.core.event_destination.ping","id":"evt_x"}""";
        StripeWebhookEnvelope.IsThinEventNotification(json).Should().BeTrue();
    }

    [Fact]
    public void Snapshot_event_payload_is_not_thin()
    {
        var json = """{"object":"event","type":"invoice.paid","id":"evt_x"}""";
        StripeWebhookEnvelope.IsThinEventNotification(json).Should().BeFalse();
    }
}
