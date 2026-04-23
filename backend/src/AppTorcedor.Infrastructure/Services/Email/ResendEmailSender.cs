using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace AppTorcedor.Infrastructure.Services.Email;

/// <summary>Sends e-mail via <see cref="IResend"/> using configured From address/name.</summary>
public sealed class ResendEmailSender(
    IResend resend,
    IOptions<EmailOptions> emailOptions,
    ILogger<ResendEmailSender> logger) : Application.Abstractions.IEmailSender
{
    public async Task SendAsync(Application.Abstractions.EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.To);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.HtmlBody);

        var resendOpts = emailOptions.Value.Resend;
        if (string.IsNullOrWhiteSpace(resendOpts.FromAddress))
            throw new InvalidOperationException("Email:Resend:FromAddress is required when Email:Provider is Resend.");

        var from = string.IsNullOrWhiteSpace(resendOpts.FromName)
            ? resendOpts.FromAddress.Trim()
            : $"{resendOpts.FromName.Trim()} <{resendOpts.FromAddress.Trim()}>";

        var payload = new global::Resend.EmailMessage
        {
            From = from,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        payload.To.Add(message.To.Trim());

        var response = await resend.EmailSendAsync(payload, cancellationToken).ConfigureAwait(false);
        if (!response.Success)
        {
            var detail = response.Exception?.Message ?? "Resend API returned failure without details.";
            logger.LogError(
                "Resend e-mail falhou (API). To={To}, From={From}, Detalhe={Detail}",
                message.To,
                from,
                detail);
            throw new InvalidOperationException($"Resend e-mail falhou: {detail}");
        }

        logger.LogInformation(
            "Resend e-mail enfileirado. Id={EmailId}, To={To}",
            response.Content,
            message.To);
    }
}
