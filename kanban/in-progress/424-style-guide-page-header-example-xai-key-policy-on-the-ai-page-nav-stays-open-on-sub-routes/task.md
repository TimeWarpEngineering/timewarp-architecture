# Style guide page-header example, xAI key policy on the AI page, nav stays open on sub-routes

## Description

Steve's feedback on the style guide and admin pages after task 421. There are seven independent
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

### 6. Rename the Style Guide to Design System

Steve chose "Design System" over "Style Guide" and "Component Library" (2026-10-28). The page
covers foundations (brand, tokens, type), components, and usage patterns, so it documents a design
system. Rename it completely (`no-churn-arguments-eliminate-debt`):

- The title, nav entry, description and Ctrl-K rows (item 5's rows read
  "Design system: Color Tokens").
- Routes: `/StyleGuide` and `/StyleGuide/{Part}` become `/DesignSystem` and
  `/DesignSystem/{Part}`. Add no redirect from the old URL; generated apps start fresh.
- Folder `features/style-guide/` becomes `features/design-system/`, and namespace
  `TimeWarp.Architecture.Features.StyleGuide` becomes `...Features.DesignSystem`, including
  `_Imports.razor`. The concept is renamed, so the namespace follows
  (`namespaces-do-not-track-folders` is about moves, not renames).
- Types: `StyleGuidePage` becomes `DesignSystemPage`, `StyleGuideParts` becomes
  `DesignSystemParts`, `StyleGuideCatalog` becomes `DesignSystemCatalog`, and so on. Use
  roslynk `rename_symbol` so references and generated code follow.
- CSS: the `sg-` prefix becomes `ds-`. Rename `data-qa` values (`StyleGuide*` becomes
  `DesignSystem*`).
- Tests: the integration folder `features/style-guide`, the Playwright files (for example
  `style-guide-catalog-playwright-tests.cs` becomes `design-system-catalog-playwright-tests.cs`),
  and the analyzer-test fixtures that use `StyleGuidePage` as a sample page name.
- Skills and comments: `tw-blazor`, `tw-slice-isolation`, Design regions, and any prose that says
  "style guide". `kanban/` history stays as written.
- Finish with `git grep -i -E 'style-?guide'` outside `kanban/`. It should come back empty, or
  each remaining hit should have a reason in `## Results`.
- Do the rename after the other items in this task, or first, but not mixed into their diffs.
  Task 423 (breadcrumb) edits `StyleGuidePage` too and merges before this task starts.

### 7. Breadcrumb focus outline is clipped

Steve confirmed task 423's breadcrumb works. The one flaw: the keyboard focus outline on the first
crumb ("Home") is cut off. The trail list in `TimeWarpPage.razor.css` has `overflow: hidden`
(with `white-space: nowrap` and `text-overflow: ellipsis`), and that clips the outline drawn
outside the link box.

- Keep the outline whole on every crumb: give the list enough inline and block padding for the
  focus ring (with matching negative margin so the trail does not move), or use an inset
  `outline-offset`. The long-trail ellipsis must keep working.
- Playwright: focus the first crumb and check that the outline is not clipped (for example the
  link's outline box sits inside the list's clip box).

## Goal (implementer restatement)

The page shell's title bar is documented with a real Page header example (an Outline "Sync" and a
Primary "Add item", each with an icon) instead of the dummy toast buttons on the style guide's own
header. That example is not a second `h1`. The placement rule is written where page authors look:
page-wide actions go in `TimeWarpPage` `Actions` (at most one Primary); a form's submit goes in
`FormActions` at the bottom of that form. The personal xAI key policy (`XaiByokMode`) moves off
Authentication onto the AI page as its own `SettingsWrite` command with an optimistic `Version`,
so saving one page cannot overwrite or fail on the other's fields. When the host sets `XAI:ApiKey`,
the policy is shown as having no effect. A nav item whose `[Page]` declares a parameterized route
stays active, and its group stays open, on those URLs; the match comes from the generated page
registry, and a page with only a plain route stays an exact match. The catalog packs tighter while
keeping a comfortable click target, the current-item mark, and hover. Each catalog part is a Ctrl-K
row built from the one part list and shown only to someone who can open the style guide. The
breadcrumb focus ring stays inside the trail's clip box, and a long trail still ellipsizes. After
those changes, Style Guide is renamed to Design System (routes, folder, namespace, types, CSS
prefix, `data-qa`, tests, and skills) and a case-insensitive `style-guide` grep outside `kanban/`
is clean or explained. Build, golden regeneration, the affected suites, and skill lint pass.

## Checklist

- [x] Dummy header buttons removed; Page header catalog part added with the action-placement rule
- [x] Action-placement rule recorded in `tw-blazor-layout` and the `TimeWarpPage` Design region
- [x] Ask API key section moved to the AI page with its own command; removed from Authentication
- [x] Host-managed key shows the policy as having no effect
- [x] Nav item active and group open on parameterized sub-routes, derived from the page registry
- [x] Catalog rows and group spacing compact
- [x] Style-guide parts in the Ctrl-K palette, from StyleGuideParts, gated by DeveloperAccess
- [x] Breadcrumb focus outline not clipped; ellipsis still works
- [x] Renamed to Design System everywhere (routes, folder, namespace, types, CSS prefix, data-qa, tests, skills); grep clean
- [x] Tests: contract and serialization for the new command; integration for the update and the
      stale-Version 409; Playwright for the Page header part, the AI page policy, the nav
      staying open on `/DesignSystem/brand`, and a palette search opening a part
