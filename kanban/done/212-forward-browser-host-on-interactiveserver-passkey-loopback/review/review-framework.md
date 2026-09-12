# Review framework — task 212

**Date:** 2026-09-12
**Host task:** kanban/in-progress/212-forward-browser-host-on-interactiveserver-passkey-loopback/
**Diff scope:** branch `task/212-forward-browser-host-on-interactiveserver-passkey` vs `origin/master` (commit `afc10441` product change; kanban brief/folderize excluded from product review)
**Plan / brief:** InteractiveServer/Auto named `WebService` HttpClient loopback must present the inbound browser Host (YARP-preserved, port stripped) so passkey RP-ID selection matches the authenticator. Do not consume `X-Forwarded-Host`. Log `WebAuthnAssertionResult.FailureReason` on authenticate verify 400. Tests for Host copy / no-overwrite and Verify fail/succeed when selected RP is localhost vs `arch.timewarp.work`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a0966a-d4db-7961-a6fb-9e4fdc1eb9d9 (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
