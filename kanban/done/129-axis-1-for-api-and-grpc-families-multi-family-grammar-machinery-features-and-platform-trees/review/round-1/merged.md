# Round 1 — merged findings
**Date:** 2026-07-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None — reviewer independently re-verified machinery, rulings, and the 6a consumer graph with
its own planted-file proofs and MSBuild evaluations; zero issues at any severity. Three
for-the-record notes (pre-existing, out of scope): no grpc integration-test project has ever
existed; wildcard !api/!grpc excludes cover the new trees automatically; the features-glob
Exists() asymmetry is inherited verbatim from web's original.
