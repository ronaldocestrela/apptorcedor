using System.Reflection;
using FluentValidation;
using MediatR;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.BuildingBlocks.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        if (!IsResultResponse(typeof(TResponse)))
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var error = Error.Validation(
            failures[0].PropertyName,
            string.Join(" ", failures.Select(f => f.ErrorMessage)));

        return (TResponse)CreateFailure(typeof(TResponse), error);
    }

    private static bool IsResultResponse(Type responseType)
    {
        if (responseType == typeof(Result))
            return true;

        return responseType.IsGenericType &&
               responseType.GetGenericTypeDefinition() == typeof(Result<>);
    }

    private static object CreateFailure(Type responseType, Error error)
    {
        if (responseType == typeof(Result))
            return Result.Fail(error);

        if (responseType.IsGenericType &&
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var closed = typeof(Result<>).MakeGenericType(valueType);
            var fail = closed.GetMethod(
                nameof(Result<int>.Fail),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Error) },
                null);
            if (fail is null)
                throw new InvalidOperationException("Result<T>.Fail not found.");

            return fail.Invoke(null, new object[] { error })!;
        }

        throw new InvalidOperationException($"Unsupported response type: {responseType.Name}");
    }
}
