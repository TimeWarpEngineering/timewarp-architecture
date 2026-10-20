# Round 1 — general (retry)
**Date:** 2026-10-20
**Scope reviewed:** branch `task/386-roles-slice-operation-info-analyzer-skill-and-docs` vs `master`. The product commit `a07d79ffd` is unchanged since the first round-1 pass. Master is ahead by two commits, both kanban-only (task 391).
**Reviewer:** review oracle, claude-opus-5-5, headless `ganda task work`. This is a second pass on round 1. The host re-ran the review node because the first pass (`general.md`) exited 143 after it wrote `ORACLE_RESULT: Done`. `general.md` is kept as written.

## Summary

The product change matches both the task and the Goal restatement. TWA0035 uses the shared hosted gate. The six roles requests carry `[Operation]`. The kind interfaces are adopted where the kind is clear. The skill write-up states that the kind is not taken from the verb. I re-ran the acceptance gates instead of trusting the earlier report. Build and tests pass. `ganda repo audit` now fails on one `.gitignore` line, so the branch does not meet the task's acceptance bar as it stands.

Verified independently:

- `OperationAttributeAnalyzer` gates on `HostedRouteDiscovery.TryGetHostedOperation`, the same gate the FastEndpoint and ingress generators use. It matches `TimeWarp.Contracts.OperationAttribute` by full name, and a whitespace name counts as missing. It reports on the identifier of the class that carries `[ApiRoute]`. The 7 tests cover: a report, `[Operation]` present, a whitespace name, a foreign attribute, client-only (both placements), and an unrouted request. That is every case the acceptance bar names.
- Kind adoption: every GET handler moved to `IQuery` is read-only. I spot-checked get-current-session, get-human-ux, get-agent-identity, get-entra-sign-in-offered, get-credentials, and the list handlers. In each one the only `.Add(` builds a result list. The two GETs that stay on bare `IRequest` (SubmitTip, InvokeMeteredCapability) give their reason in their Design regions. The only remaining `IRequest<OneOf` in `*-contracts.cs` is in those two files.
- Roles `IIdempotentCommand` claims hold. UpdateRole assigns `(Name, Description)`. SetRolePermissions replaces the whole set. DeleteRole uses `TryRemove`. A retry leaves the same state.
- The skill reference's claims match `source/foundation/timewarp-contracts/`: `OperationCatalog`, `OperationBindings`, `CliOperationBinding.Pattern`, `IResponseRenderer`, the `.`-or-space separator for `Group`, and "CLI command stays the operation name" (package readme). The generator's `OperationName`, `OperationDescription`, and `OperationGroup` are in `models/endpoint-metadata.cs`.
- Gates re-run in this worktree: `./bin/dev build` gave 0 warnings and 0 errors. Analyzer tests: 231 of 231 passed. Roles runfiles: create-role 5/5, delete-role validator 3/3, set-role-permissions 13/13, update-role validator 5/5. `ganda repo audit`: 31 passed, **1 failed** (see Issue 1).

## Issues

### Issue 1 — Severity: bug
- File: .gitignore (root)
- Description: `ganda repo audit` fails `kanban-porcelain-gitignore` (Error): "Root .gitignore is missing porcelain ignore lines — Missing gitignore pattern: mission.heartbeat". The task's acceptance bar requires `ganda repo audit` to pass, and `tw-pr` treats it as a pre-PR gate. Results still says "Repository passes all audit checks", which is no longer true. The cause is a newer ganda check, not this diff, and master's `.gitignore` lacks the line too. It still blocks this branch's PR. The untracked `kanban/to-do/mission.heartbeat` is exactly what the check guards: `git check-ignore` reports it is not ignored.
- Suggestion: Run `ganda repo audit --fix` on this task branch and commit the `.gitignore` change. Audit fixes are policy, and this boy-scout change is welcome here. Re-run `ganda repo audit` and update the audit line in Results.
- Status: open
