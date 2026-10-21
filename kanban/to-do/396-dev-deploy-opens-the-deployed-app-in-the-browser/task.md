# dev deploy opens the deployed app in the browser

## Goal (implementer restatement)

After smoke succeeds on a reachable ingress, `dev deploy` opens that URL with the same launcher as `dev open` (`wslview`, then `explorer.exe` on WSL, then the platform opener). `--no-browser` prints the URL and starts nothing. `--yes` only skips the deploy prompt. No terminal, or no opener on PATH, prints the URL and exits successfully. Kubernetes with no external address does not keep a foreground port-forward and does not open a browser: the command prints the smoke summary, then `TemporaryForwardStopped` (which names `dev open --target kubernetes`) from `finally`. Tests lock open-after-smoke, `--no-browser`, the no-opener fallback, and that stopped-forward line. `tw-deploy` says the first deploy opens the app and names `--no-browser`.

This pass clears review round 2 suggestion M3. `FollowUpAfterSmoke` and `AnnounceAsync` decide only whether a reachable ingress opens. They take no temporary-port-forward flag, and `SmokeFollowUp` has no hint. The kubernetes test asserts `TemporaryForwardStopped` directly. The Design region, the XML docs, and Results describe that split.

## Description

Task 393 made `dev deploy --target <t>` the one-command first deploy: publish, deploy, migrate,
smoke, print the URL. It stops at printing, so the user still runs `dev open` or copies the URL.
For a template, the pit of success is landing in the running app.

After a successful smoke, `dev deploy` opens the app the way `dev open` does, using the same
opener and fallback (`wslview`, then `explorer.exe` on WSL; prints the URL when no opener is on
PATH or there is no terminal). `--no-browser` prints the URL only. `--yes` does not change it.

## Checklist

- [x] `dev deploy` ends by opening the ingress URL through the same code path `dev open` uses (reuse it, do not duplicate)
- [x] `--no-browser` prints the URL and opens nothing
- [x] No terminal, or no opener on PATH: prints the URL (no failure)
- [x] kubernetes without an external address: print the `dev open --target kubernetes` hint and finish (no foreground port-forward). Decided: Steve 2026-10-22
- [x] dev-cli tests: open after smoke, `--no-browser`, no-opener fallback
- [x] `tw-deploy` skill: the first-deploy description says it opens the app, and mentions `--no-browser`
- [x] M1: `NoBrowserMessage` is `No browser opener worked; open {url}` and does not name `--no-browser`
- [x] M2: the temporary kubernetes forward prints `TemporaryForwardStopped` in `finally`; the success branch uses a non-null `CompleteMessage` local
- [x] M3: `FollowUpAfterSmoke` and `AnnounceAsync` take no temporary-port-forward flag; `SmokeFollowUp` has no hint; the kubernetes test asserts `TemporaryForwardStopped`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: 2026-10-22. After smoke, `dev deploy` calls `DeployBrowser.OpenAsync`, the opener `dev open` now uses too. `--no-browser` and a missing terminal print the URL. Kubernetes without an external address still smokes through a cancelled port-forward and prints the `dev open` hint.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T17:37:26Z
- Review round 1 (2026-10-22): review oracle (claude-opus-5-5), effort 2, roster general. 0 bugs, 2 open nits (M1, M2). A fix pass is requested. `review/round-1/merged.md`
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-21T17:39:52Z
- Fix pass (2026-10-22): M1 drops the `--no-browser` clause from `NoBrowserMessage`. M2 prints `TemporaryForwardStopped` in `finally` and uses `CompleteMessage` for the success summary.
- Review round 2 (2026-10-22): review oracle (claude-opus-5-5), effort 1, roster general, turn cap 80. M1 and M2 are fixed. M3 (suggestion) is open, so a fix pass is requested. `review/round-2/merged.md`
- Fix pass (2026-10-22): M3 drops `temporaryPortForward` from `FollowUpAfterSmoke` and `AnnounceAsync`, drops `Hint` from `SmokeFollowUp`, and asserts `TemporaryForwardStopped` directly. Design, XML docs, and Results match that split.
- Review round 3 (2026-10-22): review oracle (claude-opus-5-5), effort 1, roster general, turn cap 80. M3 is fixed and nothing new was found. Disposition clean. `review/round-3/merged.md`, `review/disposition.md`

