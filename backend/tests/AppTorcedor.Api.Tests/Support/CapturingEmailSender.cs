using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Api.Tests.Support;

public sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public void Clear() => Sent.Clear();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
