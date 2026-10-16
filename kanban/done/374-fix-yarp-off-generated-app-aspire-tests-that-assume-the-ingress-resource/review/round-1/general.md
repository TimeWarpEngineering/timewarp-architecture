# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** branch `task/374-fix-yarp-off-generated-app-aspire-tests-that-assum` vs `master` (product files: `tests/container-apps/aspire/aspire-tests/{aspire-tests.csproj,closed-box-app-host-guard-tests.cs,ingress-smoke-tests.cs}`)

## Summary

The change guards the `ingress` health gate, the ingress reachability poll, and every
through-ingress fact with `#if yarp`. It also guards the closed-box guard's two ingress model
facts plus their `DevRunIngressPorts` field. The `api-server` gate and `ApiRouteThroughIngress_`
go under `#if api`, and the `web-migrations` gate goes under `#if postgres`. The two facts that
don't need the ingress (`FirstRunOnEmptyDatabase_`, `StateDevTools_`) stay unguarded, which
matches the task's requirement 1. `DefineConstants` gets the new flags (TWA0010), the same way
`aspire-app-host.csproj` and `timewarp-testing.csproj` already do it.

Verified against the generated smoke output under `artifacts/template-smoke/work/`:
- `SmokeNoYarp` has zero `"ingress"` references in either file. It keeps the `api-server` and
  `web-migrations` gates and only the four non-ingress members.
- `SmokeNoApi` has no `api-server` gate.
- `SmokeNoPostgres` has no `web-migrations` gate.

I re-ran `ganda repo audit` and it passed. Risk is low: the change only affects tests, and the
default variant compiles the same members as before (`RootThroughIngress_` only moved below
`StateDevTools_`).

Goal restatement vs task: it matches. It covers requirements 1–3 and the acceptance criteria. The
api/postgres widening goes beyond the original yarp scope, but the task's own acceptance ("`dev
template-smoke` green", "decide on aspire-tests") surfaced it, and Results records it. That is
scope found during the work, not a misread.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs:18 and :67-69
- Description: the Design paragraph "Health-gate web→api→ingress, then wait for web-migrations
  … then poll ingress reachability" and the SetupOnce comment "three health waits, the
  web-migrations terminal-state wait, and the ingress-reachability poll" describe only the
  all-flags-on graph. In flag-off variants these comments are partly wrong. The new Task 374
  paragraph lower in the Design region does carry the caveat.
- Suggestion: say "(each gate present only when its flag is on)" in the Design paragraph, or
  move the per-flag caveat next to the SetupOnce comment.
- Status: open
