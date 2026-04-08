namespace SocioTorcedor.BuildingBlocks.Shared.Extensions;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
}
