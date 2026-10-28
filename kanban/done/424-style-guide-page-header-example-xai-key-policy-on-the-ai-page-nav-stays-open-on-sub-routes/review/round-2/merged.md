# Round 2 — merged findings
**Date:** 2026-10-28
**Sources:** general-1 (xAI policy), general-2 (nav match, palette), general-3 (page header, CSS,
skills)
**Scope:** the fix-pass delta `0d475995b..31bdd4ae7` (product commit `0c0c617af`), excluding `kanban/`.

The review oracle checked these claims against the tree:
- M23: `DesignSystemPage.razor:6` and `:30`.
- M24: `PageTitleBar.razor:15` and `:21-31`.
- M25: `PageTitleBar.razor:33` against `TimeWarpPage.razor:236-244` at `0d475995b`.
- M22: `NavMenu.razor` line lengths. Twelve other lines in the file are already over 100 characters.

## Counts

Totals for both rounds. M1–M19 are from round 1, and M20–M25 are new in round 2.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 2 | 9 | 0 |
| nit | 2 | 7 | 2 |

## Prior findings (round 1)

The reviewers re-checked each one against the fix delta.

| ID | Severity | Status | Evidence |
|----|----------|--------|----------|
| M1 | bug | fixed | `AiPage.razor` `SyncUnedited*Draft` adopt a newer Version when the draft's own server field is unchanged, and keep the old Version when that field changed. Both directions are covered. |
| M2 | bug | fixed | `tw-deploy/SKILL.md:384` says Admin → AI and that the select is read-only when the host key is set. |
| M3 | bug | fixed | `AssertPageHeaderOwnsTheOnlyActions`: one `twe-page__actions`, inside the example. Every other part has none. 5/5 pass. |
| M4 | suggestion | fixed | Stale snapshot, agent update, and cross-form save in both directions. `AiPage_Should_` 5/5. |
| M5 | suggestion | fixed | `xaiByokMode` fact added, the Design region updated, and `catalog-agent-tests` asserts it (47/47). |
| M6 | suggestion | fixed | `NavigationMatch.ContainsChildPath`. The edge cases are tested. |
| M7 | suggestion | fixed | `RouteTemplates { get; init; }` in the initializer. The side dictionary and static constructor are removed. Generator tests 36/36. |
| M8 | suggestion | fixed | `page-source-generator.cs:41-43` |
| M9 | suggestion | fixed | `command-palette-roster.cs:45-48` |
| M10 | suggestion | fixed | `PageTitleBar` is the one title-bar copy. |
| M11 | suggestion | fixed | `tokens.css:75-77` gives the reason. The nine scale comments say 4, 8, 12, 24, 48. |
| M12 | suggestion | fixed | `tw-blazor` points at `SectionSample`, `ButtonsSample`, and `FormsSample`. |
| M13 | nit | fixed | The wide grep is empty. The three test names are renamed. |
| M14 | nit | fixed | Playwright regions updated. |
| M15 | nit | fixed | The `SettingsWrite` note names `UpdateXaiByokMode`. |
| M16 | nit | fixed | Asserts the Roles item exists and is not active, and that Admin is expanded. |
| M17 | nit | fixed | `ds-page-header` prefix. |
| M18 | nit | fixed | `DesignSystemParts.PaletteName` uses `DesignSystemPage.Title`. Comments rewrapped. |
| M19 | nit | fixed | Toast handlers removed. The rule calls it a placement demo. |

## New issues (round 2)

### M20 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai-page-assistant-draft-tests.cs
- Description: no test covers the M1 branch that protects another user's change. That branch runs
  when a draft is edited, its own server field changes, and the Version advances: the draft must keep
  its old Version. A regression that adopts the Version every time would pass all five tests. It would
  then silently overwrite the other user's value.
- Suggestion: edit the policy draft to `Off`, then have the server report `Required` with a newer
  Version (for example an agent `UpdateXaiByokMode`). Assert that `data-xai-byok-version` stays old
  and the mode stays `Off`. Mirror the test for the name draft.
- Source: general-1 #1
- Disposition notes:

### M21 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/admin/ai/pages/AiPage.razor (`SyncUnedited*Draft`)
- Description: if another user changes the field A→B→A between two renders, an edited draft adopts
  the newer Version. Saving it overwrites the transient B.
- Suggestion: none needed.
- Source: general-1 #2
- Disposition notes: wontfix (review oracle). The final value is the one the other user left, so
  nothing is lost. Detecting the transient change needs a per-field version, which the 418 pattern
  does not have.

### M22 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:20
- Description: one rewrapped comment line is 101 characters.
- Source: general-2 #1
- Disposition notes: wontfix (review oracle). Twelve other lines in `NavMenu.razor` are already over
  100 characters, and no rule or analyzer enforces the width. M18 asked about the roster's comments.

### M23 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/design-system/pages/DesignSystemPage.razor:30
- Description: the palette prefix uses `DesignSystemPage.Title` (line 6), but the markup passes the
  literal `Title="Design System"` to `TimeWarpPage`. The page title and the Ctrl-K prefix can still
  drift, which is what M18 meant to prevent.
- Suggestion: `Title="@Title"`.
- Source: general-2 #2
- Disposition notes:

### M24 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/PageTitleBar.razor:15, 21-31
- Description: `HeadingLevel` is an int, and only `1` means anything. Every other value, including
  2–6, renders a `div`. So `HeadingLevel="2"` gives no h2, and the sample's `HeadingLevel="0"` is an
  undocumented magic value. This component ships in every generated app.
- Suggestion: make the parameter say what it does. Either use a bool (for example
  `IsPageHeading`, default true), or render h2–h6 for 2–6 and reject other values. Add a render test
  that the sample variant emits no h1.
- Source: general-3 #1
- Disposition notes:

### M25 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/PageTitleBar.razor:33
- Description: the extraction changed behavior. A blank `Title` now skips the whole bar. Before
  (`0d475995b` `TimeWarpPage.razor:236-244`), `twe-page__header` still rendered Description and
  Actions without a title. A blank-title page with Actions now loses them.
- Suggestion: apply the blank-title check to the heading only, or state the new behavior in the
  `PageTitleBar` Design block.
- Source: general-3 #2
- Disposition notes:

## Duplicates / conflicts

- No overlaps between slices. No reviewer disagreements.
- M20 is coverage for M1, which is fixed. It does not reopen M1.
