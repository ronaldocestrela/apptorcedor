namespace AppTorcedor.Application.Modules.Administration;

/// <summary>Chaves em <c>AppConfigurationEntries</c> para o e-mail de boas-vindas (HTML + assunto + banner opcional).</summary>
public static class EmailWelcomeTemplateKeys
{
    public const string Subject = "Email.Welcome.Subject";

    public const string Html = "Email.Welcome.Html";

    /// <summary>URL HTTPS de imagem pública (ex.: Cloudinary/CDN) injetada como &lt;img&gt; ou em <c>{{BannerImage}}</c>.</summary>
    public const string ImageUrl = "Email.Welcome.ImageUrl";
}
