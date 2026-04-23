namespace AppTorcedor.Application.Abstractions;

/// <summary>Outbound e-mail (HTML + optional plain text) for transactional sends.</summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

/// <summary>Port for sending e-mail (Mock or Resend in Infrastructure).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
