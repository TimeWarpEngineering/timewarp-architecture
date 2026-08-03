# Round 3 — fix verification (commit 2eb5416d, merged to dev)
**Date:** 2026-08-03
**Sources:** fix agent (clean worktree, adversarial repro re-run) + orchestrator reproduction on merged dev

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| critical | 0 | 1 | 0 |
| medium | 0 | 1 | 0 |
| low | 0 | 1 | 0 |
| info | 0 | 1 | 0 |

### R2-1 — CRITICAL — fixed
Config-derived environment path DELETED: Web.Spa ConfigureServices requires explicit
environmentName; web-server Main passes builder.Environment.EnvironmentName; the
IModule-constrained 2-arg overload resolves the IHostEnvironment singleton pre-registered in
Services at host-builder creation (experimentally verified immune to later config sources);
fail-closed default. False Design premise corrected. NEW composition-path regression tests
(mock-auth-composition-tests.cs, 2/2) reproduce the exact attack shape. ACCEPTANCE: the
round-2 adversarial repro re-run against fixed code prints
"MockAuthenticationStateProvider registered = False / FAIL-CLOSED (expected)".
### R2-2 — Medium — fixed: TWA0021 now catches typeof() non-generic and factory-delegate
object-creation registrations; both evasion regressions added; analyzer suite 106/106.
### R2-3 — Low — fixed by orchestrator at epic close (checklist + Results below).
### R2-4 — Info — fixed: stale count comments describe the mechanism, not a tally.

## Gate results
Fix worktree: build 0/0; full dev test all green (aspire 7/7, web suite 98 w/ new tests,
analyzers 106/106); smoke ×3 with mock-auth fail-closed surfaces OK; audit 23/23.
Merged dev (orchestrator): build 0/0; audit 23/23; composition tests 2/2; analyzers 106/106;
smoke SUCCEEDED; config-derived env keys absent from code (comments only).
