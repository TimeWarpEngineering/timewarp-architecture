# Shared form layout: dense sections, full-width fields, right-aligned actions; apply to Profile, RoleForm, and Authentication

## Description

Steve, 2026-09-16 late (screenshot of `/Profile`): the profile form wastes vertical space
(large gaps between every field), the inputs are narrow and staggered in width (Display name /
Email one width, Language / Region another, Theme a third), and the Save sits alone bottom-left.
Reference look supplied: the Tailwind UI "form layout" (stacked sections, each with a heading and
one-line description, a bottom hairline, a tight label → control rhythm, fields in a responsive
grid, Cancel + Save right-aligned at the end). **We are not adopting Tailwind.** Reproduce the
rhythm with our tokens and Fluent components.

Two explicit deviations from the reference: **inputs span the full content width** (no
`col-span-4`/`col-span-3` staggering; a field is full width unless two short fields genuinely
pair on one row, e.g. City / State / ZIP), and everything must stay on the shared components —
no page-local CSS.

## Requirements

### A. Shared layout primitives (`components/forms/`)

- `FormSection` — parameters `Title`, `Description`, `ChildContent`. Renders heading
  (`--twe-text-section-title`, 600), one-line muted description, then the fields; sections are
  separated by a hairline (`border-bottom: 1px solid var(--twe-border)` or the closest existing
  token) and a section gap token.
- `FormGrid` (or a modifier on `FormSection`) — fields laid out on a 12-column grid; default a
  field takes the full row; `Span` parameter (1–12) for the rare paired fields; collapses to one
  column under ~640px.
- `FormField` — label above control, `Hint` (muted, below), consistent label → control gap.
  Wraps `FluentTextInput` / `FluentSelect` / `FluentCheckbox` / `FluentTextArea` and forces
  `Width="100%"` / `style="width:100%"` so controls stop being staggered.
- `FormActions` — right-aligned row (`justify-content:flex-end; gap: var(--twe-space-3)`),
  Cancel = `FluentButton Appearance.Subtle` (or Transparent per style guide), primary = Submit
  Primary. Update the existing `FormContainer` to use `FormActions` for its `ActionContent`.
- Spacing tokens in `wwwroot/css/tokens.css` (add if missing; reuse if present):
  `--twe-space-2: 8px` (label→control), `--twe-space-6: 24px` (field row gap),
  `--twe-space-12: 48px` (section gap), `--twe-space-3: 12px` (action gap). Map the reference:
  Tailwind `mt-2` → space-2, `gap-y-8` (32px) → use 24–32 (pick one, document), `pb-12` /
  `space-y-12` → space-12.

### B. Apply

- `features/profiles/pages/ProfilePage.razor`: one `FormSection` "Profile" (avatar, display
  name, email) and one "Preferences" (language, region, theme, notifications); all controls
  full width; `FormActions` with Save (and Cancel that resets the draft if `ProfileState` has a
  cheap reset — otherwise Save only, say so). Keep TWA0022 state flow and `data-qa` hooks.
- `features/admin/roles/components/RoleForm.razor`: same primitives (it is the current reference
  form; it must not regress visually).
- `features/admin/site-settings/pages/AuthenticationPage.razor` (231): wrap its fields in a
  `FormSection` and use `FormActions`.
- `StyleGuidePage.razor`: add a "Forms" section showing `FormSection` + `FormField` + `FormActions`
  with the spacing tokens named, so future pages copy from there.

### C. Rule

- `skills/tw-blazor/SKILL.md`: add "Forms use FormSection / FormField / FormGrid / FormActions
  from `components/forms`; controls are full width by default; spacing via the `--twe-space-*`
  tokens; no page-local margins". Keep the 233 actions rule intact.

### Tests

- Prerender/SPA tests for Profile, Roles, Authentication still pass (selectors by `data-qa`).
- A small render test for `FormField` asserting the control gets full width and the hint renders.

## Depends on

- 233

## Checklist

- [x] `FormSection` / `FormGrid` / `FormField` / `FormActions` + spacing tokens
- [x] Profile, RoleForm, Authentication on the primitives; full-width controls; right-aligned actions
- [x] Style guide "Forms" section
- [x] tw-blazor forms rule
- [x] Tests green; `dev build` 0/0; `ganda repo audit` clean
- [x] Results and How to validate (before/after screenshot steps for `/Profile`)
- [x] Implementation review: round 1 findings fixed; round 2 clean; disposition clean

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok (2026-09-17) — task-234 worktree
- Review oracle: Grok (2026-09-17) — tw-implementation-review effort 1, rounds 1–2

## Notes

- Current `ProfilePage.razor`: `EditForm` → `FluentStack Orientation=Vertical VerticalGap="16"`,
  inputs with `style="width:100%"` / `Width="100%"` that still render narrow (FluentUI v5
  controls size to content unless the wrapper is block/full width — the `FormField` wrapper must
  make that explicit). `FormContainer.razor` exists with inline styles; fold it onto the new
  primitives rather than leaving two systems.
- Reference markup is Tailwind UI "Form layouts › Stacked" (supplied in chat); use it for rhythm
  only. Do **not** add Tailwind.
- Prior: 231 (Authentication EditForm), 233 (actions rule + Settings rebuild).

## Results

Shared form primitives live under `web-spa/components/forms/`: `FormSection`, `FormGrid`,
`FormField`, `FormActions`. `FormContainer` now renders `ActionContent` through `FormActions`
and uses token-based CSS instead of inline styles.

