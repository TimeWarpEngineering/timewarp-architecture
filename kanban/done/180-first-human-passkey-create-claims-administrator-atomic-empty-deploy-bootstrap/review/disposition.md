# Disposition — task 180

**Date:** 2026-08-12
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Product rule holds: first human passkey **Create** atomically claims Administrator+Member; later creates stay Member; sign-in and agents do not claim. One coverage gap (EF first-wins) was fixed; handler-level “sign-in does not claim” tests declined as structurally proven.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | Sign-in/agent have no role-store dependency; claim is after successful credential attach. Shared-host first-create tests are order-sensitive. | orchestrator |

## Escalations

- None.
