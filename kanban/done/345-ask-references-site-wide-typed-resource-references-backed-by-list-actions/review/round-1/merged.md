# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 1 |
| nit | 0 | 3 | 1 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/analyzers/shared/referenceable-resource-shape.cs
- Description: Analyzer accepted shapes the generated registry cannot compile (struct Response, required Query members, non-public types / UserId setter, Query not IApiRequest).
- Source: general (Issue 1)
- Disposition notes: TWA0033 now rejects all listed shapes; generator skips them; analyzer tests added (4181c4291).

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/shared/referenceable-resource-shape.cs
- Description: Roles-only `[EndpointAuthorize]` offered the type to every signed-in user.
- Source: general (Issue 2)
- Disposition notes: Policy is now required (TWA0033); skill note corrected; test added (4181c4291).

### M3 — Severity: suggestion — Status: wontfix
- File: source/analyzers/timewarp-architecture-convention-analyzers/referenceable-resource-analyzer.cs
- Description: TWA0034 duplicate-prefix check is per compilation; cross-assembly duplicates silently dropped by the generator.
- Source: general (Issue 3)
- Disposition notes: The template ships one contracts assembly; cross-assembly contracts is not a supported layout today. Revisit if a second hosted-contracts assembly appears. Decided by: review orchestrator.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/resource-reference-type.cs
- Description: Transport/JSON failures escaped the search, leaving the picker on "Searching…"; null items threw.
- Source: general (Issue 4)
- Disposition notes: Caught and returned as failed search; null items → empty (4181c4291).

### M5 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/ask-reference-state/ask-reference-state.cs
- Description: Picker state and chosen references survived sign-out / sign-in (cross-user label leak).
- Source: general (Issue 5)
- Disposition notes: New SessionChanged action keyed on principal id, dispatched from AuthenticationStateListener; SPA test added (4181c4291).

### M6 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor
- Description: Every `@` (e.g. inside an email address) opened the picker and stole focus.
- Source: general (Issue 6)
- Disposition notes: Opens only when `@` starts a word (`AtStartsWord` in ask-ai.ts); Playwright mid-word check added (4181c4291).

### M7 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts
- Description: Inserted token had no trailing space.
- Source: general (Issue 7)
- Disposition notes: One space appended unless whitespace follows (4181c4291).

### M8 — Severity: nit — Status: fixed
- File: source/analyzers/shared/referenceable-resource-shape.cs
- Description: Stamped random UserId; Design claim inaccurate.
- Source: general (Issue 8)
- Disposition notes: Design region and skill note record that referenceable IAuthApiRequest handlers must not rely on UserId (4181c4291).

### M9 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/feedback/ef-feedback-store-infrastructure.cs
- Description: Case folding differs between EF (Postgres lower) and in-memory (OrdinalIgnoreCase) stores.
- Source: general (Issue 9)
- Disposition notes: Recorded as an accepted difference in the store Design region (4181c4291).

### M10 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-reference-tests.cs
- Description: No race test for SearchGeneration; some TWA0033 branches untested.
- Source: general (Issue 10)
- Disposition notes: Partially addressed (6 new analyzer cases in 4181c4291). The concurrent race test needs a gated fake API service that the SPA test host does not have; the guard is a simple generation compare. Remaining TWA0033 branches (missing Query/Response, bad ItemsProperty, missing [ApiEndpoint]) are straightforward early returns. Decided by: review orchestrator.

## Duplicates / conflicts

- None (single reviewer).
