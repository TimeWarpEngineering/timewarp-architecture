# Review framework — task 229

**Date:** 2026-09-16
**Host task:** kanban/in-progress/229-settings-microsoft-365-card-one-linked-account-hide-link-when-linked-disable-unlink-when-it-is-the-last-credential-label-with-the-account/
**Diff scope:** branch `task/229-settings-microsoft-365-card-one-linked-account-hid` vs merge-base `origin/master` (`5a2073d7`). Product commit `ffad1e05` feat(identity): hide M365 link when linked and label the Settings card.
**Plan / brief:** See `task.md` Requirements: (1) one active EntraAccount per principal — hide Link when linked; server link of a second active EntraAccount is 409 `Microsoft 365 already linked`; (2) Unlink disabled with hint "Add a passkey first" when last active credential; server LastCredential 409 remains the backstop; (3) credential Label from `preferred_username` else `name` else fallback; card title = label, subtitle = "Microsoft 365". Out of scope: multi-tenant; storing tokens; passkey Delete last-credential UX.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0aa68-6f0f-77c1-9538-7770f499edf1 (2026-09-16). General reviewer (round 1): grok session 01a0aa6b-c811-7b50-a12d-4f28217b3463 (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
