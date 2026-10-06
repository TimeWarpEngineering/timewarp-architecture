# Round 1 — merged findings
**Date:** 2026-10-07
**Sources:** general, tests, plan_alignment

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 3 |
| nit | 0 | 2 | 2 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/deprovision-command.cs:59
- Description: The Aspire deployment record does not store the kubectl context; after a context switch `dev deprovision --target kubernetes --yes` destroys against the current context.
- Suggestion: Record/compare the context.
- Source: general
- Disposition notes: The record format is Aspire's (`FileDeploymentStateManager`), not ours, and `aspire destroy` resolves the context itself — a dev-cli-side compare would need a second private record. The current context is printed (preflight `Detail`) on every deprovision run before anything happens, and the no-`--yes` path lists the recorded ReleaseName/Namespace. Decided by: review oracle.

### M2 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/deprovision-command.cs:55-90
- Description: The `--yes` gate and record-before-destroy ordering are only tested via text helpers; deleting the `if (!command.Yes)` branch would keep tests green.
- Suggestion: Extract a testable decision.
- Source: tests
- Disposition notes: The gate is two sequential guard clauses in a handler that otherwise only shells out; extracting a decision enum adds indirection without a process seam to test the real behavior (it shells to `aspire`). Ordering is recorded in the Design region; validation steps in task.md exercise both refusal paths end to end. Decided by: review oracle.

### M3 — Severity: suggestion — Status: wontfix
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs
- Description: Only `ResolveTarget` is tested for argument parsing; `--target`/`-t`, `--yes`/`-y` binding untested.
- Suggestion: Add binding tests.
- Source: tests
- Disposition notes: Option binding is Nuru's (generated from `[Option]`), covered upstream; the task's "argument parsing" is target resolution plus aspire argv building, both tested. Bad-target exit was smoke-run (`deploy --target swarm` → exit 1). Decided by: review oracle.

### M4 — Severity: suggestion — Status: fixed
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs (NeverAutomated_Given_)
- Description: Guard scans only `.github/workflows/*.yml`; misses `*.yaml` and `.github/actions`; no positive control proving the regex matches.
- Suggestion: Widen scan, add positive control.
- Source: tests, plan_alignment (nit, collapsed)
- Disposition notes: Now scans `.github/**/*.yml` and `*.yaml` recursively plus `workflow-command.cs`; new `Pattern_Should_MatchEveryDeployInvocationShape` asserts the regex matches `dev deploy`, `./bin/dev deprovision`, `dev.cs -- deploy`, `aspire deploy`, `aspire destroy --non-interactive` and not `dev publish`. Exotic shapes (`aspire --non-interactive deploy`, `dev -v deploy`) left unmatched — the guard is a tripwire, not a parser.

### M5 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:55
- Description: Answering "n" printed the redirected-stdin refusal text.
- Suggestion: Distinct message.
- Source: general
- Disposition notes: Split into `DeployConfirmationRefusal` (no terminal) and new `DeployDeclined` ("declined at the prompt. Nothing was run."); Design region updated; asserted in `DeployPlan_Should_…`.

### M6 — Severity: nit — Status: fixed
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:245
- Description: `ShouldContain("data")` nearly vacuous; prose substrings brittle.
- Suggestion: Assert something specific.
- Source: tests
- Disposition notes: Now asserts "data volume and its data are deleted". `ShouldNotContain("docker")` on the kubernetes no-record text kept deliberately — it is the runtime-neutrality assertion.

### M7 — Severity: nit — Status: wontfix
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs (RepoRoot)
- Description: `RepoRoot` relies on `.git` existing.
- Suggestion: Alternative marker.
- Source: tests
- Disposition notes: `Path.Exists` matches both the `.git` dir and a worktree's `.git` file; dev-cli-tests only run from a checkout (the dev CLI itself resolves root via git). Decided by: review oracle.

### M8 — Severity: nit — Status: wontfix
- File: tools/dev-cli/endpoints/deprovision-command.cs:46
- Description: Full preflight runs before the no-record message; missing Helm/kubectl hides manual-removal text.
- Suggestion: Reorder.
- Source: plan_alignment
- Disposition notes: Reviewer notes it is defensible: the manual kubernetes removal is `helm uninstall` + `kubectl delete pvc`, which need those tools and that context anyway. Decided by: review oracle.

## Duplicates / conflicts

- tests Issue 3 and plan_alignment nit 1 (guard scan breadth) collapsed into M4 at the stronger severity (suggestion).
