# Review framework — task 205-001

**Date:** 2026-09-06
**Host task:** kanban/in-progress/205-001-reject-invalid-profile-language-bcp-47--culture-name/
**Diff scope:** branch `task/205-001-reject-invalid-profile-language-bcp-47-culture-nam` vs `origin/master` (commits `57224564`, `71854ead`). Product files:

- `source/container-apps/web/features/profile/profile-details-contracts.cs`
- `source/container-apps/web/features/profile/profile-domain.cs`
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`
- `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor`
- `tests/container-apps/web/web-domain-tests/profile-tests.cs`

Uncommitted `.gitignore` is out of scope (unrelated local hygiene).
**Plan / brief:** Close the `/Profile` Language / Region / Theme free-text hole. Bind Fluent UI v5 `FluentSelect` to a curated `ProfileCatalog` in contracts; `ProfileDetailsValidator.Must` membership; domain `Create` / setters / `Invariants` duplicate the code sets so a store write cannot bypass. Reject `en-US asdfasdf`, `US asdf`, unknown theme; accept defaults `en-US` / `US` / `system`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle (2026-09-06)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
