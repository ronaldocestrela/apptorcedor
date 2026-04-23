namespace AppTorcedor.Application.Abstractions;

/// <summary>Monta o <see cref="EmailMessage"/> de boas-vindas a partir de template em banco (com fallback em código).</summary>
public interface IWelcomeEmailComposer
{
    Task<EmailMessage> ComposeWelcomeAsync(string toEmail, string displayName, CancellationToken cancellationToken = default);
}
