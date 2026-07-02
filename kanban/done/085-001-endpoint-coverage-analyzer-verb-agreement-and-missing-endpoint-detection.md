# Endpoint coverage analyzer: verb agreement and missing-endpoint detection

## Parent
085-analyzer-and-source-gen-opportunities-to-remove-inference-collected-candidates

## Description

One analyzer, two diagnostics, same data walk (server compilation vs. referenced contract
metadata) — targets the exact bug class hit in tasks 078/079 (POST api/Roles → 405 because no
endpoint existed; verb drift is its sibling failure).

- **TWPA0005 — verb mismatch**: for each `BaseEndpoint<TRequest, TResponse>` subclass, the
  `[HttpGet/Post/Put/Delete]` attribute on its `Process` method must match `TRequest`'s
  `[ApiRoute]` verb (readable from referenced-assembly metadata: the generated `GetHttpVerb()` or
  the attribute data).
- **TWPA0006 — contract without endpoint**: enumerate referenced contract types carrying a
  generated `RouteTemplate` and warn when no endpoint class in the server compilation covers them.

## Checklist

- [x] Opt-out decided: **`[ClientOnlyContract(reason)]`** in foundation-contracts — reason is a
      required ctor arg (an unexplained opt-out is drift with paperwork). Matched by simple name.
- [x] Both diagnostics implemented in `timewarp-architecture-convention-analyzers` (one
      compilation walk). Gates: analyzer acts only when the compilation declares ≥1
      BaseEndpoint/BaseFastEndpoint subclass; contract discovery walks only *contracts*
      assemblies **sharing the server's first name segment** (see Results — pairing rule).
- [x] Fixie tests (5): verb match clean; mismatch flagged at the attribute; uncovered flagged
      (no-location diagnostic); `[ClientOnlyContract]` clean; endpoint-less compilation clean.
- [x] 080 recipe: enumerated 8 violations, reconciled all in this change (4 implemented,
      3 opted out, 1 was a scoping bug — see Results). `dev build` 0/0.

## Results

- **api-server: 0 violations** — source-generated FastEndpoints count as coverage (the generator
  runs before analyzers, so its endpoints are visible symbols). TWPA0005 deliberately skips
  FastEndpoints: they're generated FROM the contract's verb, drift is impossible.
- **Pairing rule discovered on first run:** web-server references `api-contracts` as a *client*
  (SPA weather page) — the analyzer initially demanded web-server serve `GetWeatherForecasts`.
  Rule: a server vouches only for contracts assemblies sharing its first name segment
  (`web-server` ↔ `web-contracts`).
- **Latent bug found by the reconcile:** `GetRole`/`DeleteRole` carried `UserId` that could
  never reach the server — GET/DELETE send no body (`BaseApiService.PrepareContent`), and
  `PrepareRoute` only consulted `IQueryStringRouteProvider` for GET. Fixed: both contracts
  compose `UserId` into the query string; `PrepareRoute` treats DELETE like GET.
- **Reconciled:** roles read/update/delete **implemented** (shared in-memory `RoleStore` seeded
  with the well-known RoleIds + 3 handlers + 3 endpoints — the roles demo is now full CRUD,
  unblocking 078's Edit/View modes); `GetCurrentUser`, `CreateTodoItem`, `UpdateTodoItem`
  **opted out** with reasons.
- **Verified:** analyzer tests 26/26 (5 new); web-server integration 22 passed (4 new: seeded
  list, get-by-id, 404 on unknown id, create→update→get→delete round-trip with query-string
  auth); contracts round-trips 7/7; sourcegen 14/14; `dev build` 0/0.
