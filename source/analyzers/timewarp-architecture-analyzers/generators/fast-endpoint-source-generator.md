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
   - Validates presence of Query/Command classes with [ApiRoute]
   - Checks for proper interface implementations (IQueryStringRouteProvider, IRequest<>)
   - Detects and prevents route conflicts

3. **OpenAPI Documentation**
   - Extracts documentation from XML comments
   - Uses feature folder structure for tags
   - Supports explicit OpenAPI configuration via attributes
   - Preserves XML documentation in generated endpoints

4. **Generated Endpoint Features**
   - Inherits from BaseFastEndpoint<TRequest, TResponse>
   - Configures routing based on ApiRoute attributes
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
  [ApiRoute("api/weatherForecasts", HttpVerb.Get)]
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

## Authorization (task 110 — fail-closed default)

The generator reads two mutually-exclusive markers on the outer `[ApiEndpoint]` class:

| Marker | Effect |
|--------|--------|
| `[EndpointAuthorize(Policy=…, Roles=…, AuthenticationSchemes=…)]` | Emits `Policies(...)` / `Roles(...)` / `AuthSchemes(...)` |
| `[EndpointAllowAnonymous("reason")]` | Emits `AllowAnonymous()` |
| *(neither)* | Emits **nothing** — FastEndpoints requires authentication by default |

Before task 110, an unmarked contract generated `AllowAnonymous()` — fail-open: a contract author
who forgot the marker shipped a public endpoint silently. The default is now fail-closed: silence
means "requires auth," and going anonymous requires the explicit, reasoned
`[EndpointAllowAnonymous]` opt-out (reason is a required constructor argument, mirroring
`[ClientOnlyContract]`). If both markers are present, `[EndpointAuthorize]` wins at generation, but
that state is a contract-author error — `TWA0013`/`TWA0014` (in
`timewarp-architecture-convention-analyzers`) enforce that every `[ApiEndpoint]` contract states
exactly one posture, and flag a contract carrying `[EndpointAllowAnonymous]` while its nested
`Query`/`Command` implements `IAuthApiRequest` (interface or `[AuthApiRequest]` mixin) as a
contradiction.

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

Authoritative diagnostics: TWE002 (missing Query/Command), TWE003 (route+verb conflict),
TWE007 (unresolvable route/HttpVerb), SG002 (missing FastEndpoints). See
`documentation/developer/reference/api-endpoint-source-generator.md` and AGENTS.md.