## Results

`dev deploy` opens a reachable ingress after a successful smoke through `services/deploy-browser.cs`. `dev open` calls that same method. Whether that path opens is `DeployPipeline.FollowUpAfterSmoke` (`OpenBrowser` and the summary) together with `DeployOperate.ChooseBrowserOpen`. `--yes` only skips the prompt. A kubernetes smoke that needed a temporary port-forward does not call `FollowUpAfterSmoke`. It prints `CompleteMessage`, then `TemporaryForwardStopped` from `finally` (that line names `dev open --target kubernetes`), and it does not open a browser. `skills/tw-deploy` says the first deploy opens the app and names `--no-browser`. Kind has no external address, so that recipe says deploy prints the hint.

Review round 1 left two nits. `NoBrowserMessage` is now `No browser opener worked; open {url}`. `--no-browser` still prints the URL and does not print that line. The temporary kubernetes forward prints `DeployPipeline.TemporaryForwardStopped` in `finally`. The success branch prints `CompleteMessage` from a non-null local.

Review round 2 left suggestion M3. `temporaryPortForward` is gone from `FollowUpAfterSmoke` and `AnnounceAsync`. `SmokeFollowUp` is `(OpenBrowser, Summary)` with no hint. The kubernetes test asserts `TemporaryForwardStopped` directly.

`ganda repo audit`: 32 passed, 0 failed, 1 skipped.

### How to validate

Smoke:

```bash
cd tests/tools/dev-cli-tests
dotnet test -c Release -- --filter-class FollowUp
dotnet test -c Release --no-build -- --filter-class Browser_Given
dotnet test -c Release --no-build
cd ../../..
dotnet build tools/dev-cli/dev.cs --nologo -v q
```

Expect:

- `FollowUp` reports 3 passed: reachable compose, kubernetes LoadBalancer, and ACA open after smoke; `--no-browser` keeps `OpenBrowser` false and the confirm prompt says `print the URL`; `TemporaryForwardStopped` for kubernetes contains `dev open --target kubernetes` and `temporary port-forward`.
- `Browser_Given` reports 7 passed: no opener returns `No browser opener worked; open {url}` and an empty launcher list; no terminal and `--no-browser` return no launchers and no fallback; WSL order is `wslview`, then `explorer.exe`.
- The full dev-cli-tests run reports 315 passed.
- `dotnet build tools/dev-cli/dev.cs` exits 0.

Ran here on 2026-10-22 after the M3 fix pass: FollowUp 3/3, Browser_Given 7/7, full suite 315/315, `dotnet build tools/dev-cli/dev.cs` exit 0, `ganda repo audit` 32 passed, 0 failed, 1 skipped. No live cluster deploy in this walk.

### Review

- Round 1 (effort 2, general): bug 0, suggestion 0, nit 2 (M1, M2). The fix pass implemented both.
- Round 2 (effort 1, general, fix delta `a129b32a9..7bca85a38`): M1 and M2 are fixed. One suggestion was open: M3. The fix pass took option (a): no `temporaryPortForward` parameter, no `SmokeFollowUp.Hint`, and the kubernetes test checks `TemporaryForwardStopped` directly. Counts at review time: bug 0, suggestion 1 open, nit 2 fixed.
- Round 3 (effort 1, general, fix delta `67075b649..d49e6a38e`): M3 is fixed and nothing new was found. Re-ran dev-cli-tests (315/315) and `dotnet build tools/dev-cli/dev.cs` (exit 0).
- Final counts: bug 0, suggestion 0 open / 1 fixed, nit 0 open / 2 fixed, 0 wontfix.
- Disposition: **clean**. All three findings are fixed.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/merged.md`, `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Review

- Round 1: Blocked — open M1, M2
- Round 2: Blocked — open M3
- Round 3: Done
## Notes

- Origin: Steve 2026-10-21, "so dev open isn't included I guess" during the aca-neon test after 393.
- Code: `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/deploy-pipeline.cs`, the `dev open` endpoint.
