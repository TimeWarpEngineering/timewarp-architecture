# Nav Developer menu updates after SetPrincipalRoles without refresh

## Description

Checking Developer on /Admin/Principals and Save persists roles, but the Developer
nav category stays hidden until a full refresh. AuthorizeView is
`developer.access`. Server InteractiveAuto circuits keep PermissionEvaluator's
task-183 ExpansionCache for the circuit lifetime, so re-eval after
NotifySessionChanged still sees the pre-Save grant set.

## Requirements

- ExpansionCache is in-flight single-flight only (evict after completion)
- Concurrent SSR checks still share one expansion (183)
- Sequential GetPermissions after a role grant change sees the new permission
- NotifySessionChanged still fires for self-edit (WASM claim projection)

## Checklist

- [x] Evaluator evicts completed expansions
- [x] Test: concurrent single-flight still holds; post-mutation re-expands
- [x] Design regions
- [x] Build / run evaluator tests
- [x] Commit

## Results

PermissionEvaluator evicts a completed expansion (TryRemove matching the
in-flight Lazy) so the next AuthorizeView after SetPrincipalRoles re-expands.
Concurrent SSR still single-flights (183). NotifySessionChanged on self-edit
is unchanged (WASM claim path).

### How to validate

```bash
dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs
# Expect: ConcurrentChecks_Should_SingleFlightStoreExpansion pass
# Expect: AfterRoleGrantChange_Should_ReExpand pass
```

Browser: sign in as Administrator, /Admin/Principals, check Developer, Save —
Developer + Demos nav categories appear without a full page refresh. Uncheck
Developer, Save — they disappear.

## Session

- Implementation: grok 2026-08-13
