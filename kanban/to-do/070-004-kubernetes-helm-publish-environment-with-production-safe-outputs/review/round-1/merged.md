# Round 1 — merged findings
**Date:** 2026-10-06
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:333
- Description: Inline comment claimed a StatefulSet `volumeClaimTemplate`; the chart has a separate `postgres-data` PVC mounted by the StatefulSet.
- Suggestion: Reword to match the chart.
- Source: general
- Disposition notes: Comment now says the volume binds to PVC `postgres-data`, mounted by a single-replica StatefulSet.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs:184,190
- Description: `Mapping()` throws on missing optional values.yaml sections (`config`/`parameters`/`secrets`), so a flag combination without secrets would crash rather than evaluate the rules.
- Suggestion: A null-returning helper for optional sections.
- Source: general
- Disposition notes: Added `OptionalMapping` (TryGetValue) for the three optional sections; `Leaves` already handles null. Structural keys keep the throwing `Mapping`.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:145-149
- Description: Two values.yaml postgres password keys (`secrets.postgres.postgres_password`, `secrets.web_server.postgres_password`) must match under a plain `helm install`; undocumented.
- Suggestion: Document it in the Design region.
- Source: general
- Disposition notes: Design region names both keys and says a plain `helm install` must set both to the same value.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:127,142,151-153
- Description: The Design region didn't say that ingress-class, storage capacity and chart version are baked into the chart at publish time, or that `aspire deploy` needs the same `Publish:Target` switch.
- Suggestion: Clarify both.
- Source: general
- Disposition notes: Design region now says they are publish/deploy-time literals (re-publish, not `helm --set`) and shows `aspire deploy -- --Publish:Target=kubernetes`.

### M5 — Severity: nit — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs:248-257
- Description: The unknown-target test accepted any `InvalidOperationException`.
- Suggestion: Assert on the message.
- Source: general
- Disposition notes: The test now captures the exception and asserts that its message contains `Publish:Target`.

## Duplicates / conflicts

- None (single reviewer).
