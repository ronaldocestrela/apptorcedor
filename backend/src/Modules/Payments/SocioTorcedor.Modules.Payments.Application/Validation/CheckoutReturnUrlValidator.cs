namespace SocioTorcedor.Modules.Payments.Application.Validation;

/// <summary>
/// Valida URLs de retorno do Stripe Checkout (success/cancel): absolutas e apenas http/https.
/// </summary>
public static class CheckoutReturnUrlValidator
{
    public static bool IsValid(string? url, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            errorMessage = "Return URL is required.";
            return false;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            errorMessage = "Return URL must be an absolute URI.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Return URL must use http or https.";
            return false;
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            errorMessage = "Return URL must include a host.";
            return false;
        }

        return true;
    }
}
