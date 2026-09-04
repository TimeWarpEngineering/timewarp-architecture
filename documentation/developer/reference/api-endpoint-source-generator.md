# API Endpoint Source Generator

The FastEndpoint source generator emits HTTP endpoint classes from contract types marked
`[ApiEndpoint]`. Both **web-server** and **api-server** use it — there are no hand-written MVC
`BaseEndpoint` shims in the template. The generated class is pure HTTP plumbing; handlers run
through TimeWarp.Mediator and validation stays on `FluentValidationBehavior`.

## Opt-in

Generation is **off by default**. Server projects enable it:

```xml
<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>
```

Optional filter when the host transitively references other contract assemblies (web-server
case — it must not emit api-contracts endpoints):

```xml
<ApiEndpointContractAssemblies>TimeWarp.Architecture.Web.Contracts</ApiEndpointContractAssemblies>
```

Empty/unset scans all referenced assemblies (api-server default).

## Usage

1. Mark the outer operation class with `[ApiEndpoint]`. Put route/verb on nested `Query`/`Command`
   via `[ApiRoute]` (not the legacy `[RouteMixin]` name).

```csharp
using TimeWarp.Architecture.Attributes;

[ApiEndpoint]
[EndpointAllowAnonymous("Public demo data with no security surface; the template's reference contract is meant to be reachable with zero setup.")]
public static partial class GetWeatherForecasts
{
    /// <summary>
    /// Get Weather Forecasts
    /// </summary>
    /// <remarks>
    /// Gets Weather Forecasts for the number of days specified in the request
    /// </remarks>
    [ApiRoute("api/weatherForecasts", HttpVerb.Get)]
    public sealed partial class Query
        : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        public int? Days { get; set; }
    }

    public sealed class Response
    {
        public IEnumerable<WeatherForecast> WeatherForecasts { get; set; } = default!;
    }
}
```

2. Authorization on the outer class — **required, exactly one marker** (task 110, fail-closed):

```csharp
[ApiEndpoint]
[EndpointAuthorize(Policy = PermissionIds.IdentityRead, AuthenticationSchemes = AuthenticationSchemeNames.AgentToken)]
public static partial class GetAgentIdentity
{
    [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
    public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;
}
```

| Contract annotation | Generated `Configure()` emission |
|---------------------|----------------------------------|
| `[EndpointAuthorize(Policy = "…")]` | `Policies("…");` |
| `Roles` / `AuthenticationSchemes` | `Roles(…)` / `AuthSchemes(…)` |
| Attribute present, no Policy/Roles | FE default (auth required); no `AllowAnonymous` |
| `[EndpointAllowAnonymous(reason)]` | `AllowAnonymous();` — `reason` is a required, honest, per-contract string |
| **Neither marker** | **Nothing emitted — fail-closed.** FastEndpoints' own default (auth required) applies. This is unreachable in a clean build: **TWA0013** flags the omission. Both markers present, or `[EndpointAllowAnonymous]` alongside a nested `Query`/`Command` implementing `IAuthApiRequest`, is **TWA0014**. |

3. The generator emits a `*Endpoint` class (shape simplified). Production emission pairs FE
   filter tags with OpenAPI tags, and auth config comes only from the contract's posture marker
   (here `[EndpointAllowAnonymous]` → `AllowAnonymous()`; protected contracts emit `Policies(…)`
   instead — there is no fail-open anonymous default):

```csharp
public class GetWeatherForecastsEndpoint
    : BaseFastEndpoint<GetWeatherForecasts.Query, GetWeatherForecasts.Response>
{
    public override void Configure()
    {
        Get("api/weatherforecast");
        AllowAnonymous();
        // FE Tags() = endpoint-filter metadata only (not OpenAPI operation tags).
        Tags("WeatherForecasts");
        Summary(s =>
        {
            s.Summary = "Get Weather Forecasts";
            s.Description = "Gets Weather Forecasts for the number of days specified in the request";
        });
        // OpenAPI/Scalar feature grouping needs Description.WithTags (paired with Tags above).
        Description(d => d
            .WithTags("WeatherForecasts")
            .Produces<GetWeatherForecasts.Response>(200, "Success")
            .ProducesProblem(400, "Bad Request"));
    }
}
```

- Request type is `Query` or `Command` per the nested type name.
- HTTP verb is resolved from the **enum member name** (`Post`, `Put`, …), not the underlying int.
- Empty request DTOs (no public properties) get `EmptyRequestBinder` so FastEndpoints' default
  binder does not reject them.
- `HandleAsync` lives on `BaseFastEndpoint` and dispatches to the mediator — do not re-implement
  the endpoint body.
- Default OpenAPI/filter tag is the **namespace leaf under `Features`** (e.g.
  `…Features.WeatherForecasts` → `"WeatherForecasts"`, `…Features.Admin.Roles` → `"Roles"`).
  Folder paths do not set tags; `[OpenApiTags]` is additive.

## Requirements

