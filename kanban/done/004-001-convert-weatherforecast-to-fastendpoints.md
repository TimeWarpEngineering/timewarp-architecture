# B004_001_convert-weatherforecast-to-fastendpoints.md

## Description

Convert the WeatherForecast endpoint to use FastEndpoints while maintaining the existing contract structure and source generation capabilities.

## Parent
B004_migrate-api-to-fastendpoints

## Requirements

- Maintain existing contract structure (Query/Response)
- Preserve RouteMixin source generation functionality
- Keep existing validation approach
- Ensure backward compatibility
- Maintain CQRS pattern
- Use generated route configuration in FastEndpoints setup

## Checklist

### Design
- [ ] Review current WeatherForecast endpoint implementation
- [ ] Design FastEndpoints conversion approach
- [ ] Plan testing strategy
- [ ] Add/Update Tests

### Implementation
- [ ] Create new FastEndpoints endpoint class
- [ ] Configure endpoint using generated route information
- [ ] Implement FastEndpoints validation
- [ ] Map existing handler to FastEndpoints
- [ ] Verify source generation still works
- [ ] Test endpoint functionality
- [ ] Ensure all HTTP status codes are properly handled

### Documentation
- [ ] Update endpoint documentation
- [ ] Document any changes to request/response handling
- [ ] Update API documentation if needed

### Review
- [ ] Consider Performance Implications
- [ ] Consider Security Implications
- [ ] Consider Backward Compatibility
- [ ] Code Review
- [ ] Integration Testing

## Notes

This conversion will serve as a template for converting other endpoints to FastEndpoints. Special attention should be paid to:
- Using the generated route configuration from RouteMixin
- Maintaining the existing validation approach
- Preserving the CQRS pattern
- Ensuring proper error handling

## Implementation Notes

Proposed FastEndpoints Implementation:
```csharp
public class GetWeatherForecastsEndpoint : FastEndpoint<Query, Response>
{
    private readonly IRequestHandler<Query, OneOf<Response, SharedProblemDetails>> handler;

    public GetWeatherForecastsEndpoint(IRequestHandler<Query, OneOf<Response, SharedProblemDetails>> handler)
    {
        this.handler = handler;
    }

    public override void Configure()
    {
        // Use the generated code to configure the endpoint
        Verbs(GetVerb()); // Maps to generated GetHttpVerb()
        Routes(Query.RouteTemplate); // Uses generated RouteTemplate constant
        AllowAnonymous();
        Description(d => d
            .Produces<Response>(200)
            .ProducesProblem(400));
    }

    private static FastEndpoints.Http.HttpVerb GetVerb() => Query.GetHttpVerb() switch
    {
        HttpVerb.Get => FastEndpoints.Http.HttpVerb.GET,
        HttpVerb.Post => FastEndpoints.Http.HttpVerb.POST,
        HttpVerb.Put => FastEndpoints.Http.HttpVerb.PUT,
        HttpVerb.Delete => FastEndpoints.Http.HttpVerb.DELETE,
        HttpVerb.Patch => FastEndpoints.Http.HttpVerb.PATCH,
        _ => throw new NotSupportedException($"HTTP verb {Query.GetHttpVerb()} not supported")
    };

    public override async Task HandleAsync(Query req, CancellationToken ct)
    {
        var result = await handler.Handle(req, ct);
        
        await result.Match(
            response => SendOkAsync(response, ct),
            problem => SendErrorsAsync(cancellation: ct)
        );
    }
}
```

Source Generated Code from RouteMixin:
```csharp
partial class GetWeatherForecasts
{
  partial class Query
  {
    public const string RouteTemplate = "api/weatherForecasts";
    public HttpVerb GetHttpVerb() => HttpVerb.Get;
    public string GetRoute() => FormattableString.Invariant($"api/weatherForecasts");
  }
}
```

Key files involved:
- Source/ContainerApps/Api/Api.Server/Features/WeatherForecast/Get/GetWeatherForecastsEndpoint.cs
- Source/ContainerApps/Api/Api.Contracts/Features/WeatherForecast/Queries/GetWeatherForecasts.cs
- Source/ContainerApps/Api/Api.Application/Features/WeatherForecast/GetWeatherForecastsHandler.cs
