# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** same as framework (`master...task/379-013-…`, commit `bb35ac1f9`)

## Summary

The change adds four leaf display components, an attribute passthrough on `StatusBadge`, one
contrast token, style-guide cards, and 18 HtmlRenderer tests. The components match the task
checklist and the implementer restatement: no role-name map, caller-owned selection, granted
state not by colour alone, isolated CSS on `--twe-*` tokens, no `<style>`, no client name.
Re-verified: the four test classes pass (4 + 5 + 4 + 5, 0 failed, Release); `--twe-positive`
`#50852c` is 4.44:1 on white and `--twe-on-positive` `#14784a` is about 5.5:1; the
`git diff master...HEAD -- source tests` client-name search has 0 matches. Risk is low. The one
defect is a style-guide spacing class that cannot reach child component roots under CSS isolation.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:342
- Description: `Class="sg-follow"` is passed to `PermissionGrid` (line 342) and `DevNote` (line 347). `.sg-follow` lives in `StyleGuidePage.razor.css`, which compiles to `.sg-follow[b-iy1milohjd]` (checked in `obj/Release/net11.0/scopedcss/bundle`). Those roots are authored by the child components and carry their own scope (`.twe-perm-grid[b-2lwjyub97l]`, `.twe-devnote[b-jkdzvil7c7]`), so the selector never matches and the `margin-top` is dead. The grid and the note sit flush under the subtitle (`.sg-subtitle` has `margin: 4px 0 0`), unlike the role-badge and role-card cards, whose `sg-follow` is on a native div the page authors. This is Wall A in `tw-blazor-css-strategy` ("a child component's root … is never stamped, so a plain isolated selector cannot match it").
- Suggestion: Wrap each in a native element the page authors (`<div class="sg-follow" data-qa="…">…</div>`), or use an anchored `::deep` from a native page element. Do not pass a page-scoped class into a child component's `Class`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/elements/RoleCard.razor:6
- Description: `RoleCard` renders `role="radio"`, and the style guide puts three cards in `role="radiogroup"` (StyleGuidePage.razor:333). The ARIA radio-group pattern promises one tab stop and arrow-key movement. Here every card is a tab stop and arrows do nothing. The repo already ships that behaviour for `SegmentedControl` (`components/composites/roving-selection.cs`, tabindex 0 / −1, `RovingSelection.CancelScript`). The Design region says the component "does not move focus between them", but it does not say that the parent then owns the roving tab stop and the arrow keys, so a generated app that copies the sample ships a radiogroup without keyboard support.
- Suggestion: In the RoleCard Design region (and the `overview.md` row), state that the parent owns the roving tab stop (`tabindex` via unmatched attributes) and the arrow keys, and point at `RovingSelection` / `SegmentedControl` as the existing pattern. Optionally have the style-guide sample pass `tabindex="0"` on the selected card and `-1` on the others.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/components/permission-grid-render-tests.cs:40
- Description: `html.ShouldContain("Granted")` is also satisfied by `"Not granted"`, so in `Render_Granted_And_Denied_Rows` the granted branch's text is not pinned on its own. `Omit_A_Blank_Group_Label` has no denied row, so it is fine there.
- Suggestion: Assert `">Granted<"` (or count occurrences) so the granted branch is pinned separately from `Not granted`.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/elements/PermissionGrid.razor:41
- Description: The visually hidden state words (Granted / Not granted) use `twe-perm-grid__name`, while the capability's name uses `twe-perm-grid__label`. "name" for the state element reads as the capability name.
- Suggestion: Rename the hidden span class to `twe-perm-grid__state` (Razor + CSS).
- Status: open
