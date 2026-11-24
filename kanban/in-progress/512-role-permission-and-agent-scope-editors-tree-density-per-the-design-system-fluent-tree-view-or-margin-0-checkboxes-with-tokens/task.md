# Role permission and agent-scope editors: tree density per the design system (FluentTreeView or Margin=0 checkboxes with tokens)

## Description

`/Admin/Roles/{RoleId}` renders permissions as parent/child checkbox groups through
`RolePermissionEditor.razor` (feature-local, `features/admin/roles/components/`, commit
`3990f0acd`, task 206). Rows sit about 53px apart. The page CSS is not the cause: its gaps are
4px and 6px. The cause is `FluentCheckbox`, which carries its own default outer margin on every
instance. Every stacked example in the FluentUI v5 docs passes `Margin="0"`; the editor does
not, on 15 group rows and children. The agent-scope editor on the same page has the same
construction.

Two design-system problems, found by Steve on 2026-11-24:

1. **Density.** A tree of 40 short labels should read as a tree, not a form of separate fields.
2. **Not on the design system.** `RolePermissionEditor` is a hand-built control that is not in
   the `/DesignSystem` catalogue. The catalogue has a `Permission grid`
   (`components/elements/PermissionGrid.razor`), but that one is read-only ("wires no events and
   does not decide the grants") and no product page uses it. There is no editable permission or
   scope control in the design system at all, so the feature invented one locally. The raw CSS
   in `RolePermissionEditor.razor.css` also uses `4px`, `6px`, `8px`, `16px`, `28px` literals
   instead of `--twe-space-*` tokens.

## Depends on

- 508

## Requirements

### One editable tree control in the design system

- Add `PermissionTree` under `components/elements/` next to `PermissionGrid`, with a
  `PermissionTreeSample` and a catalogue entry in `design-system-parts.cs` (group Identity,
  beside Permission grid). It takes groups of items, a selected set, a locked set (protected
  core), and raises one change event with the new selected set. It decides nothing about grants.
- Implementation choice, in this order, recorded in the component's Design region:
  1. `FluentTreeView` with `SelectionMode="TreeSelectionMode.MultipleRecursive"`,
     `Size="TreeSize.Small"`, `HideSelection="true"`, items from the groups. Parent three-state
     comes free. Locked items must stay checked and unclickable: use
     `MultipleSelectionVisibility` or a `Disabled` item and verify the recursive helper does not
     flip them. If locked items cannot be expressed, fall through.
  2. The current grouped `FluentCheckbox` layout, every checkbox `Margin="0"`, parent row and
     children in a native list with `gap: var(--twe-space-1)`, child indent `--twe-space-6`,
     group gap `--twe-space-3`, no literal px.
- Target density: child rows no taller than 32px, parent rows 36px, measured in the screenshot.
- Keyboard: arrow keys between rows, Space toggles, parent toggle announces the three-state.
  `data-qa` attributes keep the current names (`RolePermissionEditor`, `PermissionGroup-*`,
  `Permission-*`) so Playwright role tests keep passing.

### Replace the local editors

- `RoleDetailPage` renders `PermissionTree` for permissions and again for agent scopes. Delete
  `RolePermissionEditor.razor` and its CSS (and the scope equivalent if it is a separate file).
  The 489-003 draft-on-page behavior (drafts are page state, not store fields) is unchanged.

### Rule

- `skills/tw-blazor/SKILL.md`: a visual control that a product page renders is either a FluentUI
  component used at its defaults, or a component under `components/` with a catalogue entry on
  `/DesignSystem`. A feature folder does not define reusable chrome. Add a test in
  `web-spa-integration-tests` that every `.razor` under `components/elements/` is referenced by
  `design-system-parts.cs`, and that no `.razor` under `features/*/components/` renders
  `FluentCheckbox`, `FluentTreeView`, or `FluentRadioGroup` directly (those go through a
  catalogued element).

### Evidence

- Screenshots in `evidence/`: `/Admin/Roles/{AdministratorId}` permissions and agent scopes,
  light and dark, with a pixel ruler or DevTools box overlay showing row height; the
  `/DesignSystem` Permission tree entry.

## Checklist

- [ ] `PermissionTree` element + sample + catalogue entry; Design region records tree-vs-checkbox choice
- [ ] Row density met; locked items hold; keyboard works
- [ ] `RoleDetailPage` uses it for permissions and scopes; local editors deleted
- [ ] Skill rule + catalogue coverage test + no-raw-checkbox-in-features test
- [ ] Five screenshots in `evidence/`
- [ ] `dev build` 0/0, web-spa integration tests, Playwright role tests, `dev lint-skills`, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's review of the role detail tree on arch.timewarp.work.
