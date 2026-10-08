# Round 2 — general
**Date:** 2026-10-09
**Scope reviewed:** fix delta 42f60566b + prior M1–M7

## Summary

All five `fixed` findings are fixed correctly, with no regression. Both `wontfix` rationales hold.
`dev-cli-tests` passes (194/194, Release). `dev deploy --help` now lists both the deploy verb's
options and `migrate`. `--capabilities` still exposes the `deploy` command with its options and
examples, and lists `deploy migrate` separately. The delta introduced two new nits: a misplaced
XML doc summary, and an awkward sentence in a Design region. Neither changes behaviour.

## Prior findings
- M1 — fixed — `ConnectionStringUnreadableMessage` now emits `` `" `` inside the double-quoted `--query`, so pwsh yields `[?tags."aspire-resource-name"=='postgres-kv'].name`. The test asserts the backtick form and that `\"` is absent.
- M2 — fixed — `DeployCommand : DeployGroup` with `[NuruRoute("")]`. `dev deploy --help` shows `[--target,-t {target}] [--yes,-y]` beside `migrate …`, and `deploy migrate --help` is unchanged. The `deploy` / `deploy migrate` routes and their examples in `--capabilities` are intact. Nuru renders the empty route's pattern as `"deploy "` with a trailing space, which is cosmetic and comes from Nuru, not from this change. The Purpose/Design regions of `deploy-group.cs` and `deploy-command.cs` match the code.
- M3 — fixed — `reportCleanupFailure` is invoked inside the `finally` whenever the delete fails (non-zero exit or exception). The command no longer prints `result.CleanupFailure` separately, so the message is not printed twice. The new test `ThrowingBundleAndFailedDelete_Should_StillReportTheCleanupFailure` covers the Ctrl+C path. The XML doc and Design regions in `deploy-operate.cs` and `deploy-migrate-command.cs` describe this behaviour.
- M4 — fixed — the "Runs:" line uses `BuildPipedCommandDisplay` (`Get-Content -Raw '<script>' | <exe> <args>`). `PwshQuote` single-quotes any argument outside `[A-Za-z0-9-_./:=]` and doubles any embedded `'`. The private `Quote` helper is removed, and `RunStepAsync`'s echo uses `PwshQuote`. The display of the psql `sh -c '…"$POSTGRES_PASSWORD"…'` argument is correct pwsh (`$` is literal inside single quotes). The test covers it.
- M5 — wontfix — the rationale holds. `--connection` is the EF bundle's documented interface and the task requires it, the verb is operator-run, and the value is still redacted from output. The trade-off is now recorded in the `deploy-operate.cs` Design region.
- M6 — wontfix — the rationale holds. Since task 288, the template commits the non-secret deploy parameters in AppHost appsettings, so the shared preflight scope costs the operator nothing and keeps "resolved exactly as `dev deploy` resolves them" literal.
- M7 — fixed — after `WaitForListenerAsync` returns false, `ForwardNotReadyMessage(url, 30)` is printed only when the forward is still running and the run was not cancelled. Both a dead forward and Ctrl+C correctly skip it. The timeout is the `ListenerTimeoutSeconds` constant, used by both the wait and the message. The Design region notes the behaviour.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/services/deploy-operate.cs (`BuildPipedCommandDisplay` / `BuildComposeMigrateArguments`)
- Description: the two new members were inserted between `BuildComposeMigrateArguments`' `/// <summary>` (“`<runtime> compose … exec -T postgres` psql for project…”) and its method. As a result, `BuildPipedCommandDisplay` carries two `<summary>` elements, one of which describes the wrong member, and `BuildComposeMigrateArguments` has none. The build does not fail, because this tool does not generate documentation files.
- Suggestion: move the compose `<summary>` line down so it sits directly above `BuildComposeMigrateArguments`.
- Status: open

### Issue 2 — Severity: nit
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs (Design region, step 5)
- Description: "a failed delete prints the command, from the finally, so even when Ctrl+C propagates." is missing a verb. It is also wrapped past the region's usual line width, as is the edited step 4 line and the edited line in the `open-command.cs` Design region.
- Suggestion: "…a failed delete prints the command from inside the finally, so it is printed even when Ctrl+C propagates." Rewrap the edited lines.
- Status: open
