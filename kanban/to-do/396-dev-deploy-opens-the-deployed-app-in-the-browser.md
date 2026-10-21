# dev deploy opens the deployed app in the browser

## Description

Task 393 made `dev deploy --target <t>` the one-command first deploy: publish, deploy, migrate,
smoke, print the URL. It stops at printing, so the user still runs `dev open` or copies the URL.
For a template, the pit of success is landing in the running app.

After a successful smoke, `dev deploy` opens the app the way `dev open` does, using the same
opener and fallback (`wslview`, then `explorer.exe` on WSL; prints the URL when no opener is on
PATH or there is no terminal). `--no-browser` prints the URL only. `--yes` does not change it.

## Checklist

- [ ] `dev deploy` ends by opening the ingress URL through the same code path `dev open` uses (reuse it, do not duplicate)
- [ ] `--no-browser` prints the URL and opens nothing
- [ ] No terminal, or no opener on PATH: prints the URL (no failure)
- [ ] kubernetes without an external address: decide with Steve whether deploy starts the foreground port-forward or prints the `dev open --target kubernetes` hint (default proposal: print the hint, since a foreground forward would block the command)
- [ ] dev-cli tests: open after smoke, `--no-browser`, no-opener fallback
- [ ] `tw-deploy` skill: the first-deploy description says it opens the app, and mentions `--no-browser`

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "so dev open isn't included I guess" during the aca-neon test after 393.
- Code: `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/deploy-pipeline.cs`, the `dev open` endpoint.
