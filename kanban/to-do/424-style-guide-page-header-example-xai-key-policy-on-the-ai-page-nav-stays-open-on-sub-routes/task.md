# Style guide page-header example, xAI key policy on the AI page, nav stays open on sub-routes

## Description

Steve's feedback on the style guide and admin pages after task 421. There are five independent
fixes. Task 423 covers the breadcrumb separately.

### 1. Page-header example replaces the dummy header buttons

`StyleGuidePage.razor` puts two buttons, "Outline" and "Primary", in the shell's `Actions` slot.
They only raise a toast, and nothing says what they are for. They were meant to show the
page-actions area (commit a4d5ff241, "put page description and actions on the shell title").

- Remove both buttons from the style guide's own header.
- Add a catalog part, **Page header** (Layout group), with a framed example of the title bar:
  - the title and description on the left;
  - a secondary (Outline) action and a primary action on the right, each with a real label
    and an icon (for example "Sync" and "Add item");
  - a short rule: page-wide actions (create, sync, export) go top right in `TimeWarpPage`
    `Actions`, with at most one Primary. A form's submit button goes at the bottom of that
    form, in `FormActions`, after the fields it saves.
- The example must not render a second `h1`. `TimeWarpPage` owns the only one (its Design
  region). Use the same classes or a sample-only heading level.
- Steve confirmed this rule (2026-10-27). Record it where page authors look: the
  `tw-blazor-layout` skill and the `TimeWarpPage` Design region.

### 2. The xAI key policy moves to the AI page

The Authentication page (`features/admin/site-settings/pages/AuthenticationPage.razor`) has an
"Ask API key" section. It sets `XaiByokMode` (the personal xAI API key policy, task 399) through
the site-settings command. The xAI API key itself is on the AI page
(`features/admin/ai/pages/AiPage.razor`). Steve: the AI settings belong on the AI page.

- Move the "Ask API key" section to the AI page, next to the "xAI API key" section, and remove
  it from Authentication. "Save authentication settings" then saves only authentication
  fields.
- Follow task 418's pattern (the assistant name moved the same way): give the policy its own
  command and endpoint with `[Operation]`, `SettingsWrite`, and an optimistic `Version`, so
  saving one page cannot fail on, or overwrite, the other page's fields. Reuse 418's handling
  of a stale Version and of refreshing the form after an update.
- When the host sets `XAI:ApiKey`, the policy has no effect (the deployment key always wins).
  Show that on the AI page instead of an editable select that seems to work. Either disable it
  with that reason, or show the saved value read-only with the reason.
- `XaiByokMode` stays in the site-settings row. This needs no migration unless the new command
  requires one.

### 3. The left nav stays open on a page's sub-routes

Clicking any style-guide deep link (`/StyleGuide/brand`) collapses the Developer group in the
left nav. `NavMenu.razor` renders every item with `Match=NavLinkMatch.All`. The item's `Href` is
`/StyleGuide`, so `/StyleGuide/{Part}` matches no item, and the category with no active item
collapses.

- A page whose `[Page]` attribute declares a parameterized route (`StyleGuidePage` has
  `/StyleGuide` and `/StyleGuide/{Part}`) keeps its nav item active, and its group open, on
  those sub-routes.
- Derive the match mode from the page registry (`PageRegistryEntry` from `[Page]`), not a
  hand-kept list. A page with only a plain route keeps `NavLinkMatch.All`, so `/Admin` doesn't
  light up on `/Admin/Roles`.
- Check the other parameterized pages (for example role detail) for a sensible result.

### 4. The catalog list is tight

The style-guide catalog (`features/style-guide/components/StyleGuideCatalog.razor.css`) wastes
vertical space, so the list scrolls early. Each link has `padding: var(--twe-space-2)` at body
size, and groups add `--twe-space-3` above plus `--twe-space-2` under the heading. The reference
Steve used (https://ryanlanciaux.com/native-surface/#playground) packs its catalog much tighter.

- Make the rows compact: smaller vertical padding (horizontal can stay) and a smaller gap
  between groups and under group headings, so more of the catalog fits without scrolling. Keep
  the link a comfortable click target (about 28px tall), the current-item indicator, and the
  hover state.
- Use existing spacing tokens. Add a token only if none fits.

### 5. Style-guide parts are in the Ctrl-K palette

Each catalog part is a palette row ("Style guide: Color Tokens") that opens
`/StyleGuide/{slug}`, so a part is reachable without scrolling the catalog. Steve chose this over
moving the parts into the app nav (2026-10-28).

- Build the rows from `StyleGuideParts` (the one part list), not a second hand-kept list.
- Show them only to users who can open the style guide (`PermissionIds.DeveloperAccess`), the
  same way the palette already gates page rows.
- The palette is in `features/application/command-palette/` (`command-palette-roster.cs`
  builds rows, `command-palette-ranker.cs` ranks them). Fit the parts into that roster.
  Don't add a side channel.
- The catalog column already scrolls on its own and stays in view (`position: sticky` in
  `StyleGuideCatalog.razor.css`). Keep that.

## Checklist

- [ ] Dummy header buttons removed; Page header catalog part added with the action-placement rule
- [ ] Action-placement rule recorded in `tw-blazor-layout` and the `TimeWarpPage` Design region
- [ ] Ask API key section moved to the AI page with its own command; removed from Authentication
- [ ] Host-managed key shows the policy as having no effect
- [ ] Nav item active and group open on parameterized sub-routes, derived from the page registry
- [ ] Catalog rows and group spacing compact
- [ ] Style-guide parts in the Ctrl-K palette, from StyleGuideParts, gated by DeveloperAccess
- [ ] Tests: contract and serialization for the new command; integration for the update and the
      stale-Version 409; Playwright for the Page header part, the AI page policy, the nav
      staying open on `/StyleGuide/brand`, and a palette search opening a part
- [ ] `dev build` 0/0; `dev regenerate-generated` golden diff empty; affected suites pass;
      `dev lint-skills` when `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)

## Notes

- Task 418 (PR #584) is the reference for moving a setting: `UpdateAssistantName`,
  `PUT api/settings/assistant`.
- `XaiByokMode`: `source/libraries/timewarp-identity/settings/xai-byok-mode.cs` and
  `site-settings.cs`; the spa state is `features/settings/site-settings-state/`. A change to
  TimeWarp.Identity that is more than a comment means a platform package release.
