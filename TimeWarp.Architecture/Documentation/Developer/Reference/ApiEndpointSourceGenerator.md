# API Endpoint Source Generator

The API Endpoint Source Generator automatically generates FastEndpoint implementations from contract classes, reducing boilerplate code and ensuring consistency across endpoints.

## Usage

1. Mark your contract class with the `[ApiEndpoint]` attribute:

```csharp
[ApiEndpoint]
public static partial class GetWeatherForecasts
{
    /// <summary>
    /// Get Weather Forecasts
    /// </summary>
    /// <remarks>
    /// Gets Weather Forecasts for the number of days specified in the request
    /// </remarks>
    [RouteMixin("api/weatherForecasts", HttpVerb.Get)]
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        public int? Days { get; set; }
    }

    public sealed class Response 
    {
        public IEnumerable<WeatherForecast> WeatherForecasts { get; set; } = default!;
    }
}
```

2. The source generator will create a corresponding endpoint class:

```csharp
public class GetWeatherForecastsEndpoint : BaseFastEndpoint<GetWeatherForecasts.Query, GetWeatherForecasts.Response>
{
    public override void Configure()
    {
        Get("api/weatherForecasts");
        Tags("WeatherForecast");
        Summary(s => {
            s.Summary = "Get Weather Forecasts";
            s.Description = "Gets Weather Forecasts for the number of days specified in the request";
        });
        Description(d => d
            .Produces<Response>(200, "Success")
            .ProducesProblem(400, "Bad Request")
        );
    }

    public override async Task HandleAsync(Query request, CancellationToken ct)
    {
        // Implementation will be provided by the user
        throw new NotImplementedException();
    }
}
```

## Requirements

1. Contract classes must:
   - Be marked with `[ApiEndpoint]`
   - Be `static` and `partial`
   - Contain either a `Query` or `Command` class with `[RouteMixin]`
   - Have proper interface implementations (IQueryStringRouteProvider, IRequest<>)

2. Route uniqueness:
   - Each endpoint must have a unique combination of route and HTTP verb
   - Conflicts will result in compilation errors

## OpenAPI Documentation

The generator extracts OpenAPI documentation from:

1. XML documentation comments:
   ```csharp
   /// <summary>
   /// Get Weather Forecasts
   /// </summary>
   /// <remarks>
   /// Detailed description here
   /// </remarks>
   ```

2. Feature folder structure:
   - `Features/WeatherForecast/` → Tag: "WeatherForecast"

3. Explicit attributes:
   ```csharp
   [OpenApiTags("Weather", "Forecasting")]
   ```

## Diagnostics

The source generator provides clear error messages:

- TWE001: Class marked with [ApiEndpoint] must be static and partial
- TWE002: No Query or Command class found in ApiEndpoint class
- TWE003: Route conflict detected
- TWE004: Invalid interface implementation

## Customization

1. Custom endpoint types:
   ```csharp
   [ApiEndpoint(EndpointType = typeof(MinimalApiEndpoint<,>))]
   ```

2. Authorization:
   ```csharp
   [ApiEndpoint]
   [Authorize]
   public static partial class SecureEndpoint
   ```

## Best Practices

1. Keep contracts focused and small
2. Use consistent naming patterns
3. Document endpoints using XML comments
4. Organize endpoints in feature folders
5. Validate routes early in development