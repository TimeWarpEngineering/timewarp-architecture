# Disposition — task 253

**Date:** 2026-09-24
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Effort-1 general review of commit 188466a1 found no bugs. One suggestion (M1: AddPasskey does not
refuse new-account challenges) was accepted as wontfix: cosmetic naming only, the attach target is
always the authenticated caller, and enforcing it would break AddPasskey for SPA bundles cached from
before the deploy.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Name-only mislabel for non-SPA/legacy-bundle callers; no security impact; enforcing breaks cached pre-deploy SPA bundles | orchestrator (review oracle) |

## Escalations

- None.
