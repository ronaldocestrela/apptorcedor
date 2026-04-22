using System.Text.Json;

namespace AppTorcedor.Application.Modules.Administration;

public static class CorsAllowedOriginsParser
{
    /// <summary>
    /// Extrai e valida origens a partir de uma string (JSON array, linhas, CSV).
    /// Apenas <c>http</c> ou <c>https</c> com authority; sem path, query ou fragmento.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? raw, Action<string, Exception>? onJsonError = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        var trimmed = raw.Trim();
        if (trimmed.Length is 0)
            return Array.Empty<string>();

        if (trimmed.StartsWith('['))
        {
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(trimmed);
                if (array is not null)
                {
                    return ToNormalizedList(
                        array.SelectMany(s => ExpandCsvAndLines(s?.Trim() ?? string.Empty)));
                }
            }
            catch (Exception ex)
            {
                onJsonError?.Invoke(trimmed, ex);
            }
        }

        return ToNormalizedList(ExpandCsvAndLines(trimmed));
    }

    private static IReadOnlyList<string> ToNormalizedList(IEnumerable<string> parts) =>
        parts
            .Select(TryNormalizeOrigin)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<string> ExpandCsvAndLines(string s)
    {
        foreach (var part in s.Split(
                     [',', '\r', '\n', ';'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length is 0) continue;
            var p = part.Trim();
            if (p.Length is 0) continue;
            if (p.StartsWith('[')) continue; // evitar reparse de JSON partido; fallback geral aplica
            yield return p;
        }
    }

    public static string? TryNormalizeOrigin(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var t = raw.Trim();
        if (t.Length is 0)
            return null;

        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;
        if (string.IsNullOrEmpty(uri.Host))
            return null;
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return null;
        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            return null;
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
            return null;

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
