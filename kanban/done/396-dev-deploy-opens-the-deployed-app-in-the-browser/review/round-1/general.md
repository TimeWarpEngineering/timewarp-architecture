# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** `git diff master...HEAD` (head ff9717e14): `tools/dev-cli/endpoints/deploy-command.cs`,
`endpoints/open-command.cs`, `services/deploy-browser.cs` (new), `services/deploy-operate.cs`,
`services/deploy-pipeline.cs`, `tests/tools/dev-cli-tests/deploy-operate-tests.cs`,
`deploy-pipeline-tests.cs`, `skills/tw-deploy/SKILL.md` and three references.

## Summary

`dev deploy` now ends with `AnnounceAsync`, which prints the smoke summary and calls
`DeployBrowser.OpenAsync`, the method `dev open` now calls too. The opener logic moved out of
`OpenCommand` without duplication. The whether-to-open decision (`FollowUpAfterSmoke`,
`ChooseBrowserOpen`, `BrowserLaunchSucceeded`) is pure and tested. Every checklist item and the
restated goal are met. The restatement matches the task and the recorded kubernetes decision
(Steve 2026-10-22), so there is no misread. Re-ran here: dev-cli-tests 315/315 passed, and
`dotnet build tools/dev-cli/dev.cs` exit 0. Risk is low. The two findings are small leftovers
from the move.

## Issues

### Issue 1 — Severity: nit
- File: tools/dev-cli/services/deploy-operate.cs:780
- Description: `NoBrowserMessage` still reads `No browser opener found (or --no-browser); open {url}`.
  After this change `--no-browser` never produces that line, because `ChooseBrowserOpen` returns
  `([], null)` for `--no-browser` and for no terminal. The line is shown only when no launcher is on
  PATH, or when every launcher on PATH failed (`deploy-browser.cs:65`). The `(or --no-browser)`
  clause is now wrong operator text.
- Suggestion: drop the `--no-browser` clause, e.g. `No browser opener worked; open {url}` (this also
  covers the every-launcher-failed case). Keep the test at `deploy-operate-tests.cs:765` that pins
  `open {url}`.
- Status: open

### Issue 2 — Severity: nit
- File: tools/dev-cli/endpoints/deploy-command.cs:420
- Description: in the temporary port-forward path, `followUp?.Hint ?? DeployPipeline.TemporaryForwardStopped(preflight.Target)`
  always evaluates to `TemporaryForwardStopped(target)`. `FollowUpAfterSmoke(..., temporaryPortForward: true, ...)`
  sets `Hint` to exactly that string, and `followUp` is null only when smoke failed, where the fallback
  is the same string. The `SmokeFollowUp? followUp` local and the coalesce make it look like
  two different hints exist.
- Suggestion: print `DeployPipeline.TemporaryForwardStopped(preflight.Target)` in the `finally` as
  before, and use `DeployPipeline.CompleteMessage` (or `FollowUpAfterSmoke(...).Summary` as a local in
  the success branch) for the summary. Drop the nullable local.
- Status: open
