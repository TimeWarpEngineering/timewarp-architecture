# 006_Implement-Endpoint-Source-Generator.md

## Description

Create a source generator that automatically generates FastEndpoint implementations from contract classes. This will reduce boilerplate code and ensure consistency across all endpoints in the application.

## Requirements

1. Source generator should:
   - Detect static partial classes marked with [ApiEndpoint] attribute
   - Use [ApiEndpoint] to determine the endpoint type to generate (BaseFastEndpoint by default)
   - Use RouteTemplate from RouteMixin source generator and HttpVerb from RouteMixin attribute for endpoint configuration
   - Validate source code structure:
     * Ensure classes with [ApiEndpoint] are static and partial
     * Verify presence of Query/Command class with [RouteMixin]
     * Check for proper interface implementations (IQueryStringRouteProvider, IRequest<>)
     * Detect route conflicts with existing endpoints
   - Emit clear compiler diagnostics for:
     * Missing or incorrect class structure
     * Missing required attributes
     * Route conflicts
     * Invalid endpoint type configurations
   - Extract OpenAPI documentation from:
     * XML documentation comments on the Query/Command class (for Summary and Description)
     * Feature folder structure (for Tags, e.g., "WeatherForecast" from Features/WeatherForecast/)
     * Additional attributes that can be added to the contract for explicit OpenAPI configuration
   - Handle proper routing configuration
   - Support both query and command endpoints
   - Preserve XML documentation comments

2. Generated endpoints should:
   - Inherit from BaseFastEndpoint<TRequest, TResponse>
   - Configure routing based on RouteMixin attributes
   - Set up proper OpenAPI documentation
   - Handle authorization settings
   - Configure response types

## Checklist

### Design
- [ ] Create source generator project structure
- [ ] Design the generator's syntax tree analysis
- [ ] Plan attribute-based configuration options
- [ ] Design error reporting and diagnostics
- [ ] Create unit tests for the generator

### Implementation
- [ ] Create TimeWarp.Architecture.SourceGenerator project
- [ ] Implement contract detection and analysis
- [ ] Implement endpoint code generation
- [ ] Add diagnostic reporting
- [ ] Implement unit tests
- [ ] Create sample endpoints for testing

### Documentation
- [ ] Document generator usage and configuration
- [ ] Add XML documentation comments
- [ ] Create usage examples
- [ ] Document known limitations

### Review
- [ ] Consider Performance Implications
  - Generator execution time
  - Generated code efficiency
- [ ] Consider Security Implications
  - Authorization handling
  - Input validation preservation
- [ ] Code Review
  - Generated code quality
  - Error handling
  - Edge cases

## Notes

Example contract showing ApiEndpoint generation with OpenAPI documentation (in Features/WeatherForecast/Queries/GetWeatherForecasts.cs):
```csharp
[ApiEndpoint] // Triggers FastEndpoint generation (default)
// Or [ApiEndpoint(EndpointType = typeof(MinimalApiEndpoint<,>))] for different endpoint type
public static partial class GetWeatherForecasts
{
    /// <summary>
    /// Get Weather Forecasts
    /// </summary>
    /// <remarks>
    /// Gets Weather Forecasts for the number of days specified in the request
    /// </remarks>
    [RouteMixin("api/weatherForecasts", HttpVerb.Get)] // Handles routing
    [OpenApiTags("Weather")] // Explicit tag (highest priority)
    [OpenApiOperation("Get Weather Forecasts", "Gets Weather Forecasts for the number of days specified")] // Explicit summary/description
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        /// <summary>
        /// The Number of days of forecasts to get
        /// </summary>
        /// <example>5</example>
        [OpenApiParameter(Description = "Number of forecast days to retrieve")] // Explicit parameter documentation
        public int? Days { get; set; }
    }
}

// Alternative example using only XML comments (fallback documentation):
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
        /// <summary>
        /// The Number of days of forecasts to get
        /// </summary>
        /// <example>5</example>
        public int? Days { get; set; }
    }
}
```

Example generated endpoint:
```csharp
public class GetWeatherForecastsEndpoint : BaseFastEndpoint<Query, Response>
{
    public override void Configure()
    {
        // The HttpVerb is known at generation time from the RouteMixin attribute
        Get(GetWeatherForecasts.Query.RouteTemplate); // Generated as Get/Post/Put/Delete based on attribute
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Weather Forecasts";
            s.Description = "Gets Weather Forecasts for the number of days specified in the request";
        });
        Description(d => d
            .Produces<Response>(200)
            .ProducesProblem(400)
        );
        Tags("Weather");
    }
}
```

## Implementation Notes

Key considerations:
1. Use Roslyn for source code analysis and generation
2. Preserve XML documentation for OpenAPI
3. Handle both query string and route parameters
4. Support different HTTP verbs (GET, POST, PUT, DELETE)
5. Maintain proper namespace organization
6. Documentation sourcing priority:
   - First: Look for explicit OpenAPI attributes on the contract
   - Second: Use XML documentation comments
   - Third: Generate from class/feature names (e.g., "GetWeatherForecasts" -> "Get Weather Forecasts")
   - Fourth: Allow global defaults in source generator configuration

7. Example compiler diagnostics:
   ```
   error TWE001: Class marked with [ApiEndpoint] must be static and partial
   Location: Features/WeatherForecast/Queries/GetWeatherForecasts.cs:10

   error TWE002: No Query or Command class with [RouteMixin] found in ApiEndpoint class
   Location: Features/WeatherForecast/Queries/GetWeatherForecasts.cs:10

   error TWE003: Route conflict detected: 'api/weatherForecasts' [GET] is already used by 'ExistingEndpoint'
   Location: Features/WeatherForecast/Queries/GetWeatherForecasts.cs:15

   warning TWE101: Query class should implement IQueryStringRouteProvider for proper route generation
   Location: Features/WeatherForecast/Queries/GetWeatherForecasts.cs:12
   ```
