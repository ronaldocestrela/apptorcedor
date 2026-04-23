using AppTorcedor.Application.Modules.Administration;
using AppTorcedor.Infrastructure.Services.Email;
using AppTorcedor.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppTorcedor.Infrastructure.Tests.Services.Email;

public sealed class WelcomeEmailComposerTests
{
    [Fact]
    public async Task Compose_uses_defaults_when_config_empty()
    {
        var sut = new WelcomeEmailComposer(new EmptyAppConfigurationPort(), NullLogger<WelcomeEmailComposer>.Instance);
        var msg = await sut.ComposeWelcomeAsync("u@test", "Pat", CancellationToken.None);
        Assert.Equal(WelcomeEmailComposer.DefaultSubject, msg.Subject);
        Assert.Contains("Pat", msg.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", msg.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compose_replaces_banner_placeholder_and_encodes_name()
    {
        var cfg = new DictionaryAppConfigurationPort(new Dictionary<string, string>
        {
            [EmailWelcomeTemplateKeys.Html] = "{{BannerImage}}<p>Oi {{Name}}</p>",
            [EmailWelcomeTemplateKeys.ImageUrl] = "https://img.example/x.png",
        });
        var sut = new WelcomeEmailComposer(cfg, NullLogger<WelcomeEmailComposer>.Instance);
        var msg = await sut.ComposeWelcomeAsync("u@test", "A & B", CancellationToken.None);
        Assert.Contains("Oi A &amp; B", msg.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("https://img.example/x.png", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compose_prepends_banner_when_placeholder_absent()
    {
        var cfg = new DictionaryAppConfigurationPort(new Dictionary<string, string>
        {
            [EmailWelcomeTemplateKeys.Html] = "<p>Só texto {{Name}}</p>",
            [EmailWelcomeTemplateKeys.ImageUrl] = "https://img.example/y.png",
        });
        var sut = new WelcomeEmailComposer(cfg, NullLogger<WelcomeEmailComposer>.Instance);
        var msg = await sut.ComposeWelcomeAsync("u@test", "X", CancellationToken.None);
        Assert.StartsWith("<div", msg.HtmlBody.TrimStart(), StringComparison.Ordinal);
        Assert.Contains("<p>Só texto X</p>", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compose_falls_back_when_html_has_script()
    {
        var cfg = new DictionaryAppConfigurationPort(new Dictionary<string, string>
        {
            [EmailWelcomeTemplateKeys.Html] = "<script>alert(1)</script><p>x</p>",
        });
        var sut = new WelcomeEmailComposer(cfg, NullLogger<WelcomeEmailComposer>.Instance);
        var msg = await sut.ComposeWelcomeAsync("u@test", "Y", CancellationToken.None);
        Assert.DoesNotContain("alert", msg.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Y", msg.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compose_ignores_non_http_image_url()
    {
        var cfg = new DictionaryAppConfigurationPort(new Dictionary<string, string>
        {
            [EmailWelcomeTemplateKeys.Html] = "{{BannerImage}}<p>{{Name}}</p>",
            [EmailWelcomeTemplateKeys.ImageUrl] = "javascript:alert(1)",
        });
        var sut = new WelcomeEmailComposer(cfg, NullLogger<WelcomeEmailComposer>.Instance);
        var msg = await sut.ComposeWelcomeAsync("u@test", "Z", CancellationToken.None);
        Assert.DoesNotContain("javascript", msg.HtmlBody, StringComparison.OrdinalIgnoreCase);
    }
}
