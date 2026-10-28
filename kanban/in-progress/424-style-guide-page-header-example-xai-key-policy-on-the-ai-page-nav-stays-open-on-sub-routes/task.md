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

The shipped work stays: a Page header example (Outline "Sync", Primary "Add item") that is not a
second `h1`; page-wide actions in `TimeWarpPage` `Actions` and form submit in `FormActions`;
`XaiByokMode` on the AI page as its own `SettingsWrite` command; a host `XAI:ApiKey` shown as
having no effect; parameterized design-system routes keeping their nav item active and the group
open; a tighter catalog; Ctrl-K rows from the one part list; the breadcrumb focus ring inside the
clip; and the rename from Style Guide to Design System.

This pass closes review round 1 (M1–M19).

The assistant-name draft and the key-policy draft share `SiteSettingsState.Version`. When that
version advances and the server value of a draft's own field still equals the value that draft
last synced, the draft copies the new version and keeps a local edit. An unedited draft still
takes a server change to its own field. Saving one form no longer leaves the other form on a
stale version that 409s.

`tw-deploy` says the BYOK policy is set on Admin → AI and that the select is read-only when the
host key is set.

The design-system contract asserts the shell header has no actions: the only `twe-page__actions`
is inside the Page header example, and a non-header part renders none.

Policy draft-sync render tests cover a stale snapshot, an agent update copied into an unedited
draft, and a cross-form save advancing the other draft's version in both directions.

Agent page facts include `xaiByokMode`. The Design region lists `UpdateXaiByokMode`. The host key
is not part of that snapshot.

A nav category opens when the current path is under one of its items' `Url + "/"`, taken from the
registry, without marking that item active. Exact list routes and Home stay exact matches.

`PageRegistryEntry.RouteTemplates` is init-only and is set in the registry object initializer.
The side dictionary and static constructor are gone. The generator Design region says the list
holds every `[Page]` template, including parameterized aliases, that nav match reads it, and that
it is not a second registry row.

The command-palette Design region names both slice edges (`LoginPage` and `DesignSystemParts`).

One `PageTitleBar` owns the title-bar markup and CSS. The shell uses heading level 1. The sample
uses another level so the page still has one `h1`. The description is a `div` with the shell
margin. When both description and actions are empty, the header wrapper is omitted. The sample
frame uses the `ds-page-header` prefix. The example buttons show placement and do not post toasts.

`--twe-space-1` (4px) stays. Scale comments name 4, 8, 12, 24, 48. The token exists so catalog
rows stay near 28px and the breadcrumb focus ring fits inside the clip.

`tw-blazor` points the section, button, and forms references at the sample files.

The three `Style_Guide` test names are renamed. The host-key Playwright regions cover the
read-only policy. The `SettingsWrite` note includes `UpdateXaiByokMode`. The role-detail
Playwright asserts the Roles item exists and is not active, and that the Admin group is expanded.
Palette labels use `DesignSystemPage.Title`, so the visible prefix is "Design System: ".

Acceptance: those behaviors hold in code and tests; skill lint and `ganda repo audit` pass;
Results include Smoke and Expect; the task branch is committed and pushed.

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
- [x] Review round 1 findings M1–M19 fixed on this branch

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-27)
- Implementation: 01a17fee-eaee-7431-874e-5b29f604f11f (2026-10-28)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-28T01:34:59Z
- Review oracle round 1 (2026-10-28): claude-opus-5-5, headless, effort 3. Reviewers general-1
  (xAI policy), general-2 (nav, palette), general-3 (page header, CSS, breadcrumb, rename). Subagent
  ids abfcef1c21f175cfa, a1e79616ebde5ed6a, a5aa812952a2f275c. Outcome: fix pass needed.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-28T01:43:43Z
- Fix pass (implement-fix-2): implemented by implementer-grok (grok, model grok-4.7), session not reported — 2026-10-28. Addressed M1–M19 in product and tests.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7, M8, M9, M10, M11, M12, M13, M14, M15, M16, M17, M18, M19
- Fix pass: those findings are addressed on this branch. Dispositions in `review/round-1/merged.md` are unchanged. Round 2 is the host review node.

## Notes

- Task 418 (PR #584) is the reference for moving a setting: `UpdateAssistantName`,
  `PUT api/settings/assistant`.
- `XaiByokMode`: `source/libraries/timewarp-identity/settings/xai-byok-mode.cs` and
  `site-settings.cs`; the spa state is `features/settings/site-settings-state/`. A change to
  TimeWarp.Identity that is more than a comment means a platform package release.

## Learnings

- Misread (review round 1): `## Goal (implementer restatement)` dropped two constraints from the
  task. Both gaps became findings.
  - Item 3 says "Check the other parameterized pages for a sensible result". The detail pages under
    Admin still collapse their group (M6).
  - Item 4 says "Add a token only if none fits". `--twe-space-1` was added without a stated reason
    (M11).

  Otherwise the restatement matches items 1–7. why: review oracle round 1, 2026-10-28

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
include `DesignSystemPage`. The row name is `DesignSystemPage.Title` plus the part title
(`Design System: {title}`), from `DesignSystemParts.PaletteName`.

The first breadcrumb focus ring sits inside the trail list. The list keeps overflow hidden,
nowrap, and ellipsis, with `--twe-space-1` padding and a matching negative margin.

Style Guide is now Design System: routes `/DesignSystem` and `/DesignSystem/{Part}` with no
redirect, folder `features/design-system/`, namespace `Features.DesignSystem`, types
`DesignSystemPage` / `DesignSystemParts` / `DesignSystemCatalog`, CSS prefix `ds-`, and
`data-qa` values `DesignSystem*`. `git grep -i -E 'style-?guide'` outside `kanban/` is empty.
Kanban history is unchanged. `rename_symbol` returned NotSupported because the namespace token
sits in generated Razor scaffolding, so the identifiers were renamed in source.

`ganda repo audit` passes (exit 0). `kanban-landed` remains an advisory: eight other claims have
no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). `ganda repo audit --fix`
landed no kitchen and did not change this tree. Those claims are outside this id.

