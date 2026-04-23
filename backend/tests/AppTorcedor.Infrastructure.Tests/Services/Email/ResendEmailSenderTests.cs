using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Resend;
using OutboundEmail = AppTorcedor.Application.Abstractions.EmailMessage;

namespace AppTorcedor.Infrastructure.Tests.Services.Email;

public sealed class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_maps_to_resend_with_display_name()
    {
        var resend = new Mock<IResend>(MockBehavior.Strict);
        var sentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        global::Resend.EmailMessage? captured = null;
        resend
            .Setup(r => r.EmailSendAsync(It.IsAny<global::Resend.EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<global::Resend.EmailMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(new ResendResponse<Guid>(sentId, default));

        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Provider = "Resend",
            Resend = new EmailResendOptions
            {
                FromAddress = "noreply@club.test",
                FromName = "Clube FC"
            }
        });

        var sut = new ResendEmailSender(resend.Object, options, NullLogger<ResendEmailSender>.Instance);
        await sut.SendAsync(new OutboundEmail("torcedor@mail.test", "Assunto", "<strong>HTML</strong>", "Texto"));

        Assert.NotNull(captured);
        Assert.Equal("Clube FC <noreply@club.test>", captured!.From);
        Assert.Single(captured.To);
        Assert.Equal("torcedor@mail.test", captured.To[0]);
        Assert.Equal("Assunto", captured.Subject);
        Assert.Equal("<strong>HTML</strong>", captured.HtmlBody);
        Assert.Equal("Texto", captured.TextBody);
        resend.Verify(
            r => r.EmailSendAsync(It.IsAny<global::Resend.EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_uses_address_only_when_name_empty()
    {
        var resend = new Mock<IResend>(MockBehavior.Strict);
        global::Resend.EmailMessage? captured = null;
        resend
            .Setup(r => r.EmailSendAsync(It.IsAny<global::Resend.EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<global::Resend.EmailMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), default));

        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Resend = new EmailResendOptions { FromAddress = "hello@club.test", FromName = "   " }
        });

        var sut = new ResendEmailSender(resend.Object, options, NullLogger<ResendEmailSender>.Instance);
        await sut.SendAsync(new OutboundEmail("a@b", "s", "<p>x</p>"));

        Assert.Equal("hello@club.test", captured!.From);
    }

    [Fact]
    public async Task SendAsync_throws_when_from_address_missing()
    {
        var resend = new Mock<IResend>();
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Resend = new EmailResendOptions { FromAddress = "", FromName = "X" }
        });
        var sut = new ResendEmailSender(resend.Object, options, NullLogger<ResendEmailSender>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SendAsync(new OutboundEmail("a@b", "s", "<p>x</p>")));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SendAsync_throws_when_to_or_subject_or_html_invalid(string bad)
    {
        var resend = new Mock<IResend>(MockBehavior.Strict);
        var options = Microsoft.Extensions.Options.Options.Create(new EmailOptions
        {
            Resend = new EmailResendOptions { FromAddress = "f@club.test" }
        });
        var sut = new ResendEmailSender(resend.Object, options, NullLogger<ResendEmailSender>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SendAsync(new OutboundEmail(bad, "s", "<p>x</p>")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SendAsync(new OutboundEmail("a@b", bad, "<p>x</p>")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SendAsync(new OutboundEmail("a@b", "s", bad)));
    }
}
