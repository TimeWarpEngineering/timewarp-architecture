# Round 1 — general
**Date:** 2026-07-31

## Summary

aspire-tests migrated to Jaribu MTP with SetupOnce/CleanUpOnce for DistributedApplication.
integration-test1 deleted. xUnit CPM pins removed (last consumer). Full `dev test` green
including aspire-tests via MTP path.

## Verification

| Gate | Result |
|------|--------|
| bare `dotnet test` in project dir | 6/6 (5 ingress + 1 prefix unit) |
| `dev test` MTP path | aspire-tests passed (~22s) |
| full `dev test` | completed successfully |
| solution build | 0/0 |
| xunit remaining in repo | none |

## Issues

_None._
