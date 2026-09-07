# Review framework — task 205-002

**Date:** 2026-09-07
**Host task:** kanban/in-progress/205-002-full-iso-language-and-region-catalogs-with-english-ui-fallback/
**Diff scope:** branch `task/205-002-full-iso-language-and-region-catalogs-with-english` vs `origin/master` (commit `33aae301`). Product files:

- `source/container-apps/web/features/profile/profile-details-contracts.cs`
- `source/container-apps/web/features/profile/profile-domain.cs`
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`
- `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor`
- `source/container-apps/web/projects/web-spa/program.cs`
- `tests/container-apps/web/web-domain-tests/profile-tests.cs`
- `documentation/developer/how-to-guides/how-to-progressive-profile-and-agent-human-link.md`
- `tools/dev-cli/services/template-smoke-harness.cs`

Uncommitted `.gitignore` is out of scope (unrelated local hygiene).
**Plan / brief:** Open Language/Region from the 205-001 demo allow-lists to BCL ISO catalogs (`CultureInfo.GetCultures(SpecificCultures)` + distinct ISO 3166-1 alpha-2). Thai (`th-TH` / `TH`) must validate and persist; junk (`en-US asdfasdf`, `US asdf`) still fails. Theme stays `system`/`light`/`dark`. SPA UI culture stays `en-US` (recognize vs apply). Dropdowns: searchable FluentCombobox for Language/Region. Domain repeats BCL checks without referencing contracts.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle session 01a07984-a157-7fe3-a4c1-6535a378f47b (2026-09-07)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
