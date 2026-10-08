# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** branch vs master

## Summary
The implementation matches the requirements closely. The firewall rule is deleted in `finally` with an uncancellable token. The password and connection string never reach a printed line. The runtime comes from `ASPIRE_CONTAINER_RUNTIME`, the published output is refused when missing, the confirmation and summary lines are in place, and the NeverAutomated regex covers `deploy migrate` and `open`. dev-cli-tests pass (192/192, re-run). Problems found: one pwsh command in a refusal does not parse, `dev deploy --help` no longer documents `dev deploy` itself, a cleanup-failure warning is lost on the exception and cancel path, and a few smaller pwsh and UX points.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/services/deploy-operate.cs:398
- Description: `ConnectionStringUnreadableMessage` says it prints the role grant "(pwsh)". Its first line is `$vault = az keyvault list ... --query "[?tags.\"aspire-resource-name\"=='postgres-kv'].name" --output tsv`. pwsh does not treat `\"` as an escape, so the string ends at `\"`. Checked with pwsh 7: the native command gets two broken arguments, `[?tags.\` and `aspire-resource-name\=='postgres-kv'].name`, and the same text in expression mode is a ParserError. An operator who copies the refusal's fix gets an az JMESPath error and an empty `$vault`.
- Suggestion: Emit pwsh escaping: `--query "[?tags.\`"aspire-resource-name\`"=='postgres-kv'].name"` (or `""` doubling). Add a test that pins the pwsh form.
- Status: open

### Issue 2 — Severity: bug
- File: tools/dev-cli/endpoints/deploy-group.cs:13
- Description: Adding the `[NuruRouteGroup("deploy")]` group changes `dev deploy --help`. It now prints only the group table (`deploy commands: migrate …`), not DeployCommand's own usage and options (`--target`, `--yes`, examples). Checked with `dotnet run tools/dev-cli/dev.cs -- deploy --help`. Routing `dev deploy` to DeployCommand still works, as Results says, but the existing verb's help is gone. (`publish --help` behaves the same way, but `publish` has no bare-verb command of its own, so it loses nothing.)
- Suggestion: Make `dev deploy --help` show DeployCommand's usage again, with `migrate` listed as a subcommand. This may need a Nuru change or a different group shape. Otherwise record the limitation and point at `dev --help` / `--capabilities` in the Design region and the skill.
- Status: open

### Issue 3 — Severity: bug
- File: tools/dev-cli/services/deploy-operate.cs:437-456 (consumer: tools/dev-cli/endpoints/deploy-migrate-command.cs:209-213)
- Description: When the create or bundle step throws, for example OperationCanceledException on Ctrl+C, the `finally` still runs the delete and sets `cleanupFailure`. The exception then propagates, so `RunAcaBundleAsync` never returns an `AcaMigrationResult`. The handler's `FirewallCleanupFailedMessage` ("could NOT be deleted … Delete it now: az …") is therefore never printed on the exception or cancel path. If the delete also fails there, the `operator-migrate` rule stays open and nobody is told. The Design region and the skill both claim "on failure and Ctrl+C too; if the delete itself fails it prints the delete command". The `ThrowingBundle` test checks only that the delete ran, not this reporting.
- Suggestion: Report a cleanup failure from inside the `finally`, for example through an `onCleanupFailure` callback or by having `RunStepAsync` print the delete command. Or catch the exception, keep it, and rethrow after the result has been reported. Add a test where the bundle throws and the delete fails.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs:132
- Description: The plan line `Runs: <exe> … < web-migrations.sql` uses bash input redirection. pwsh rejects `<` ("reserved for future use"). Task rule: show operator-facing commands in pwsh syntax. The skill already gives the pwsh form (`Get-Content -Raw … | <command>`).
- Suggestion: Print `Get-Content -Raw <script> | <exe> <args>`. Use the full script path, so the line also works from the repo root.
- Status: open

### Issue 5 — Severity: suggestion
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs:198 / tools/dev-cli/services/deploy-operate.cs:380
- Description: The Key Vault connection string, which includes the password, is passed to the bundle as an argv value (`--connection <cs>`). It is never printed, but while the bundle runs it is visible to other local users and tools through `ps` and `/proc/<pid>/cmdline`. This matches the skill's by-hand recipe, so it is not a regression, but the verb could do better than the manual path.
- Suggestion: If the bundle's DbContext factory reads the connection from configuration, pass it through an environment variable on the child process (such as `ConnectionStrings__postgres-db`) instead of argv. Otherwise record the argv exposure as an accepted trade-off in the Design region.
- Status: open

### Issue 6 — Severity: nit
- File: tools/dev-cli/services/aspire-deploy.cs:125
- Description: `PreflightScope.Migrate` sets `RequireParameters: true`. For kubernetes, `dev deploy migrate` therefore refuses unless `helm-release-name`, `registry-endpoint` and `registry-repository` all resolve, but it uses only `k8s-namespace`. An operator in a fresh shell who set those through `Parameters__*` env vars at deploy time gets a refusal about registry settings that migrate never uses.
- Suggestion: Resolve and require only `k8s-namespace` for migrate, the same way `dev open` resolves `ingress-port`.
- Status: open

### Issue 7 — Severity: nit
- File: tools/dev-cli/endpoints/open-command.cs:139-142
- Description: If `localhost:<port>` does not accept a connection within 30 s while kubectl keeps running (slow API server or pod), `WaitForListenerAsync` returns false. The browser is then silently never opened and no URL hint is printed. The forward keeps running with no indication of what to open.
- Suggestion: When the wait times out and the forward is still running, print `NoBrowserMessage(url)` or a "forward not ready yet; open <url>" line.
- Status: open