Spacing tokens in `wwwroot/css/tokens.css`: `--twe-space-2` 8px (label→control),
`--twe-space-3` 12px (action gap), `--twe-space-6` 24px (field row gap), `--twe-space-12`
48px (section gap). Hairline uses `--twe-rule` (no `--twe-border`). Field-row gap is 24px
(Fluent’s recommended 24px between fields; Tailwind `gap-y-8` is 32px — 24 is the pick,
documented on the token and on the Style Guide Forms card).

**Full-width:** `FormField` is a block wrapper at `width: 100%` so FluentUI v5 hosts no
longer size to content. Pages also pass `Width="100%"` on Fluent controls. No inline
`style="width:100%"` (CSP / `tw-blazor-css-strategy`). Default grid span is 12; `Span` 1–12
for genuine pairs; one column under 640px.

**Apply**

- `/Profile`: FormSection “Profile” (avatar, display name, email) and “Preferences”
  (language, region, theme, notifications). `FormActions` Cancel (Subtle) + Save (Primary).
  Cancel calls `SyncCommandFromState` — cheap draft reset from `ProfileState`, no extra
  action-set. TWA0022 and `data-qa` (`ProfileAlias`, `ProfileEmail`, `ProfileLanguage`,
  `ProfileRegion`, `ProfileTheme`, `ProfileSave`) kept; added `ProfileCancel`.
- `RoleForm`: same primitives. Save only — `CreateRole.Command` has no cheap unused reset.
  `data-qa` `RoleName` / `RoleDescription` / `RoleSave` kept. RolePage Card title dropped
  so FormSection “Role” is the heading.
- `/Admin/Authentication`: FormSection + FormGrid + FormField + FormActions. Native
  `<select>` replaced with `FluentSelect`. Page-local `twe-settings__*` CSS deleted.
  Tenant line and drift banner stay outside the EditForm. Save only (`data-qa`
  `AuthenticationSave`, `AuthenticationSettings`, `ConfigurationTenant` kept).
- Style Guide: Forms card names the tokens and shows FormSection + FormField + FormActions.
- `skills/tw-blazor/SKILL.md`: forms rule added; 233 FluentButton / EditForm rule intact.

**Tests:** `dev build` 0/0. `ganda repo audit` passes (2 pre-existing advisory warnings:
memsearch-scaffold, vscode-window-icon). FormField HtmlRenderer test: hint +
`twe-form-field--span-12` + Exception B Fluent host selectors. SPA unit tag 25/25. Prerender
`Ok_Page_Given_Passkey_Administrator_{Admin_Authentication,Admin_Roles}_Html` 2/2.

**Not in this session:** live `/Profile` screenshot (AppHost was not running; starting
Aspire would lock the just-built outputs). Follow How to validate for the before/after look.

**Review (effort 1, general only; 2 rounds)**

- Roster: general. Round 1 raised M1 (bug, FormField isolation vs Fluent host width),
  M2 (bug, FormActions + FormContainer double `--twe-space-6`), M3 (suggestion,
  Authentication FormActions inside FormSection). All fixed on this task id.
  Round 2 re-verified M1–M3; 0 new findings.
- Final counts: bug 0 open / 2 fixed / 0 wontfix; suggestion 0 / 1 / 0; nit 0 / 0 / 0.
- **Disposition:** clean (0 open).
- Paths: `review/review-framework.md`, `review/round-1/merged.md`,
  `review/round-2/merged.md`, `review/disposition.md`.

Review follow-up: Exception B `<style>` on `FormField` stretches Fluent field hosts
(v5 wrap is `fluent-field`); FormActions has no `margin-top`; Authentication
FormActions sits after FormSection with tenant/drift outside EditForm.

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-class FormField_Should_
# expect: passed, total 1

cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-method Ok_Page_Given_Passkey_Administrator
# expect: passed, total 2 (Authentication + Roles prerender; data-qa AuthenticationSettings / NewRole)

ganda repo audit
# expect: Repository passes (advisory warnings only)
```

**UI (`/Profile` before/after)**

1. `dotnet run tools/dev-cli/dev.cs -- run` (or `aspire start --isolated --non-interactive --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj`).
2. Sign in, open `/Profile`.
3. **Expect:** two sections “Profile” and “Preferences”, each with a one-line muted
   description and a bottom hairline; Display name / Email / Language / Region / Theme
   each span the full content width (no staggered Fluent widths); Notifications on its
   own full row; Cancel (Subtle) + Save (Primary) right-aligned; `data-qa="ProfileSave"`
   still present.
4. Change Display name, click Cancel. **Expect:** the draft reverts to the last fetched
   or saved profile (no PUT).
5. Optional: `/Admin/Roles/New` — Name and Description full width, Save right-aligned
   (`data-qa="RoleSave"`). `/Admin/Authentication` — FormSection “Microsoft 365 sign-in
   policy”, FluentSelect for passkey prompt, Save right-aligned
   (`data-qa="AuthenticationSave"`). `/StyleGuide` Forms card names `--twe-space-2/3/6/12`.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release -- --filter-tag Unit
```

**Depends on:** signed-in session with `profile.read` for `/Profile`; `admin.roles.manage`
for New Role; `settings.write` for Authentication; `developer.access` for Style Guide.

**Not in scope:** Tailwind; page-local form CSS; pairing short fields on Profile (none
qualify as City/State/ZIP).

