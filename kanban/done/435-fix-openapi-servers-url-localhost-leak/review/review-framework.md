# Review framework

## Budget (by-diff)

- Lines changed: 0
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 1 framework — task 435

**Date:** 2026-10-29
**Host task:** kanban/in-progress/435-fix-openapi-servers-url-localhost-leak/
**Diff scope:** branch `task/435-fix-openapi-servers-url-localhost-leak` vs `master` (merge-base), product paths `source/foundation/foundation-server/`, `tests/container-apps/web/web-server-integration-tests/`
**Plan / brief:** task.md `## Goal (implementer restatement)` — the OpenAPI `servers` entry follows the forwarded public origin; Scalar adds no server list
**Effort:** 2 (budget by diff)
**Reviewer roster:** general (review oracle, claude-opus-5-5, inline)
**Session IDs:** review oracle under ganda task work (headless print mode)

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 framework — task 435

**Date:** 2026-10-29
**Diff scope:** fix-pass delta since round 1 (`review/round-1/base.sha` = `69576b371`, working tree), product paths `Directory.Packages.props`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`, `source/foundation/foundation-server/`, `source/container-apps/web/platform/mail/`, `source/container-apps/web/projects/web-server/configuration/link-preview.cs`, and the two touched test files
**Plan / brief:** `review/round-1/merged.md` M1 and M2, plus the Goal restatement's new version and one-resolver bullets
**Effort:** 1 (budget by diff)
**Reviewer roster:** general (review oracle, claude-opus-5-5, inline)
**Session IDs:** review-2 oracle under ganda task work (headless print mode)