1. Contract classes must:
   - Be marked with `[ApiEndpoint]` (from `TimeWarp.Architecture.Attributes`)
   - Be `static` and `partial`
   - Contain a nested `Query` or `Command` with `[ApiRoute]`
   - Implement the usual request interfaces (`IApiRequest` / `IRequest<OneOf<…>>`, etc.)

2. Route uniqueness:
   - Each endpoint must have a unique route + HTTP verb combination
   - Conflicts report **TWE003** on **all** parties in the conflict group; **none** of them are
     generated (per-compilation batch via `.Collect()` — no static cross-compilation registry)

3. HTTP verb is fail-closed: only `Get|Post|Put|Delete|Patch|Head|Options` emit. Unknown or
   unresolvable verbs report **TWE007** and skip that contract (never defaults to Get).

4. Host compilation must reference FastEndpoints and
   `TimeWarp.Foundation.Features.BaseFastEndpoint<,>`. When generation is enabled but those types
   are missing, the generator reports **SG002** once per compilation instead of failing the whole
   compilation (feature flags can strip FE packages while the generator package remains attached).

## OpenAPI documentation

The generator extracts OpenAPI documentation from:

1. XML documentation comments on the nested `Query`/`Command`:
   ```csharp
   /// <summary>
   /// Get Weather Forecasts
   /// </summary>
   /// <remarks>
   /// Detailed description here
   /// </remarks>
   ```

2. Namespace leaf under `Features` (not folder paths):
   - `TimeWarp.Architecture.Features.WeatherForecasts` → Tag: `"WeatherForecasts"`
   - `TimeWarp.Architecture.Features.Admin.Roles` → Tag: `"Roles"`
   - The generator emits both `Tags("…")` (FE endpoint filters) and
     `Description(d => d.WithTags("…"))` (OpenAPI operation tags for Scalar grouping)

3. Explicit attributes (additive to the default feature tag):
   ```csharp
   [OpenApiTags("Weather", "Forecasting")]
   ```

## Diagnostics

| ID | Meaning |
|----|---------|
| TWE002 | `[ApiEndpoint]` missing nested `Query`/`Command` — no emission |
| TWE003 | Same route + verb claimed by multiple contracts — all parties; none generated |
| TWE007 | Unknown / unresolvable `HttpVerb` — fail-closed; no emission |
| SG001 | Source generator log (warning) |
| SG002 | `EnableApiEndpointGeneration` is true but FastEndpoints / `BaseFastEndpoint` are missing |
| TWA0013/0014 | Auth-posture missing or contradictory (convention analyzer) |
| TWA0020 | `[ApiEndpoint]` + `[ClientOnlyContract]` contradiction (convention analyzer) |
| TWA0024 | hosted `[EndpointAuthorize] Policy` is not registered by this server (convention analyzer) |

Contract-shape rules for the outer class (`static`/`partial`, Query/Command present) are enforced
alongside TWA0006 (coverage for every routed contract; TWA0005 MVC verb-mismatch retired
with BaseEndpoint, task 131 F-002) and TWA0013/0014 (every `[ApiEndpoint]` contract carries
exactly one auth-posture marker). Generated endpoints always inherit `BaseFastEndpoint` — there is
no `EndpointType` override (task 131-001 F-005; YAGNI).

## Authorization

The contract is the single source of auth intent; every `[ApiEndpoint]` contract carries exactly
one of `[EndpointAuthorize]` (protected) or `[EndpointAllowAnonymous]` (genuinely public):

```csharp
[ApiEndpoint]
[EndpointAuthorize(Policy = "my-policy")]
public static partial class SecureEndpoint { /* … */ }

[ApiEndpoint]
[EndpointAllowAnonymous("Public demo endpoint; no security surface.")]
public static partial class PublicEndpoint { /* … */ }
```

`IAuthApiRequest` on the nested `Query`/`Command` is a client/mock-mode identity signal only —
it does not secure the server and must not be paired with `[EndpointAllowAnonymous]` (TWA0014).
Do not combine `[ApiEndpoint]` with `[ClientOnlyContract]` (TWA0020).
A named `Policy` must equal a policy this server registers (`AddPolicy` or `PermissionIds` via
`AddPermissionPolicies`); **TWA0024** flags comment-coordinated drift.

## Validation (not in the generator)

Backend validation is **not** generated and is **not** FastEndpoints' FluentValidation pipeline.
Keep `IncludeAbstractValidators = false`. Request validators run via the mediator's
`FluentValidationBehavior`. Handlers must not re-validate.

## Best practices

1. Keep contracts focused and small
2. Use consistent naming patterns
3. Document endpoints using XML comments on Query/Command
4. Organize endpoints in feature folders
5. Put auth intent on the contract — exactly one of `[EndpointAuthorize]` or
   `[EndpointAllowAnonymous(reason)]`, always — not a hand-maintained sidecar; TWA0013/TWA0014
   enforce the pairing at build time
6. Do not hand-write `BaseEndpoint` / MVC controller shims for template contracts
