using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Tests.TestSupport;

/// <summary>Usado em testes que não exercitam envio de e-mail.</summary>
public sealed class NoopEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
