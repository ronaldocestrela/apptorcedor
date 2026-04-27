namespace AppTorcedor.Infrastructure.Options;

/// <summary>URL pública do SPA para montar o link de redefinição de senha enviado por e-mail.</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "Auth:PasswordReset";

    /// <summary>Origem do app (ex.: <c>https://app.exemplo.com</c>), sem barra final.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
