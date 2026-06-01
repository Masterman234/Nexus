using FluentValidation;
using MediatR;
using Nexus.SharedKernel;

namespace Nexus.Application.Behaviors;

public class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Select(validationFailure => validationFailure.ErrorMessage)
            .Distinct()
            .ToArray();

        if (errors.Any())
        {
            return CreateValidationResult<TResponse>(string.Join(", ", errors));
        }

        return await next();
    }

    private static TResult CreateValidationResult<TResult>(string error)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
        {
            return (TResult)Result.Failure(error);
        }

        object validationResult = typeof(Result<>)
            .GetGenericTypeDefinition()
            .MakeGenericType(typeof(TResult).GetGenericArguments()[0])
            .GetMethod(nameof(Result.Failure))!
            .Invoke(null, [error])!;

        return (TResult)validationResult;
    }
}