`./bin/dev build` exited 0 after two test-project fixes (`new Regex` became an exact Playwright
text query, and a role id became `const`). `./bin/dev lint-skills` passed (9 skills). This pass
did not rerun `./bin/dev regenerate-generated`. That command rewrites only the ingress prefix
golden, and this pass adds no API route. The earlier ship left that golden unchanged.
`git grep -i -E 'style[ _-]?guide'` outside `kanban/` is empty.

Review round 1 is addressed in code:

- The assistant-name draft and the key-policy draft share `SiteSettingsState.Version`. When that
  version advances and a draft's own server field still equals the value it last synced, the
  draft copies the version and keeps a local edit. An unedited draft still takes a server change
  to its own field.
- `tw-deploy` says the policy is Required, Allow, or Off on Admin → AI, and that the select is
  read-only when the host key is set.
- The design-system contract counts `twe-page__actions` once, after the Page header example, and
  zero times on any other part. The shell header itself has no actions.
- Agent page facts include `xaiByokMode`. The host key stays on `XaiApiKeyState`.
- A nav category opens when the current path is under an item `Url + "/"`, without activating
  that item. Role, principal, and feedback details, and `/Admin/Roles/New`, open Admin. Exact
  list routes and Home stay `NavLinkMatch.All`.
- `PageRegistryEntry.RouteTemplates` is init-only and is assigned in the registry initializer.
  The side dictionary and static constructor are gone.
- `PageTitleBar` owns the title-bar markup and CSS. The shell uses heading level 1. The sample
  uses another level, so the page still has one `h1`. Empty description and actions omit the
  header wrapper. The sample frame is `ds-page-header`.
- `--twe-space-1` stays 4px. Scale comments name 4, 8, 12, 24, 48. Catalog rows target about 28px
  (20px line-height plus 4px padding on each side). `--twe-space-2` (8px) would make those rows
  about 36px. The same 4px pad keeps the breadcrumb focus ring inside the clip.
- Palette labels use `DesignSystemPage.Title`, so the visible prefix is "Design System: ".

Fix-pass suites: PageSourceGenerator_Tests 36/36, AiPage_Should_ 5/5, DesignSystemPage_Should_
5/5, PageRegistry_ 7/7, CommandPalette_ 45/45, PageBreadcrumbTrail_Should_ 4/4,
TimeWarpPage_Should_ 15/15, CatalogAgent_Should 47/47, Breadcrumb Playwright 1/1,
ClearXaiApiKey Playwright 2/2, DesignSystemCatalog Playwright 1/1. Browser proof is those
Playwright hosts. There was no separate interactive browser session.

### Review

Round 1 (effort 3, three general reviewers) found 19 issues: 3 bugs, 9 suggestions, and 7 nits.
This pass implements them. The ledger dispositions are still open until round 2. Ledger:
`review/round-1/merged.md`. Framework: `review/review-framework.md`.

### How to validate

Smoke: Sign in as a developer. Open `/DesignSystem/brand`. Open Ctrl-K and search `color tokens`.
Open `/Admin/AI` with no host key, set the key policy to Off, change the assistant name, and save
the name. Repeat `/Admin/AI` with `XAI:ApiKey` set. Open `/Admin/Roles`, then Administrator, and
open `/Admin/Roles/New`. Tab to the Home crumb on a page that shows the trail.

Expect: The Developer group stays open and the Design System item stays active on
`/DesignSystem/brand`. The page has one `h1`, "Design System". The Page header part shows Sync
and Add item and the FormActions rule, and that sample has no `h1`. The only `twe-page__actions`
is inside that example. Ctrl-K `color tokens` highlights "Design System: Color Tokens" and Enter
opens `/DesignSystem/color-tokens`. On `/Admin/AI` the Ask API key section sits next to the xAI
API key. Saving the name after a local policy edit keeps Off and copies the new site-settings
version into the policy draft, so the policy save does not 409. When `XAI:ApiKey` is set, the
select is gone and the host reason is shown. Authentication has no Ask API key section. On a
role detail URL and on `/Admin/Roles/New`, the Admin group is expanded and the Roles item is not
active. A principal detail and a feedback item do the same for their list items. The Home crumb's
focus outline is fully inside the trail, and a long trail still ellipsizes. `/Admin` does not
stay active on `/Admin/Roles`.