- [x] `dev build` 0/0; `dev regenerate-generated` golden diff empty; affected suites pass;
      `dev lint-skills` when `skills/` changes

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)
- Implementation: 01a17fee-eaee-7431-874e-5b29f604f11f (2026-10-28)

## Notes

- Task 418 (PR #584) is the reference for moving a setting: `UpdateAssistantName`,
  `PUT api/settings/assistant`.
- `XaiByokMode`: `source/libraries/timewarp-identity/settings/xai-byok-mode.cs` and
  `site-settings.cs`; the spa state is `features/settings/site-settings-state/`. A change to
  TimeWarp.Identity that is more than a comment means a platform package release.

## Results

The shell documents page-wide actions with a Page header catalog part (Outline "Sync", Primary
"Add item"). That sample is a `div`, not a second `h1`. The placement rule is in `tw-blazor-layout`
and the `TimeWarpPage` Design region.

`XaiByokMode` moved to the AI page as `UpdateXaiByokMode` (`PUT api/settings/xai-byok`,
`SettingsWrite`, optimistic `Version`). Authentication no longer saves it. When the host sets
`XAI:ApiKey`, the AI page shows the saved policy read-only and says the deployment key always
wins. `TimeWarp.Identity` was not changed, so there is no platform package release.

Nav match comes from `PageRegistryEntry.RouteTemplates`. A template that contains `{` uses
`NavLinkMatch.Prefix`. A plain route stays `All`, so `/Admin` does not light up on `/Admin/Roles`.
Role detail is a separate non-navigable page, so the Roles list item stays an exact match and
does not stay active on `/Admin/Roles/{id}`.

Catalog rows use `--twe-space-1` vertical padding and a 20px line-height (about 28px). Group
gaps use `--twe-space-2` and `--twe-space-1`.

Ctrl-K rows come from `DesignSystemParts` and appear only when the filtered destinations still
include `DesignSystemPage`. The row name is `Design system: {title}`.

The first breadcrumb focus ring sits inside the trail list. The list keeps overflow hidden,
nowrap, and ellipsis, with `--twe-space-1` padding and a matching negative margin.

Style Guide is now Design System: routes `/DesignSystem` and `/DesignSystem/{Part}` with no
redirect, folder `features/design-system/`, namespace `Features.DesignSystem`, types
`DesignSystemPage` / `DesignSystemParts` / `DesignSystemCatalog`, CSS prefix `ds-`, and
`data-qa` values `DesignSystem*`. `git grep -i -E 'style-?guide'` outside `kanban/` is empty.
Kanban history is unchanged. `rename_symbol` returned NotSupported because the namespace token
sits in generated Razor scaffolding, so the identifiers were renamed in source.

`ganda repo audit` passes. `kanban-landed` warns that eight other claims have no usable
worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). `--fix` landed nothing and
did not change this tree.

`./bin/dev build` 0 warnings, 0 errors. `./bin/dev regenerate-generated` left no golden diff.
`./bin/dev lint-skills` passed (9 skills).

Release suites after the rename: PageSourceGenerator_Tests 36/36, analyzer `Should_Ban` 39/39,
PageRegistry_ 6/6, CommandPalette_ 45/45, Destinations_Should_ 8/8, DesignSystemPage_Should_
5/5, PageBreadcrumbTrail_Should_ 4/4, FeaturesRazor_Should_ 2/2, CatalogAgent_Should 47/47,
DesignSystemCatalog Playwright 1/1, Breadcrumb Playwright 1/1, DesignSystemDrawer Playwright
1/1, ClearXaiApiKey Playwright 2/2. Before the rename, the same behavior passed as
UpdateXaiByokMode 10/10, UpdateSiteSettingsHandler 5/5, ActionCatalog 10/10, and
SiteSettingsEndpoints 17/17.

### How to validate

Smoke: Sign in as a developer. Open `/DesignSystem/brand`. Open Ctrl-K and search `color tokens`.
Open `/Admin/AI` with and without `XAI:ApiKey`. Tab to the Home crumb on a page that shows the trail.

Expect: The Developer group stays open and the Design System item stays active on
`/DesignSystem/brand`. The page has one `h1`, "Design System". The Page header part shows Sync
and Add item and the FormActions rule, and that sample has no `h1`. Ctrl-K `color tokens`
highlights "Design system: Color Tokens" and Enter opens `/DesignSystem/color-tokens`. On
`/Admin/AI` the Ask API key section sits next to the xAI API key. Saving it does not require
authentication fields. When `XAI:ApiKey` is set, the select is gone and the host reason is
shown. Authentication has no Ask API key section. The Home crumb's focus outline is fully
inside the trail, and a long trail still ellipsizes. `/Admin` does not stay active on
`/Admin/Roles`. The Roles list item is not active on a role detail URL.
