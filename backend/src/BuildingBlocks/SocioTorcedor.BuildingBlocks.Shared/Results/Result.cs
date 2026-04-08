namespace SocioTorcedor.BuildingBlocks.Shared.Results;

public sealed class Result
{
    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public Error? Error { get; }

    public static Result Ok() => new(true, null);

    public static Result Fail(Error error) => new(false, error);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }

    public static Result<T> Ok(T value) => new(true, value, null);

    public static Result<T> Fail(Error error) => new(false, default, error);
}
