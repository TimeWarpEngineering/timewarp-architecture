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

- [ ] Decide the opt-out for deliberately client-only/mock-only contracts (marker attribute vs
      config list) — TWPA0006 needs it or it fires on every unimplemented demo contract.
- [ ] Implement both diagnostics in `timewarp-architecture-convention-analyzers`; scope to
      compilations that reference a `*contracts*` assembly AND declare `BaseEndpoint` subclasses
      (server projects), so contracts/spa projects are unaffected.
- [ ] Fixie tests: matching verb clean; mismatched verb flagged; covered contract clean;
      uncovered contract flagged; opted-out contract clean.
- [ ] 080 recipe: wire, enumerate real violations (expect: GetRole/GetRoles/UpdateRole/DeleteRole
      have no endpoints — decide implement vs opt-out per contract), reconcile in the same PR.

## Notes

- api-server's FastEndpoints path generates endpoints from contracts, so TWPA0006 may be
  web-server-specific; verify how generated endpoints appear to the analyzer before scoping.
