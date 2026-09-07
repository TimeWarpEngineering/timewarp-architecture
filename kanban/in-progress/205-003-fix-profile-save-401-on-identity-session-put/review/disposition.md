# Disposition — task 205-003

**Date:** 2026-09-07
**Outcome:** clean
**Rounds:** 1
**Final open count:** 0

## Summary

Effort-1 general review of the Profile Save identity-session PUT 401 fix. Round 1 raised no issues: server named WebService loopback forwards inbound Cookie and mock-principal header; WASM named clients set fetch credentials SameOrigin; empty/non-JSON 401/403 synthesize honest SharedProblemDetails; unsigned-in Save toasts then `/Login?returnUrl=/Profile` without rethrowing; anonymous PUT stays 401; mock auth stays fail-closed; HTTP cookie isolation matches the GetProfile session exemplar. Live `/Profile` Save was not re-run on this branch (documented in implementer Results). No wontfix.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
