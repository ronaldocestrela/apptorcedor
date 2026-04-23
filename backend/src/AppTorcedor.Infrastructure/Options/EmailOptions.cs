namespace AppTorcedor.Infrastructure.Options;

/// <summary>E-mail provider (Mock vs Resend) and Resend-specific settings.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Mock (default) or Resend.</summary>
    public string Provider { get; set; } = "Mock";

    public EmailResendOptions Resend { get; set; } = new();
}

/// <summary>Resend API and default From header.</summary>
public sealed class EmailResendOptions
{
    /// <summary>API key (<c>re_…</c>).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender address (e.g. <c>noreply@yourdomain.com</c>).</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Optional display name for the From header.</summary>
    public string FromName { get; set; } = string.Empty;
}
