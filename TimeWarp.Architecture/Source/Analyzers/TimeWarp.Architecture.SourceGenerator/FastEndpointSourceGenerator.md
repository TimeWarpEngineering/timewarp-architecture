# FastEndpointSourceGenerator

## Purpose
The FastEndpointSourceGenerator is a Roslyn-based source generator that automatically generates FastEndpoint implementations based on contract classes marked with the [ApiEndpoint] attribute. It works across assembly boundaries, scanning both the including project and its referenced assemblies for endpoint definitions. This reduces boilerplate code and ensures consistency across all endpoints in the application.

## Key Features
1. **Cross-Assembly Generation**
   - Scans both the including project and referenced assemblies for endpoint definitions
   - Detects classes marked with [ApiEndpoint] attribute
   - Generates implementations in the project that includes the generator

2. **Contract Detection**
   - Identifies static partial classes with [ApiEndpoint] attribute
   - Validates presence of Query/Command classes with [RouteMixin]
   - Checks for proper interface implementations (IQueryStringRouteProvider, IRequest<>)
   - Detects and prevents route conflicts

3. **OpenAPI Documentation**
   - Extracts documentation from XML comments
   - Uses feature folder structure for tags
   - Supports explicit OpenAPI configuration via attributes
   - Preserves XML documentation in generated endpoints

4. **Generated Endpoint Features**
   - Inherits from BaseFastEndpoint<TRequest, TResponse>
   - Configures routing based on RouteMixin attributes
   - Sets up OpenAPI documentation
   - Handles authorization settings
   - Configures response types

## Example Usage

### Contract Definition
```csharp
[ApiEndpoint]
public static partial class GetWeatherForecasts
{
  /// <summary>
  /// Get Weather Forecasts
  /// </summary>
  [RouteMixin("api/weatherForecasts", HttpVerb.Get)]
  public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public int? Days { get; set; }
  }
}
```

### Generated Endpoint
```csharp
public class GetWeatherForecastsEndpoint : BaseFastEndpoint<Query, Response>
{
  public override void Configure()
  {
    Get(GetWeatherForecasts.Query.RouteTemplate);
    Summary
    (
      s =>
      {
        s.Summary = "Get Weather Forecasts";
      }
    );
    Description
    (
      d => d.Produces<Response>(200).ProducesProblem(400)
    );
  }
}
```

## Implementation Details
1. Uses SelectMany with recursive namespace traversal to find classes in referenced assemblies
2. Validates class structure and attributes
3. Generates endpoint code with proper configuration
4. Outputs files to the Generated folder in the including project
5. Provides clear compiler diagnostics for validation errors

## Project Configuration
- Source generator is referenced as an analyzer in the project that needs endpoint generation
- Generated files are output to the Generated folder
- Cross-assembly type resolution is handled automatically

## Error Handling
Provides clear compiler diagnostics for:
- Missing or incorrect class structure
- Missing required attributes
- Route conflicts
- Invalid endpoint type configurations
