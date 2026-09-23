# Review framework — task 058-001

**Date:** 2026-09-22
**Host task:** kanban/to-do/058-001-harden-web-spa-test-host-and-upgrade-timewarp-state/
**Diff scope:** branch `task/058-001-harden-web-spa-test-host-and-upgrade-timewarp-stat` vs `origin/master` (`ff83a5c3..12cfb442`). Product diff is commit `12cfb442` (web-spa message bars, test-assembly rename removal, SPA test host weather fetch). Commit `4672e102` is the kanban rescope only.
**Plan / brief:** Remove the three task-058 web-spa workarounds that remained after TimeWarp.State 12.0.0-beta.3 (task 237) and the epic-145 test host: delete the `web-spa-integration-Tests` assembly rename, move exception and problem-details notifications onto `FluentMessageBar` rows in `ToastNotificationState` (no `IToastService`, no test-host handler removal), and un-skip the weather fetch by pointing the SPA `HttpClient` at Aspire ingress with `MockAccessTokenProvider`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a0c8ad-9504-7392-896d-5f8b9eff692b`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-09-22
**Session:** grok `01a0c8c8-5d89-7543-89ce-9db5ac7c3f37`
**Diff scope:** Same product commit `12cfb442` vs `origin/master`. Commits after it (`6609ac61`, `d0522d46`, `714cd7a1`) are kanban-only. The folder now lives under `kanban/done/` (moved after round 1).
**Re-check:** Handlers do not `Send` or call `INotificationService`. `StateTransactionBehavior` in TimeWarp.State 12.0.0-beta.3 restores state, then publishes `ExceptionNotification`. `TestCaller.Ensure` accepts `test` ordinal-ignore-case. Weather fetch is not skipped; ingress HTTP client and `MockAccessTokenProvider` registration remain.
