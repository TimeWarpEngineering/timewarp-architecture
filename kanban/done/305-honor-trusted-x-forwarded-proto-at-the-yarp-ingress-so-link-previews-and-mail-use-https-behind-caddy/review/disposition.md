# Disposition — task 305

**Date:** 2026-10-11
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 2 with the general reviewer. Round 1 raised 1 bug and 2 suggestions, and all three are fixed
on this task id. M1: run mode now loads its ingress routes from the yarp Development config, so
that config is now template-gated per flag. M2: an empty `TrustedProxy` list now means empty, not
the loopback default. M3: the docs no longer claim the published ACA ingress keeps https. Round 2
re-verified the fixes and found nothing new. Gates: `dev build` clean, yarp-integration-tests 15/15,
`dev template-smoke` SUCCEEDED, `ganda repo audit` pass. The Aspire `IngressSmoke` suite was not
re-run in review. For the default flags the run-mode route config is unchanged; the only additions
are comment lines.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|

## Escalations

- None. Out of scope, noted for a follow-up: the published AddYarp ingress behind a TLS-terminating edge may forward `http`. The docs now say so and point to `SiteMetadata:BaseUrl` / `PublicOrigin`.
