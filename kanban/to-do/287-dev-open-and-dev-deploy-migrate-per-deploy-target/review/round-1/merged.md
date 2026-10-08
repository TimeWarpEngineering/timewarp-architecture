# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs (ConnectionStringUnreadableMessage)
- Description: the "pwsh" Key Vault grant used `\"` inside a double-quoted `--query`; pwsh does not escape with backslash, so az gets broken arguments.
- Suggestion: escape with backticks.
- Source: general
- Disposition notes: now `` `" ``; verified in pwsh 7 that the query reaches the child as one argument with literal quotes. Test `Refusals_Should_NameTheFix` asserts the backtick form and no `\"`.

### M2 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/deploy-group.cs / deploy-command.cs
- Description: adding the `deploy` route group made `dev deploy --help` print only the group table (`migrate`), hiding the deploy verb's options.
- Suggestion: make `dev deploy` part of the group.
- Source: general
- Disposition notes: `DeployCommand : DeployGroup` with `[NuruRoute("")]` (Nuru's empty-subcommand-in-group support). `dev deploy --help` now lists `[--target] [--yes]` beside `migrate`; `dev deploy` and `dev deploy migrate` still route (checked by hand). Design regions of both files updated.

### M3 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs (RunAcaBundleAsync) + endpoints/deploy-migrate-command.cs
- Description: when create/bundle threw (Ctrl+C), the rule was still deleted, but a failed delete was only in the result that was never returned, so the "Delete it now" message was lost.
- Suggestion: report the cleanup failure from inside the finally.
- Source: general
- Disposition notes: new `Action<string> reportCleanupFailure` parameter, invoked inside the finally; the command prints `FirewallCleanupFailedMessage` from it. New test `ThrowingBundleAndFailedDelete_Should_StillReportTheCleanupFailure`.

### M4 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs (plan "Runs:" line)
- Description: printed `… < web-migrations.sql` (bash redirection; pwsh rejects `<`).
- Suggestion: `Get-Content -Raw … | …`.
- Source: general
- Disposition notes: `DeployOperate.BuildPipedCommandDisplay` + `PwshQuote` (single-quoted, `'` doubled); step echo uses `PwshQuote` too. Test `PrintedMigrateCommand_Should_BePwsh`.

### M5 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/services/deploy-operate.cs (BuildBundleArguments)
- Description: the connection string (with password) is a bundle argv value, visible in the local process list while the bundle runs.
- Suggestion: pass via environment, or record the trade-off.
- Source: general
- Disposition notes: `--connection` is the EF bundle's documented interface (and the task requirement: "run the published bundle with `--connection`"); the process is short-lived on the operator's own machine, and the dev CLI never prints it. Trade-off recorded in the `deploy-operate.cs` Design region. Decided by: review oracle.

### M6 — Severity: nit — Status: wontfix
- File: tools/dev-cli/services/aspire-deploy.cs (PreflightScope.Migrate)
- Description: migrate on kubernetes requires all four deploy parameters but only uses `k8s-namespace`.
- Suggestion: require only `k8s-namespace`.
- Source: general
- Disposition notes: the template commits all four in AppHost appsettings, so they always resolve; requiring the same set as `dev deploy` keeps "resolved exactly as `dev deploy` resolves them" literal. No operator-visible cost. Decided by: review oracle.

### M7 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/open-command.cs (port-forward wait)
- Description: if the forward is alive but not listening within 30 s, the browser silently never opens and no URL is printed.
- Suggestion: print the URL.
- Source: general
- Disposition notes: prints `DeployOperate.ForwardNotReadyMessage(url, 30)` when the forward is still running and not cancelled; timeout is a named constant; Design region updated.

## Duplicates / conflicts

- None (single reviewer).
