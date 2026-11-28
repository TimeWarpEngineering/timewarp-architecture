# Round 2 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

Final ledger (round-1 IDs carried forward, no new findings):

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/aspire/projects/aspire-service-defaults/secondary-otlp-exporter.cs:65
- Description: (round 1) The code used reflection to set the private `AppendSignalPathToEndpoint`.
- Disposition notes: Verified in round 2. There is now one named exporter per signal: http/protobuf gets the full signal URL and gRPC gets the base URI. The reflection is removed, and the graph path assertions run for both protocols.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs:192
- Description: (round 1) The `ListedCommands` parser change was not recorded and had no tests.
- Disposition notes: Verified in round 2. Results records why. `ListedCommands_Given_` has three cases (offset 0, after warning lines, ANSI on the JSON line).

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/secondary-otlp-environment.cs:17
- Description: (round 1) Each test class had its own environment gate.
- Disposition notes: Verified in round 2. `SecondaryOtlpEnvironment.IsolateAsync` is the one gate both classes use.

## Duplicates / conflicts

- None. Single reviewer.

## Evidence

- No screenshots were listed under `evidence/` for this task. The change is server-side telemetry wiring with no page.
