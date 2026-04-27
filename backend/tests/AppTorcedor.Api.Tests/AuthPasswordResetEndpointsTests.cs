using System.Net;
using System.Net.Http.Json;
using AppTorcedor.Api.Tests.Support;
using Microsoft.AspNetCore.WebUtilities;

namespace AppTorcedor.Api.Tests;

public sealed class AuthPasswordResetEndpointsTests : IClassFixture<PasswordResetWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CapturingEmailSender _emails;

    public AuthPasswordResetEndpointsTests(PasswordResetWebApplicationFactory factory)
    {
        _emails = factory.EmailCapture;
        _emails.Clear();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Forgot_password_unknown_email_returns_204_and_sends_nothing()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@example.com" });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Empty(_emails.Sent);
    }

    [Fact]
    public async Task Forgot_password_active_user_sends_email_with_reset_link()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "admin@test.local" });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Single(_emails.Sent);
        var html = _emails.Sent[0].HtmlBody;
        Assert.Contains("reset-password", html, StringComparison.Ordinal);
        Assert.Contains("admin@test.local", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reset_password_with_token_from_email_allows_login_with_new_password()
    {
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "admin@test.local" });
        Assert.Single(_emails.Sent);
        var html = _emails.Sent[0].HtmlBody;
        var href = ExtractHref(html);
        Assert.NotNull(href);
        var uri = new Uri(href!, UriKind.Absolute);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var email = query["email"].ToString();
        var token = query["token"].ToString();
        Assert.False(string.IsNullOrEmpty(email));
        Assert.False(string.IsNullOrEmpty(token));

        var newPassword = "NewSecurePass9";
        var resetRes = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { email, token, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, resetRes.StatusCode);

        var oldLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Reset_password_with_invalid_token_returns_bad_request()
    {
        var resetRes = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new { email = "admin@test.local", token = "not-a-real-token", newPassword = "AnotherPass9" });
        Assert.Equal(HttpStatusCode.BadRequest, resetRes.StatusCode);
    }

    private static string? ExtractHref(string html)
    {
        const string prefix = "href=\"";
        var i = html.IndexOf(prefix, StringComparison.Ordinal);
        if (i < 0)
            return null;
        i += prefix.Length;
        var j = html.IndexOf('"', i);
        if (j < 0)
            return null;
        var raw = html[i..j];
        return System.Net.WebUtility.HtmlDecode(raw);
    }
}
