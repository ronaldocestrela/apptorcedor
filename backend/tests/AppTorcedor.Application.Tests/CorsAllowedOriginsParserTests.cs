using AppTorcedor.Application.Modules.Administration;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class CorsAllowedOriginsParserTests
{
    [Fact]
    public void Parse_json_array_normalizes_and_deduplicates()
    {
        var raw = """["https://A.COM", "https://a.com", "  http://localhost:5173 "]""";
        var list = CorsAllowedOriginsParser.Parse(raw);
        Assert.Equal(2, list.Count);
        Assert.Contains("https://a.com", list);
        Assert.Contains("http://localhost:5173", list);
    }

    [Fact]
    public void Parse_csv_and_lines()
    {
        var raw = "https://one.example\nhttps://two.example,https://three.example";
        var list = CorsAllowedOriginsParser.Parse(raw);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Parse_rejects_path_query_userinfo()
    {
        Assert.Null(CorsAllowedOriginsParser.TryNormalizeOrigin("https://x.com/path"));
        Assert.Null(CorsAllowedOriginsParser.TryNormalizeOrigin("https://x.com?q=1"));
        Assert.Null(CorsAllowedOriginsParser.TryNormalizeOrigin("https://u@x.com"));
    }

    [Fact]
    public void Parse_json_invalid_falls_back_to_token_split()
    {
        var raw = "[not json";
        var list = CorsAllowedOriginsParser.Parse(raw, (_, _) => { });
        Assert.Empty(list);
    }
}
