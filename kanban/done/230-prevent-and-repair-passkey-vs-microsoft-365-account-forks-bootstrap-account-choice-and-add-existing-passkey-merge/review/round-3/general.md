# Round 3 — general
**Date:** 2026-09-16
**Scope reviewed:** M5 Jaribu wrapper wiring + carry-forward M1–M4

## Summary

M5 is fixed: both InMemory and EF Credentials runners now expose a public static `Update_rejects_PrincipalId_reparent` wrapper next to `Update_missing_credential_fails`, matching sibling forwarding shape. Wrapper delta is four lines only and introduces no new defects. M1–M4 remain fixed on a glance of the product paths from round 2; orchestrator reported Credentials filter 13/13 in both projects.

## Prior findings

### M1 — Severity: bug — Status: fixed
- Verification: carried from round 2 unless you reopened
### M2 — Severity: bug — Status: fixed
### M3 — Severity: suggestion — Status: fixed
### M4 — Severity: suggestion — Status: fixed
### M5 — Severity: bug — Status: fixed
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-contract-tests.cs:85; tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs:147
- Verification: wrappers present in both runners (InMemory cast-to-base `static new Task`; EF `new Suite()` forwarder). Shared contract method still at `principal-store-contract-tests.cs:288`. Delta is +3/+1 lines only; Credentials suite now 13 cases (was 12). Orchestrator: `dotnet test -c Release -- --filter-class Credentials` → 13/13 both projects.
- Status: fixed
