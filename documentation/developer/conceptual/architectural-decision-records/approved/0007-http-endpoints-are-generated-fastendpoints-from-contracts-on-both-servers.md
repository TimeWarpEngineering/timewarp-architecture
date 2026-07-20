# HTTP endpoints are generated FastEndpoints from contracts on both servers

* Status: accepted
* Architect: Architect
* Date: 2026-07-20

Technical Story: kanban 109 (finish FastEndpoints migration started in 004)

## Context and Problem Statement

Task 004 migrated **api-server** to FastEndpoints generated from contracts and closed that work.
**web-server** was left on hand-written MVC `BaseEndpoint<TRequest, TResponse>` shims — not as a
recorded decision, but as an unfinished half of the migration. Over time web-server accumulated
many shims (including identity ceremony endpoints from the 104 tasks). That dual stack forced
duplicate serializer-pipeline maintenance (MVC `JsonOptions` and `HttpJsonOptions`), let endpoint
verb/route drift away from contracts (the class of bugs TWA0005/0006 guard), and taught the
template two endpoint stories.

Should the template keep MVC shims on web-server, or finish generation on both hosts?

## Decision Drivers

* One endpoint convention for agents and humans reading the template
* Single source of route, verb, and auth intent on the contract
* One JSON seam (`ContractSerializationDefaults` on `ConfigureHttpJsonOptions`) for string enums
  and casing
* Keep validation on the mediator pipeline; avoid dual validation stacks
* Preserve existing web-server integration routes and identity auth flows

## Considered Options

* **A — Status quo split**: api-server generates FastEndpoints; web-server keeps hand MVC shims
* **B — Generate FastEndpoints from contracts on both servers** (complete the 004 migration)
* **C — Hand-written FastEndpoints on both servers** (no generator; drop MVC but keep dual authoring)

## Decision Outcome

Chosen option: **B — Generate FastEndpoints from contracts on both servers**, because it unifies
the template story, collapses the dual HTTP/serializer stack, and makes the contract the single
authoring surface for route, verb, and auth.

Concrete shape:

* **`[ApiEndpoint]`** on the outer operation class — generation opt-in (not every `[ApiRoute]`).
* **`[ApiRoute("…", HttpVerb.X)]`** on nested `Query`/`Command` — route and verb source of truth.
* **`[EndpointAuthorize(Policy=…)]`** / **`[EndpointAllowAnonymous(reason)]`** — exactly one is
  required on every `[ApiEndpoint]` contract; the generator emits `Policies(...)` / `Roles(...)` /
  `AuthSchemes(...)` for the former, `AllowAnonymous()` for the latter, and nothing (fail-closed)
  if neither is present (task 110; TWA0013/TWA0014 enforce the pairing).
* **No hand-written `BaseEndpoint` shims** in the template after cutover.
* **Validation** remains `FluentValidationBehavior` on TimeWarp.Mediator; FastEndpoints'
  `IncludeAbstractValidators` stays false; handlers do not re-validate.
* Hosts set `EnableApiEndpointGeneration`; web-server filters with
  `ApiEndpointContractAssemblies` so transitively referenced contract assemblies do not emit
  foreign endpoints.

### Positive Consequences

* One pipeline and one serializer configuration path through FastEndpoints / HTTP JSON options
* Auth and route metadata live on the contract; TWA0005/0006 cover generated endpoints
* Identity and other web routes keep the same templates; integration suites remain the safety net
* Agents author contracts once; no second MVC shim file per operation

### Negative Consequences

* Generator must stay correct on HttpVerb enum resolution, Query vs Command generics, empty
  request binders, and auth emission (hardened in 109-001/002)
* Blazor + FastEndpoints middleware order must keep auth before FE (documented in host wiring)
* Older docs/skills that described the MVC split needed updating (109-004)
* The original absence-means-anonymous default let auth intent go unstated on generated endpoints;
  landing the fail-closed default required a same-pass sweep annotating every existing
  `[ApiEndpoint]` contract with an explicit marker (task 110)

## Pros and Cons of the Options

### A — Status quo split

* Good, because api-server already worked and web cutover has risk
* Bad, because every new web slice adds another hand shim
* Bad, because dual serializer and dual endpoint conventions drift

### B — Generate on both servers

* Good, because one convention and one generation path
* Good, because contracts already carry route/verb; auth attribute closes the last gap
* Bad, because cutover is atomic (no dual MVC+FE routes on the same templates)

### C — Hand-written FastEndpoints only

* Good, because drops MVC without generator complexity
* Bad, because reintroduces hand endpoint files that must stay in sync with contracts
* Bad, because TWA0005/0006 still need a second artifact to exist by convention

## Links

* Related: [ADR-0003](0003-endpoint-centric-api-with-interface-based-validation.md) — endpoint-centric
  contracts and interface validation
* Reference: [ApiEndpointSourceGenerator.md](../../../reference/ApiEndpointSourceGenerator.md)
* Skill: `skills/web-api-contracts/SKILL.md`
* Tasks: 004 (api-only), 109 / 109-001…109-004 (web cutover + docs), 110 (fail-closed auth default)

<!-- markdownlint-disable-file MD013 -->
