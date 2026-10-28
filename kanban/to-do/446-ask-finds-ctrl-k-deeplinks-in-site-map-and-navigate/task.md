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

## Checklist

- [ ] Include the DesignSystem part URLs in AgentSiteMap.DestinationsAsync
  (gated on the Design System page being permitted), AgentNavigate's enum and
  description, and the Ask instructions site-map summary.
- [ ] Update the CommandPaletteRoster / AgentSiteMap design comments that say
  parts are not navigate destinations.
- [ ] Cover with a catalog-agent test: destinations contain
  /DesignSystem/filter-bar and navigate accepts it.

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)

## Notes

Root cause evidence: command-palette-roster.cs BuildRowsAsync adds
DesignSystemParts.PageOrder rows (PaletteName) when the Design System page is
permitted; agent-site-map.cs DestinationsAsync iterates only
NavigationDestinations.ForAsync (PageRegistry.All). PageAgentContext.Identity
already prefix-matches /DesignSystem/*, so only the destination list needs the
fix. Keep the SiteMapCap budget in mind (30+ parts).
