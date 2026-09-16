# Review framework — task 234

**Date:** 2026-09-17
**Host task:** kanban/in-progress/234-shared-form-layout-dense-sections-full-width-fields-right-aligned-actions-apply-to-profile-roleform-and-authentication/
**Diff scope:** branch `task/234-shared-form-layout-dense-sections-full-width-field` vs `origin/master` (commits `32f2fdfb` feat + `c90e0576` docs). Product paths:

- `source/container-apps/web/projects/web-spa/components/forms/` (`FormSection`, `FormGrid`, `FormField`, `FormActions`, `FormContainer`)
- `source/container-apps/web/projects/web-spa/features/profiles/pages/ProfilePage.razor` (+ `.cs`)
- `source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor`
- `source/container-apps/web/projects/web-spa/features/admin/roles/pages/RolePage.razor`
- `source/container-apps/web/projects/web-spa/features/admin/site-settings/pages/AuthenticationPage.razor` (+ `.cs`)
- `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor`
- `source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css`
- `source/container-apps/web/projects/web-spa/components/overview.md`
- `skills/tw-blazor/SKILL.md`
- `tests/container-apps/web/web-spa-integration-tests/features/application/form-field-render-tests.cs`

**Plan / brief:** Shared form layout primitives (dense sections, full-width fields, right-aligned actions) applied to Profile, RoleForm, and Authentication. Spacing tokens `--twe-space-2/3/6/12`. No Tailwind. No page-local form CSS. Keep TWA0022 and `data-qa` hooks.

**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle (2026-09-17) — task-234 worktree
**Rounds:** round-1 (findings) → fix on same task id → round-2 (re-verify M1–M3 + fix delta)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
