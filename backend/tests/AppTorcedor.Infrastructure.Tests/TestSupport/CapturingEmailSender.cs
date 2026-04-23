using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Tests.TestSupport;

public sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
