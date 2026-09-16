# Review framework — task 231

**Date:** 2026-09-16
**Host task:** kanban/in-progress/231-admin-authentication-page-use-the-editform-and-fluentbutton-form-pattern-document-form-vs-inline-action-rule-in-tw-blazor/
**Diff scope:** branch `task/231-admin-authentication-page-use-the-editform-and-flu` vs merge-base `origin/master` (`09993048`). Product commit `e8f75802` feat(web-spa): use EditForm and FluentButton on Admin Authentication; results commit `f18669f1`.
**Plan / brief:** See `task.md` Requirements: (1) `/Admin/Authentication` wraps policy fields in `EditForm` bound to a small edit model (`ISiteSettingsDetails` / `UpdateSiteSettings.Command` including Version), `OnValidSubmit` → existing `SiteSettingsState` update action; Save is `FluentButton Type=ButtonType.Submit Appearance=ButtonAppearance.Primary` with `data-qa="AuthenticationSave"`, disabled while busy; keep the read-only app-registration tenant line and Enabled/AllowBootstrap drift banner outside the form; match RoleForm layout/spacing; CSS per `tw-blazor-css-strategy`. (2) Add a "Form vs inline action" rule to repo `skills/tw-blazor/SKILL.md` (or record SSOT in timewarp-flow if managed). (3) Update SPA/prerender tests from 225 that click the save control (`data-qa`). Out of scope: personal `/Settings` row actions (task 229).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0aae0-033d-7e42-8eb9-34428c1af215 (2026-09-16). General reviewer (round 1): grok session 01a0aae5-395b-7211-9205-bd0fdb7aca4a (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
