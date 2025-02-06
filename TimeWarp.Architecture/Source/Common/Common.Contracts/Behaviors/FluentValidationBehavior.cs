﻿namespace TimeWarp.Architecture.Behaviors;

using FluentValidation;
using FluentValidation.Results;
using MediatR;

public class FluentValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IValidator<TRequest>? Validator;

    public FluentValidationBehavior(IValidator<TRequest>? validator)
    {
        Validator = validator;
    }

    public async Task<TResponse> Handle
    (
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine("Validating request");
        if (Validator is null)
            return await next();

        ValidationResult? validationResult = await Validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
            return await next();

        // Debug: Log validation errors
        foreach (ValidationFailure error in validationResult.Errors)
        {
            Console.WriteLine($"Validation Error - Property: {error.PropertyName}, Error: {error.ErrorMessage}");
        }

        // Group validation errors by property name
        Dictionary<string, string[]> errors = validationResult.Errors
            .GroupBy(e => e.PropertyName.ToLower())
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        // Debug: Log grouped errors
        foreach (KeyValuePair<string, string[]> error in errors)
        {
            Console.WriteLine($"Grouped Error - Property: {error.Key}, Messages: {string.Join(", ", error.Value)}");
        }

        // Construct SharedProblemDetails matching ASP.NET Core's ValidationProblemDetails format
        SharedProblemDetails problemDetails = new()
        {
            Title = "One or more validation errors occurred",
            Status = 400,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
            Extensions = new Dictionary<string, object?>
            {
                ["errors"] = errors
            }
        };

        // Debug: Log problem details
        Console.WriteLine($"Problem Details - Title: {problemDetails.Title}");
        Console.WriteLine($"Problem Details - Detail: {problemDetails.Detail}");
        Console.WriteLine($"Problem Details - Extensions: {System.Text.Json.JsonSerializer.Serialize(problemDetails.Extensions)}");

        // Create OneOf with SharedProblemDetails
        Type successType = typeof(TResponse).GetGenericArguments()[0];
        Type oneOfType = typeof(OneOf<,>).MakeGenericType(successType, typeof(SharedProblemDetails));
        return (TResponse)oneOfType.GetMethod("FromT1")!.Invoke(null, new object[] { problemDetails })!;
    }
}
