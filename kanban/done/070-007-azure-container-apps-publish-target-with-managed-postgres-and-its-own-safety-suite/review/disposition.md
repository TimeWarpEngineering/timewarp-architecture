# Disposition — task 070-007

**Date:** 2026-10-07
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Two rounds were run at effort 3 by a general reviewer.

- **Round 1:** 1 bug, 5 suggestions and 1 nit.
- **Round 2:** confirmed that M2–M7 are fixed (commit 5e759c6b7) and raised one nit, N1. The orchestrator fixed N1.

The safety-suite facts were checked against deliberately broken copies of the published Bicep, and each one caught its regression. Final gates:
- `AcaPublish_Given_` 9/9
- dev-cli-tests 118/118
- `dev build` 0/0
- `ganda repo audit` passes

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | bug | **What changed:** docs only — every place now says web routes are expected to fail through the ACA ingress. **Why the routing itself is left alone:** fixing it means choosing the aca web-route host strategy, which is a security-design change to passkey RP-ID host selection. It also needs a real Azure deploy to verify, and workers must not deploy. **Scope:** the aca target is opt-in, and Compose and Kubernetes are unaffected. | Review orchestrator; escalated to the maintainer |

## Escalations

- M1 needs a maintainer decision on the ACA web-route host strategy before the aca web surface works. The candidate is aca-only routes without the original-Host transform, with web-server's RP-ID host accessor reading X-Forwarded-Host. It is recorded in the AppHost `#region Open Questions` and in the tw-deploy skill.
