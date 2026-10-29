# Ask finds Ctrl-K deeplinks in site_map and navigate

## Description

Ask answers about normal pages but not deeplinks that Ctrl-K lists. Reported
2026-10-28: "show me the sample buttons" and "show me the Filter bar" were not
found, "it found the normal pages". Those are DesignSystemParts deep links
(/DesignSystem/buttons, /DesignSystem/filter-bar), which CommandPaletteRoster
adds as Page rows in Ctrl-K but AgentSiteMap.DestinationsAsync never returns:
destinations come only from NavigationDestinations (PageRegistry.All), and the
roster comment says parts are "not agent navigate destinations". The Ask
instructions also forbid navigating off the site map, so the model cannot offer
them. Teach site_map and navigate the same deeplinks Ctrl-K shows.

## Goal (implementer restatement)

Ask (and WebMCP) must know the same Design System part deep links that Ctrl-K shows, so
"show me the Filter bar" finds /DesignSystem/filter-bar and can move there.

Acceptance:

- `AgentSiteMap.DestinationsAsync` returns one row per `DesignSystemParts.PageOrder` part, after
  the menu pages, only when the Design System page is permitted. The part list comes from the same
  source Ctrl-K uses (no second hand-written list).
- `navigate` accepts those URLs (enum, description, invoke-time check), and still refuses them for
  a principal who cannot open the Design System page.
- The Ask site-map summary lists the parts compactly, so the 30+ parts fit `SiteMapCap`, and the
  navigate description stays under `CompleteAgentChat.MaxToolDescriptionLength`.
- Design comments in CommandPaletteRoster, AgentSiteMap, AgentNavigate and the instructions no
  longer say parts are not navigate destinations.
- Tests: catalog-agent test shows site_map contains /DesignSystem/filter-bar and navigate goes
  there; the destinations parity test expects parts on navigate and site_map.

## Checklist

- [x] Include the DesignSystem part URLs in AgentSiteMap.DestinationsAsync
  (gated on the Design System page being permitted), AgentNavigate's enum and
  description, and the Ask instructions site-map summary.
- [x] Update the CommandPaletteRoster / AgentSiteMap design comments that say
  parts are not navigate destinations.
- [x] Cover with a catalog-agent test: destinations contain
  /DesignSystem/filter-bar and navigate accepts it.

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implemented under ganda task work (implement oracle, 2026-10-30).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 513ba748-4b05-4267-91d4-33a1e7c542f7, max-turns 500, Done — 2026-10-29T18:00:49Z

## Notes

Root cause evidence: command-palette-roster.cs BuildRowsAsync adds
DesignSystemParts.PageOrder rows (PaletteName) when the Design System page is
permitted; agent-site-map.cs DestinationsAsync iterates only
NavigationDestinations.ForAsync (PageRegistry.All). PageAgentContext.Identity
already prefix-matches /DesignSystem/*, so only the destination list needs the
fix. Keep the SiteMapCap budget in mind (30+ parts).

## Results

- `CommandPaletteRoster.DeepLinks(page)` (new, public) is the one source of a page's deep links:
  the `DesignSystemParts.PageOrder` parts for the Design System page, else none. Ctrl-K's part
  rows now come from it (same rows, same order, after the pages).
- `AgentSiteMap.DestinationsAsync` appends those deep links after the menu pages, only when the
  parent page is permitted. `Destination` gains an optional `parent` (the page url). Each part's
  description is `DesignSystemParts.Description` ("Live Filter Bar sample and its usage snippet
  (Forms).").
- `AgentNavigate` enum lists every part url. The description and the Ask summary collapse a
  page's deep links to one line, `AgentSiteMap.DeepLinkLines`:
  `/DesignSystem/<part> for Design System parts: brand, color-tokens, …, filter-bar, …`, so 32
  parts cost about 400 characters. The navigate description stays under
  `CompleteAgentChat.MaxToolDescriptionLength` for the administrator (asserted in the parity test).
- Design regions updated: command-palette-roster.cs, agent-site-map.cs, agent-navigate.cs,
  agent-ask-instructions.cs, catalog-agent-tool-set.cs, design-system-parts.cs, and both test
  files. The site_map tool description now mentions deep links. `skills/tw-blazor/SKILL.md`
  agentic-UI bullet says site_map includes the Ctrl-K deep links.
- Tests: new `CatalogAgent_Should.Site_Map_And_Navigate_Include_Design_System_Parts` (developer
  sees /DesignSystem/filter-bar in site_map with parent /DesignSystem, navigate's enum and
  description carry it, navigate moves there with title "Design System: Filter Bar"; a member
  without DeveloperAccess gets no parts and navigate refuses). `Destinations_Should_` parity now
  expects parts on the navigate enum and site_map after the pages, the summary parts line, and
  refuses /DesignSystem/drawers only when the design system is not permitted.

Gates (2026-10-30, this worktree):

- `./bin/dev build`: 0 warnings, 0 errors.
- `web-spa-integration-tests --filter-class`: Destinations_Should_ 8/8, CatalogAgent_Should 48/48,
  CommandPalette_Should_ 45/45, DesignSystemPage_Should_ 6/6, All_Should_ 7/7.
- `./bin/dev lint-skills`: passed. `./bin/dev template-smoke` (default variant): SUCCEEDED.
- `./bin/dev check-version`: source 2.0.0-beta.29 is ahead of NuGet 2.0.0-beta.28; no bump needed.
- `ganda repo audit`: exit 0, 34 passed. The one finding is the `kanban-landed` warning: 8 other
  tasks' claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) have no worktree on this
  machine. Not fixed here, because `--fix` would change those tasks' claims on the shared board.

### How to validate

Smoke:

```bash
./bin/dev build
(cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class CatalogAgent_Should)
(cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class Destinations_Should_)
```

Manual: `dev run`, sign in as a developer, open Ask, and ask "show me the Filter bar" or "show
me the sample buttons".

Expect: both suites pass, including `Site_Map_And_Navigate_Include_Design_System_Parts`. In the
app, Ask links or offers to navigate to `/DesignSystem/filter-bar` or `/DesignSystem/buttons`,
and approving the navigate opens that part. A user without DeveloperAccess gets no part links.
