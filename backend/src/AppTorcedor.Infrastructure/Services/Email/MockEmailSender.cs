using AppTorcedor.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Email;

/// <summary>Logs intended sends without calling an external provider (dev/tests).</summary>
public sealed class MockEmailSender(ILogger<MockEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "E-mail (Mock): não enviado à rede; simulação. To={To}, Subject={Subject}",
            message.To,
            message.Subject);
        return Task.CompletedTask;
    }
}
