using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Email;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace AppTorcedor.Infrastructure.Tests.Services.Email;

public sealed class PasswordResetEmailComposerTests
{
    [Fact]
    public void Compose_builds_absolute_reset_link_with_encoded_query()
    {
        var opt = Microsoft.Extensions.Options.Options.Create(
            new PasswordResetOptions { FrontendBaseUrl = "https://app.example.com" });
        var composer = new PasswordResetEmailComposer(opt);
        var msg = composer.Compose("user@test.local", "user@test.local", "tok+with/special=");
        Assert.Equal("user@test.local", msg.To);
        Assert.Equal(PasswordResetEmailComposer.DefaultSubject, msg.Subject);
        Assert.Contains("https://app.example.com/reset-password", msg.HtmlBody, StringComparison.Ordinal);

        var i = msg.HtmlBody.IndexOf("href=\"", StringComparison.Ordinal);
        Assert.True(i >= 0);
        i += "href=\"".Length;
        var j = msg.HtmlBody.IndexOf('"', i);
        var href = System.Net.WebUtility.HtmlDecode(msg.HtmlBody[i..j]);
        var uri = new Uri(href, UriKind.Absolute);
        var q = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal("user@test.local", q["email"].ToString());
        Assert.Equal("tok+with/special=", q["token"].ToString());
    }
}
