# Review framework — task 233

**Date:** 2026-09-17
**Host task:** kanban/in-progress/233-settings-page-rebuild-on-the-shared-fluent-components-delete-hand-rolled-markup-and-make-actions-are-fluentbutton-the-rule/
**Diff scope:** branch `task/233-settings-page-rebuild-on-the-shared-fluent-compone` vs merge-base `origin/master` (`c546195c`). Product commit `edd0533e` feat: rebuild Settings on FluentButton and shared CredentialList; results commit `9e402490`.
**Plan / brief:** See `task.md` Requirements: rebuild `/Settings` on shared `Card` / `Text` / `StatusBadge` + `FluentButton` (Primary / Outline / Outline+`--twe-danger`); extract shared credential list used by Settings and PasskeysPage; style-guide danger example; replace the 231 tw-blazor “link-styled buttons” rule; raw `<button` guard under `web-spa/features`; preserve 229/230 behaviour; tests select `data-qa` (`CreatePasskey`, `AddExistingPasskey`, `LinkMicrosoft365`, `Unlink`, `DeletePasskey`).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0ab3b-770a-7d03-b5c6-c9500e96b209 (2026-09-17). General reviewer (round 1): grok session 01a0ab3f-6952-71d1-9f70-f6013aea9b63 (2026-09-17).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
