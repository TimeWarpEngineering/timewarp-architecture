# Disposition — task 275

**Date:** 2026-10-05
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 3 (general, security, tests). One real bug was found and fixed: the Ctrl-K row `@key` collided for contextual rows sharing a catalog Target (M1). Two nits were fixed: the ranker Design region (M3), and JSON null now counts as missing for a required parameter (M5, with a test). Five findings were accepted as lab-scope exceptions; the comparison Notes record the security-relevant ones (M2, M4) for the adoption follow-up. No reviewer found a weakening of server enforcement: the `CredentialRules` reuse is behaviour-identical.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | Cross-approach staleness comes from running B and C side by side; adoption keeps one approach; the server's 409 still holds | orchestrator |
| M4 | suggestion | A documented design decision (the server decides and re-enforces). Recorded as B adoption hardening in the comparison | orchestrator |
| M6 | nit | NAVIGATE targets the non-`/api/` Entra challenge path; same-origin reach is already C's documented weakness | orchestrator |
| M7 | suggestion | No render harness in the repo; buttons come from the tested `HypermediaLabRows`; browser check recorded as not performed | orchestrator |
| M8 | nit | Rename href shape is pinned and the SPA suite covers Rename; end-to-end only worth it for the adopted approach | orchestrator |

## Escalations

- None.
