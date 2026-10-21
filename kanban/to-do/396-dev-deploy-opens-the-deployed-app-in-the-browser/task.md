# dev deploy opens the deployed app in the browser

## Goal (implementer restatement)

After smoke succeeds, `dev deploy` should land the operator in the running app. It opens the ingress URL with the same launcher `dev open` uses (`wslview`, then `explorer.exe` on WSL, then the platform opener). `--no-browser` prints that URL and starts nothing. `--yes` only skips the deploy prompt; it does not change the browser. When there is no terminal, or no opener on PATH, the command prints the URL and exits successfully. Kubernetes with no external address does not start a foreground port-forward: it prints the existing hint to run `dev open --target kubernetes` and finishes. Tests lock open-after-smoke, `--no-browser`, and the no-opener fallback. The `tw-deploy` first-deploy description says the command opens the app and names `--no-browser`.

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

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: 2026-10-22. After smoke, `dev deploy` calls `DeployBrowser.OpenAsync`, the opener `dev open` now uses too. `--no-browser` and a missing terminal print the URL. Kubernetes without an external address still smokes through a cancelled port-forward and prints the `dev open` hint.

## Results

`dev deploy` opens the ingress after a successful smoke through `services/deploy-browser.cs`. `dev open` calls that same method. The decision of whether to open lives in `DeployPipeline.FollowUpAfterSmoke` and `DeployOperate.ChooseBrowserOpen`, which the dev-cli tests compile. `--yes` only skips the prompt. A kubernetes smoke that needed a temporary port-forward prints the URL and `dev open --target kubernetes`, then stops the forward. It does not leave a foreground forward and it does not open a browser. `skills/tw-deploy` says the first deploy opens the app and names `--no-browser`. Kind has no external address, so that recipe says deploy prints the hint.

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

- `FollowUp` reports 3 passed: reachable compose, kubernetes LoadBalancer, and ACA open after smoke; `--no-browser` keeps `OpenBrowser` false and the confirm prompt says `print the URL`; kubernetes with a temporary port-forward sets `OpenBrowser` false and the hint contains `dev open --target kubernetes`.
- `Browser_Given` reports 7 passed: no opener returns `NoBrowserMessage` and an empty launcher list; no terminal and `--no-browser` return no launchers and no fallback; WSL order is `wslview`, then `explorer.exe`.
- The full dev-cli-tests run reports 315 passed.
- `dotnet build tools/dev-cli/dev.cs` exits 0.

Ran here on 2026-10-22: FollowUp 3/3, Browser_Given 7/7, full suite 315/315, `dotnet build tools/dev-cli/dev.cs` exit 0. No live cluster deploy in this walk.

## Notes

- Origin: Steve 2026-10-21, "so dev open isn't included I guess" during the aca-neon test after 393.
- Code: `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/deploy-pipeline.cs`, the `dev open` endpoint.
