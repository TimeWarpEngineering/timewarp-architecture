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

## Goal (implementer restatement)

The role detail page's permission and agent-scope editors are a hand-built feature control with
~53px rows (FluentCheckbox default margin) and literal px spacing. Replace them with one
catalogued, editable design-system element and make the rule enforceable.

Acceptance:
- `PermissionTree` lives in `components/elements/` beside `PermissionGrid`, with a sample and a
  `/DesignSystem` catalogue entry (Identity group). It takes groups, a selected set, a locked set,
  raises one change event with a new set, and decides nothing about grants. Its Design region
  records why FluentTreeView was or was not used.
- Child rows are at most 32px, parent rows at most 36px; locked items stay checked and cannot be
  changed (including through the parent); arrows move between rows, Space toggles, the parent
  shows and announces mixed.
- `RoleDetailPage` uses it for permissions and agent scopes; the local editor is deleted;
  drafts stay page state; `data-qa` names (`RolePermissionEditor`, `PermissionGroup-*`,
  `Permission-*`, `AgentScope-*`) are kept.
- `tw-blazor` states the rule; a web-spa integration test fails when an element is not named in
  `design-system-parts.cs` or a feature component renders FluentCheckbox / FluentTreeView /
  FluentRadioGroup directly.
- Five screenshots in `evidence/` with row heights visible; build, tests, lint-skills, audit green.

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

- [x] `PermissionTree` element + sample + catalogue entry; Design region records tree-vs-checkbox choice
- [x] Row density met; locked items hold; keyboard works
- [x] `RoleDetailPage` uses it for permissions and scopes; local editors deleted
- [x] Skill rule + catalogue coverage test + no-raw-checkbox-in-features test
- [x] Five screenshots in `evidence/`
- [x] `dev build` 0/0, web-spa integration tests, Playwright role tests, `dev lint-skills`, `ganda repo audit` green

## Session

- Created: 2026-11-24 from Steve's review of the role detail tree on arch.timewarp.work.
- 2026-11-24 implement: FluentTreeView rejected (FluentUI 5.1.0 `ITreeViewItem` / `FluentTreeItem`
  have no `Disabled`; `TreeViewThreeStateSelection` flips every descendant, so locked rows and a
  read-only tree cannot be expressed). Fallback 2 shipped: Margin="0" FluentCheckbox rows in
  native lists, `--twe-space-*` only. Rows measure 25px (was ~53px).
- Boy-scout: the coverage test found three uncatalogued elements. `Text.razor` and
  `TimeWarpNavUrl.razor` had no callers and were deleted; `Card` got its own catalogue entry
  (`CardSample`). `design-system-parts.cs` would pass the TWA0038 500-line cap, so slug lookup
  (Resolve / TryGet / Url / Render) moved to `DesignSystemPartLookup`; callers updated.

## Results

- `PermissionTree` (`components/elements/PermissionTree.razor`, `.razor.css`,
  `permission-tree-group.cs`), `PermissionTreeSample`, catalogue entry `permission-tree` (Identity,
  after Permission grid). `RoleMembershipTrees` (feature) builds permission groups by prefix,
  agent-scope groups by resource (`identity:*`, `demo:*`, `credential:*`, `feedback:*`), and the
  protected-core locked set. `RoleDetailPage` renders the tree twice (`RolePermissionEditor` /
  `PermissionGroup-*` / `Permission-*`; `RoleAgentScopeEditor` / `AgentScopeGroup-*` /
  `AgentScope-*`). `RolePermissionEditor.razor` + css deleted.
- Rule: `skills/tw-blazor/SKILL.md` "Visual controls come from the design system";
  `components/overview.md` lists `PermissionTree`.
- Evidence (`evidence/`, row outline + height label injected by the Playwright test):
  `role-administrator-permissions-light.png`, `role-administrator-permissions-dark.png`,
  `role-administrator-agent-scopes-light.png`, `role-administrator-agent-scopes-dark.png`,
  `design-system-permission-tree.png`. Every row reads 25px.
- Not changed: `PrincipalDetailPage` still renders its agent-scope FluentCheckbox list in a page
  (pages are outside the test's `features/*/components/` scope); a follow-up could move it onto
  `PermissionTree`.

### Gates

- `./bin/dev build` — passed, 0 warnings / 0 errors (re-run after the last product edit).
- Test files are not standalone runfiles (no `#if !JARIBU_MULTI` guard); each was run as a
  targeted class filter on its project:
- `components/permission-tree-render-tests.cs` (new): `./bin/dev test --project web-spa-integration-tests --filter-class PermissionTree` — 6/6 passed.
- `features/design-system/design-system-catalog-coverage-tests.cs` (new) and `design-system-page-contract-tests.cs` (edited): `--filter-class DesignSystem` — 8/8 passed.
- `features/application/command-palette-tests.cs` (edited): `--filter-class CommandPalette_` — 45/45 passed.
- `features/application/navigation-destinations-test-fixture.cs` (edited): `--filter-class NavigationDestinations_` — 8/8 passed.
- `web-spa-playwright-tests/role-pages-playwright-tests.cs` (edited): `./bin/dev test --project web-spa-playwright-tests --filter-class RolePages_` — 2/2 passed.
- `./bin/dev lint-skills` — 9/9 skills passed.
- `dev template-smoke` not run locally; CI runs the default variant on the PR.
- `ganda repo audit` — see below.

### How to validate

Smoke: `./bin/dev test --project web-spa-playwright-tests --filter-class RolePages_`, then open
`tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/playwright-screenshots/RolePages_Given_Wasm/`;
or `dev run`, sign in as the first (Administrator) account, open `/Admin/Roles/{Administrator id}`
and `/DesignSystem/permission-tree`.

Expect: permissions and agent scopes render as grouped trees with rows about 25px apart; every
`admin.*` row and the `admin.*` parent are checked and disabled; clicking `developer.access`
flips it and leaves `developer.*` mixed or clear; ArrowDown / ArrowUp move focus between rows and
Space toggles; dark mode keeps the same layout; the Permission tree catalogue entry shows a mixed,
a checked, and a clear parent with `people.manage` locked.

