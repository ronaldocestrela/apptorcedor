using System.Net;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Email;

/// <summary>Lê <see cref="EmailWelcomeTemplateKeys"/> via <see cref="IAppConfigurationPort"/> e aplica placeholders (<c>{{Name}}</c>, <c>{{BannerImage}}</c>).</summary>
public sealed class WelcomeEmailComposer(
    IAppConfigurationPort appConfiguration,
    ILogger<WelcomeEmailComposer> logger) : IWelcomeEmailComposer
{
    internal const string DefaultSubject = "Bem-vindo ao sócio torcedor";

    internal const string DefaultHtml =
        "{{BannerImage}}<p>Olá, {{Name}}!</p><p>Obrigado por se cadastrar.</p>";

    public async Task<EmailMessage> ComposeWelcomeAsync(
        string toEmail,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var safeName = string.IsNullOrWhiteSpace(displayName) ? "Torcedor" : displayName.Trim();
        var encodedName = WebUtility.HtmlEncode(safeName);

        var subjectRow = await appConfiguration.GetAsync(EmailWelcomeTemplateKeys.Subject, cancellationToken).ConfigureAwait(false);
        var htmlRow = await appConfiguration.GetAsync(EmailWelcomeTemplateKeys.Html, cancellationToken).ConfigureAwait(false);
        var imageRow = await appConfiguration.GetAsync(EmailWelcomeTemplateKeys.ImageUrl, cancellationToken).ConfigureAwait(false);

        var subjectTemplate = string.IsNullOrWhiteSpace(subjectRow?.Value) ? DefaultSubject : subjectRow!.Value.Trim();
        var htmlTemplate = string.IsNullOrWhiteSpace(htmlRow?.Value) ? DefaultHtml : htmlRow!.Value;

        if (ContainsSuspiciousTemplate(htmlTemplate))
        {
            logger.LogWarning(
                "Email.Welcome.Html rejeitado (conteúdo suspeito); usando template padrão.");
            htmlTemplate = DefaultHtml;
        }

        var subject = subjectTemplate.Replace("{{Name}}", safeName, StringComparison.Ordinal);

        var bannerHtml = BuildBannerHtml(imageRow?.Value);
        var html = htmlTemplate
            .Replace("{{Name}}", encodedName, StringComparison.Ordinal)
            .Replace("{{BannerImage}}", bannerHtml, StringComparison.Ordinal);

        if (bannerHtml.Length > 0 && !htmlTemplate.Contains("{{BannerImage}}", StringComparison.Ordinal))
            html = bannerHtml + html;

        var text = $"Olá, {safeName}!\n\nObrigado por se cadastrar.";

        return new EmailMessage(toEmail.Trim(), subject, html, text);
    }

    private static bool ContainsSuspiciousTemplate(string html)
    {
        if (string.IsNullOrEmpty(html))
            return true;
        var lower = html.ToLowerInvariant();
        if (lower.Contains("<script", StringComparison.Ordinal) || lower.Contains("javascript:", StringComparison.Ordinal))
            return true;
        if (lower.Contains(" onerror=", StringComparison.Ordinal) || lower.Contains(" onload=", StringComparison.Ordinal))
            return true;
        return false;
    }

    private static string BuildBannerHtml(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return string.Empty;

        var trimmed = rawUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return string.Empty;

        var safeSrc = WebUtility.HtmlEncode(trimmed);
        return $"""<div style="text-align:center;margin-bottom:16px"><img src="{safeSrc}" alt="" style="max-width:100%;height:auto" /></div>""";
    }
}
