# Round 2 — general
**Date:** 2026-09-04
**Scope reviewed:** Post-fix delta on `task.md` coverage-audit intro (M1). Product files unchanged vs round 1.

## Summary

M1 is fixed: the coverage-audit intro no longer attributes InvokeMeteredCapability anonymous 401 to `web-server-integration-tests` alone. It names that suite for in-proc HostGraph coverage and the co-located `invoke-metered-capability-tests.cs` `Unauthorized_Given_No_Bearer` for the anonymous cell. Cookie-isolation gap on that route is still marked. No new findings. Product diff is unchanged from round 1 (4/4 probe + 4/4 GetAgentBearerIdentity already re-run).

## Issues
