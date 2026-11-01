# Round 5 — general
**Date:** 2026-11-01
**Scope reviewed:** the CI fix for PR #620 (run 41281550727, `aspire-tests`), commit `278424009`, delta `3cd8eef26..0151ed962`. That commit's product changes are 7 files, +23/−16: `skills/tw-deploy/SKILL.md`, `source/container-apps/aspire/projects/aspire-app-host/program.cs`, and five files under `tests/container-apps/aspire/aspire-tests/` (`aca-publish-tests.cs`, `compose-publish-tests.cs`, `ingress-smoke-tests.cs`, `kubernetes-publish-tests.cs`, `state-dev-tools-model-tests.cs`). The other commits in the delta change only kanban kitchen files.

## Summary

The fix follows `TimeWarp.State.DevTools.Hosting` `12.0.0-beta.16`. The package dll in `~/.nuget/packages/timewarp.state.devtools.hosting/12.0.0-beta.16` contains the literals `timewarp-devtools` and `TimeWarp DevTools`, and does not contain `timewarp-state-devtools`. The AppHost still calls `AddTimeWarpStateDevTools()` with no name, so the resource takes the package default. Every assertion now uses `timewarp-devtools`: the run-mode model fact, the ingress smoke wait and `http` client, and the compose, Kubernetes, and ACA rejections. `/_timewarp/state-devtools` and `TIMEWARP_STATE_DEVTOOLS_ENDPOINT` are unchanged, and `TimeWarp.State*` stays at `12.0.0-beta.16`. The `tw-deploy` row names `timewarp-devtools` and the **TimeWarp DevTools** dashboard link. Outside the two Design regions in M4, nothing in `source/`, `tests/`, or `skills/` still names `timewarp-state-devtools`. The publish suites only checked for the old name, so this change does not weaken them. The package no longer emits that name.

The restatement in `## Goal (implementer restatement)` covers this CI walk. It matches the task's `## Session` CI-red note: follow the package, never pin State backward, and do not change timewarp-state. Nothing was misread.

Gates re-run by this reviewer on 2026-11-01:

- `./bin/dev build`: 0 warnings, 0 errors (32.5s).
- `aspire-tests` (Release, from the project directory): 84 passed, 0 failed (1m 20s). The run includes `RunMode_Should_AddDevToolsAndPointWebServerAtIt`, the ingress smoke, and the publish facts.
- `./bin/dev lint-skills`: 9/9, eval.yaml valid.
- `ganda repo audit`: exit 0. The only advisory is `kanban-landed` for other tasks' claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244, 454).
- The shared AppHost user-secrets file (`a3d4de92-…/secrets.json`) was last modified on 2026-10-29, before and after the test run, so `aspire-tests` does not rewrite it.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:67 (also tests/container-apps/aspire/aspire-tests/state-dev-tools-model-tests.cs:13)
- Description: both Design regions now record package version history: "(TimeWarp.State.DevTools.Hosting 12.0.0-beta.16; 12.0.0-beta.15 named it timewarp-state-devtools and labeled the link "TimeWarp.State DevTools")" in the AppHost, and "TimeWarp.State.DevTools.Hosting 12.0.0-beta.16 names the executable timewarp-devtools (12.0.0-beta.15 named it timewarp-state-devtools)" in the model test. `tw-agent-context-regions` says to state Design decisions timelessly, and git keeps the history. Both files ship in every generated app, whose readers never used beta.15. Each version number is also another place that goes stale at the next State bump.
- Suggestion: say the current fact only, for example "as the timewarp-devtools executable resource (the package default); the dashboard links it as "TimeWarp DevTools"". In the model test, keep "Names are literals, not package constants…" and drop the beta.15/beta.16 sentence, or reduce it to "the package's default resource name". Change comments only.
- Status: open
