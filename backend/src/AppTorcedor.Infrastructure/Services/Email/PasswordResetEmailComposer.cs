using System.Net;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Email;

public sealed class PasswordResetEmailComposer(IOptions<PasswordResetOptions> options) : IPasswordResetEmailComposer
{
    internal const string DefaultSubject = "Redefinição de senha";

    public EmailMessage Compose(string toEmail, string accountEmail, string resetToken)
    {
        var baseUrl = (options.Value.FrontendBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = "http://localhost:5173";

        var path = "/reset-password";
        var query = new Dictionary<string, string?>
        {
            ["email"] = accountEmail,
            ["token"] = resetToken,
        };
        var relative = QueryHelpers.AddQueryString(path, query);
        var resetLink = $"{baseUrl}{relative}";

        var safeTo = toEmail.Trim();
        var encodedLink = WebUtility.HtmlEncode(resetLink);

        var html =
            $"""
            <p>Olá,</p>
            <p>Recebemos um pedido para redefinir a senha da sua conta.</p>
            <p><a href="{encodedLink}">Redefinir senha</a></p>
            <p>Se você não solicitou, ignore este e-mail.</p>
            """;

        var text = $"Redefinir senha: {resetLink}\n\nSe você não solicitou, ignore este e-mail.";

        return new EmailMessage(safeTo, DefaultSubject, html, text);
    }
}
