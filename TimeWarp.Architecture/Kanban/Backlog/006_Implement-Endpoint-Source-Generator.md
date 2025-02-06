# 006_Implement-Endpoint-Source-Generator.md

## Description

Create a source generator that automatically generates FastEndpoint implementations from contract classes. This will reduce boilerplate code and ensure consistency across all endpoints in the application.

## Requirements

1. Source generator should:
   - Detect contract classes with RouteMixin attribute
   - Generate corresponding FastEndpoint implementations
   - Maintain all OpenAPI documentation from the contract
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

Example contract for reference:
```csharp
[RouteMixin("api/weatherForecasts", HttpVerb.Get)]
public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
{
    public int? Days { get; set; }
}
```

Example generated endpoint:
```csharp
public class GetWeatherForecastsEndpoint : BaseFastEndpoint<Query, Response>
{
    public override void Configure()
    {
        Get(GetWeatherForecasts.Query.RouteTemplate);
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
