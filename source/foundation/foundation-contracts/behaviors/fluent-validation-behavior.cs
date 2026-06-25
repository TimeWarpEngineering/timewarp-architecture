namespace TimeWarp.Architecture.Behaviors;

public class FluentValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IValidator<TRequest>[] Validators;

    public FluentValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        Validators = validators.ToArray();
    }

    public async Task<TResponse> Handle
    (
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (Validators.Length == 0)
            return await next();

        ValidationResult[] validationResults = await Task.WhenAll
        (
            Validators.Select(validator => validator.ValidateAsync(request, cancellationToken))
        );

        var validationFailures = validationResults
            .SelectMany(validationResult => validationResult.Errors)
            .ToList();

        if (validationFailures.Count == 0)
            return await next();

        // Group validation errors by property name
        var errors = validationFailures
            .GroupBy(e => e.PropertyName.ToLower())
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        // Construct SharedProblemDetails matching ASP.NET Core's ValidationProblemDetails format
        SharedProblemDetails problemDetails = new()
        {
            Title = "One or more validation errors occurred",
            Status = 400,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Detail = string.Join("; ", validationFailures.Select(e => e.ErrorMessage)),
            Extensions = new Dictionary<string, object?>
            {
                ["errors"] = errors
            }
        };

        // Create OneOf with SharedProblemDetails
        Type successType = typeof(TResponse).GetGenericArguments()[0];
        Type oneOfType = typeof(OneOf<,>).MakeGenericType(successType, typeof(SharedProblemDetails));
        return (TResponse)oneOfType.GetMethod("FromT1")!.Invoke(null, new object[] { problemDetails })!;
    }
}
