using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Payments;

/// <summary>ASAAS: link de pagamento (cartão, parcelado) e cobrança PIX.</summary>
public sealed class AsaasPaymentProvider(
    HttpClient http,
    IOptions<PaymentsOptions> paymentsOptions,
    AppDbContext db) : IPaymentProvider
{
    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ProviderKey => "Asaas";

    public Task CreateSubscriptionAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task<PixPaymentProviderResult> CreatePixAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        Guid? payingUserId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCurrency(currency);
        if (payingUserId is null)
            throw new InvalidOperationException("ASAAS PIX exige identificação do usuário (payingUserId).");

        var apiKey = RequireApiKey();

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == payingUserId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
            throw new InvalidOperationException("Usuário não encontrado para cobrança PIX.");

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == payingUserId.Value, cancellationToken)
            .ConfigureAwait(false);
        var cpf = DigitsOnly(profile?.Document);
        if (string.IsNullOrEmpty(cpf))
            throw new InvalidOperationException("Cadastre seu CPF no perfil para pagar com PIX via ASAAS.");

        var customerId = await EnsureCustomerAsync(
                apiKey,
                payingUserId.Value,
                user.UserName ?? user.Email ?? "Cliente",
                cpf,
                user.Email,
                cancellationToken)
            .ConfigureAwait(false);

        var dueDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var body = new Dictionary<string, object?>
        {
            ["customer"] = customerId,
            ["billingType"] = "PIX",
            ["value"] = amount,
            ["dueDate"] = dueDate,
            ["description"] = "Assinatura sócio torcedor",
            ["externalReference"] = paymentId.ToString("D"),
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "v3/payments");
        req.Headers.TryAddWithoutValidation("access_token", apiKey);
        req.Content = JsonContent.Create(body, options: JsonWrite);

        using var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"ASAAS PIX falhou ({(int)resp.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var copyPaste = root.TryGetProperty("pixCopiaECola", out var pc) ? pc.GetString() : null;
        var payload = root.TryGetProperty("payload", out var pl) ? pl.GetString() : null;
        var qrPayload = !string.IsNullOrEmpty(copyPaste) ? copyPaste : payload;
        if (string.IsNullOrEmpty(qrPayload))
            throw new InvalidOperationException("Resposta ASAAS PIX sem payload / pixCopiaECola.");
        return new PixPaymentProviderResult(qrPayload, copyPaste);
    }

    public async Task<CardPaymentProviderResult> CreateCardAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        int? maxInstallments = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCurrency(currency);
        var apiKey = RequireApiKey();
        var asaasOpts = paymentsOptions.Value.Asaas;
        var successUrl = asaasOpts.SuccessUrl?.Trim();
        if (string.IsNullOrEmpty(successUrl))
            throw new InvalidOperationException("Payments:Asaas:SuccessUrl é obrigatória.");

        var installments = maxInstallments ?? 1;
        var chargeType = installments >= 2 ? "INSTALLMENT" : "DETACHED";

        var body = new Dictionary<string, object?>
        {
            ["name"] = "Assinatura sócio torcedor",
            ["description"] = "Cobrança de associação — AppTorcedor",
            ["billingType"] = "CREDIT_CARD",
            ["chargeType"] = chargeType,
            ["value"] = amount,
            ["externalReference"] = paymentId.ToString("D"),
            ["callback"] = new Dictionary<string, object?>
            {
                ["successUrl"] = successUrl,
                ["autoRedirect"] = true,
            },
        };

        if (chargeType == "INSTALLMENT")
            body["maxInstallmentCount"] = Math.Clamp(installments, 2, 21);

        using var req = new HttpRequestMessage(HttpMethod.Post, "v3/paymentLinks");
        req.Headers.TryAddWithoutValidation("access_token", apiKey);
        req.Content = JsonContent.Create(body, options: JsonWrite);

        using var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"ASAAS link de pagamento falhou ({(int)resp.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var url = root.GetProperty("url").GetString();
        var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("Resposta ASAAS sem URL do link de pagamento.");

        return new CardPaymentProviderResult(url, id);
    }

    public async Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
            return;

        var apiKey = paymentsOptions.Value.Asaas.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            return;

        // Payment links and charges use different DELETE paths; try link first.
        await TryDeleteAsync($"v3/paymentLinks/{Uri.EscapeDataString(externalReference)}", apiKey, cancellationToken)
            .ConfigureAwait(false);
        await TryDeleteAsync($"v3/payments/{Uri.EscapeDataString(externalReference)}", apiKey, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalReference) || !externalReference.StartsWith("pay_", StringComparison.Ordinal))
            return;

        var apiKey = paymentsOptions.Value.Asaas.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Payments:Asaas:ApiKey não está configurada.");

        using var req = new HttpRequestMessage(HttpMethod.Post, $"v3/payments/{Uri.EscapeDataString(externalReference)}/refund");
        req.Headers.TryAddWithoutValidation("access_token", apiKey);
        req.Content = JsonContent.Create(new Dictionary<string, object?>());

        using var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"ASAAS estorno falhou ({(int)resp.StatusCode}): {err}");
        }
    }

    private string RequireApiKey()
    {
        var apiKey = paymentsOptions.Value.Asaas.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Payments:Asaas:ApiKey não está configurada.");
        return apiKey;
    }

    private static void ValidateCurrency(string currency)
    {
        if (!string.Equals(currency.Trim(), "BRL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O provedor ASAAS nesta integração aceita apenas moeda BRL.");
    }

    private async Task TryDeleteAsync(string relative, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, relative);
            req.Headers.TryAddWithoutValidation("access_token", apiKey);
            using var resp = await http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            // 404 = already gone
        }
        catch (HttpRequestException)
        {
            // best-effort cancel
        }
    }

    private async Task<string> EnsureCustomerAsync(
        string apiKey,
        Guid userId,
        string name,
        string cpfCnpj,
        string? email,
        CancellationToken cancellationToken)
    {
        var extRef = userId.ToString("D");
        using (var getReq = new HttpRequestMessage(HttpMethod.Get, $"v3/customers?externalReference={Uri.EscapeDataString(extRef)}"))
        {
            getReq.Headers.TryAddWithoutValidation("access_token", apiKey);
            using var getResp = await http.SendAsync(getReq, cancellationToken).ConfigureAwait(false);
            if (getResp.IsSuccessStatusCode)
            {
                var json = await getResp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data)
                    && data.ValueKind == JsonValueKind.Array
                    && data.GetArrayLength() > 0)
                {
                    var first = data[0];
                    if (first.TryGetProperty("id", out var idEl))
                        return idEl.GetString() ?? throw new InvalidOperationException("ASAAS: cliente sem id.");
                }
            }
        }

        var createBody = new Dictionary<string, object?>
        {
            ["name"] = name.Length > 0 ? name : "Cliente",
            ["cpfCnpj"] = cpfCnpj,
            ["externalReference"] = extRef,
        };
        if (!string.IsNullOrWhiteSpace(email))
            createBody["email"] = email.Trim();

        using var postReq = new HttpRequestMessage(HttpMethod.Post, "v3/customers");
        postReq.Headers.TryAddWithoutValidation("access_token", apiKey);
        postReq.Content = JsonContent.Create(createBody, options: JsonWrite);

        using var postResp = await http.SendAsync(postReq, cancellationToken).ConfigureAwait(false);
        var createJson = await postResp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!postResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"ASAAS: falha ao criar cliente ({(int)postResp.StatusCode}): {createJson}");

        using var created = JsonDocument.Parse(createJson);
        return created.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("ASAAS: resposta de cliente sem id.");
    }

    private static string DigitsOnly(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return string.Empty;
        return new string(s.Where(char.IsDigit).ToArray());
    }
}
