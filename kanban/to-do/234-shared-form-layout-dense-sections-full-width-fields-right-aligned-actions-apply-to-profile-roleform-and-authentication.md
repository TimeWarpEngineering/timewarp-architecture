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

- [ ] `FormSection` / `FormGrid` / `FormField` / `FormActions` + spacing tokens
- [ ] Profile, RoleForm, Authentication on the primitives; full-width controls; right-aligned actions
- [ ] Style guide "Forms" section
- [ ] tw-blazor forms rule
- [ ] Tests green; `dev build` 0/0; `ganda repo audit` clean
- [ ] Results and How to validate (before/after screenshot steps for `/Profile`)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

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

_Pending._

### How to validate

_Pending._
