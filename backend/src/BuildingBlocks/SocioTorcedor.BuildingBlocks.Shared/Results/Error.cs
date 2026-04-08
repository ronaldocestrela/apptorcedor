namespace SocioTorcedor.BuildingBlocks.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static Error Validation(string code, string message) => new(code, message);

    public static Error NotFound(string code, string message) => new(code, message);

    public static Error Conflict(string code, string message) => new(code, message);

    public static Error Failure(string code, string message) => new(code, message);
}
