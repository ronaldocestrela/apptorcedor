using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Shipping;

public sealed class MelhorEnvioShippingService : IMelhorEnvioShippingPort
{
    private static readonly JsonSerializerOptions s_json =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions s_melhorRequestJson =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly HttpClient _http;
    private readonly MelhorEnvioOptions _options;
    private readonly ILogger<MelhorEnvioShippingService> _logger;

    public MelhorEnvioShippingService(
        HttpClient http,
        IOptions<MelhorEnvioOptions> options,
        ILogger<MelhorEnvioShippingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShippingOptionDto>> CalculateAsync(string toCep, CancellationToken cancellationToken = default)
    {
        var cep = new string((toCep ?? "").Where(char.IsDigit).ToArray());
        if (cep.Length != 8)
            return Array.Empty<ShippingOptionDto>();

        if (string.IsNullOrWhiteSpace(_options.Token))
            return Array.Empty<ShippingOptionDto>();

        var from = new string(_options.FromPostalCode.Where(char.IsDigit).ToArray());
        if (from.Length != 8)
        {
            _logger.LogWarning("MelhorEnvio FromPostalCode is invalid or empty.");
            return Array.Empty<ShippingOptionDto>();
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/v2/me/shipment/calculate";
        var envelope = new MeCalculateRequest(
            new MePostal(from),
            new MePostal(cep),
            new MePackage(
                _options.PackageHeight,
                _options.PackageWidth,
                _options.PackageLength,
                _options.PackageWeight));
        var jsonBody = JsonSerializer.Serialize(envelope, s_melhorRequestJson);

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token.Trim());
        if (!string.IsNullOrWhiteSpace(_options.UserAgent))
        {
            // HTTP headers must be ASCII-only; strip any accented characters.
            var ua = new string(_options.UserAgent.Trim().Where(c => c < 128).ToArray());
            if (!string.IsNullOrWhiteSpace(ua))
                req.Headers.TryAddWithoutValidation("User-Agent", ua);
        }

        try
        {
            var res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var json = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Melhor Envio calculate failed: {Status} {Body}", (int)res.StatusCode, json);
                return Array.Empty<ShippingOptionDto>();
            }

            var items = JsonSerializer.Deserialize<List<MelhorCalculateItem>>(json, s_json);
            if (items is null || items.Count == 0)
                return Array.Empty<ShippingOptionDto>();

            var list = new List<ShippingOptionDto>();
            foreach (var row in items)
            {
                if (!string.IsNullOrWhiteSpace(row.Error))
                    continue;
                if (string.IsNullOrWhiteSpace(row.Price))
                    continue;
                if (!decimal.TryParse(row.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    continue;

                var name = (row.Name ?? "").Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                var companyName = row.Company?.Name?.Trim() ?? "";
                var picture = row.Company?.Picture?.Trim() ?? "";
                var days = row.DeliveryRange?.Max ?? row.DeliveryTime ?? 0;
                list.Add(new ShippingOptionDto(row.Id, name, companyName, picture, price, days));
            }

            list.Sort(static (a, b) => a.Price.CompareTo(b.Price));
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Melhor Envio calculate exception.");
            return Array.Empty<ShippingOptionDto>();
        }
    }

    private sealed class MelhorCalculateItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("company")]
        public MelhorCompany? Company { get; set; }

        [JsonPropertyName("delivery_time")]
        public int? DeliveryTime { get; set; }

        [JsonPropertyName("delivery_range")]
        public MelhorDeliveryRange? DeliveryRange { get; set; }
    }

    private sealed class MelhorCompany
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    private sealed class MelhorDeliveryRange
    {
        [JsonPropertyName("max")]
        public int? Max { get; set; }
    }

    private sealed record MeCalculateRequest(MePostal From, MePostal To, MePackage Package);

    private sealed record MePostal(string PostalCode);

    private sealed record MePackage(int Height, int Width, int Length, decimal Weight);
}
