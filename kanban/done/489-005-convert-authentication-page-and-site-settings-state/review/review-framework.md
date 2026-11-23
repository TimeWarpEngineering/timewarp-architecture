# Review framework

## Budget (by-diff)

- Lines changed: 1301
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Review framework — task 489-005

**Date:** 2026-11-23
**Host task:** kanban/in-progress/489-005-convert-authentication-page-and-site-settings-state/
**Diff scope:** branch `task/489-005-convert-authentication-page-and-site-settings` vs `182192de9` (tip of `origin/feature/edit-forms-whole-object` merged in at `7914ef1e5`); implementation commit `d24afebf9`. 20 files, +923/−378.
**Plan / brief:** `task.md` Description and `## Goal (implementer restatement)`: store holds `GetSiteSettings.Response` whole, `DraftAcceptedTenants` leaves the store, the Authentication page edits `UpdateSiteSettings.Command.From(SiteSettings)` with Cancel, readers move to the response.
**Effort:** 3 (by-diff budget); roster axes from the budget: general only
**Reviewer roster:** general
**Session IDs:** review oracle Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work` review node

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
