# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit dc4e009e4 vs 892eb484d (source/, skills/, tests/), plus the call sites of `AgentSiteMap.DestinationsAsync` (`WebMcpDiscoveryCatalog.DocumentJsonAsync`, `PageLoadJsonAsync`, `CatalogAgentToolSet`, `AgentNavigate.InvokeAsync`)

## Summary

The change makes `CommandPaletteRoster.DeepLinks(page)` the single source of a page's deep links (the `DesignSystemParts.PageOrder` parts for the Design System page, else none). Ctrl-K now builds its part rows from it, and `AgentSiteMap.DestinationsAsync` appends the same links after the menu pages, each with a `parent` url, only when the parent page passed the policy filter. `navigate`'s enum lists every part url; its description and the Ask summary collapse a parent's parts to one `{parent}/<part> for {Title} parts: a, b, …` line (`AgentSiteMap.DeepLinkLines`), so 30+ parts stay under `MaxToolDescriptionLength` and `SiteMapCap`. Invoke-time refusal is unchanged (it rebuilds the destinations for the principal), so a principal without the Design System page still gets `Refusal` for a part url.

Risk is low. The gating is inherited from the existing policy filter rather than re-implemented, the WebMCP discovery document and page-load payload pick up the parts through the same `DestinationsAsync` call, and the `<` in the new description line is already escaped by `PageLoadJsonAsync`. The parity test pins palette, navigate enum and site_map to the same `[pages…, parts…]` order for every principal, and the new catalog-agent test covers the positive and refused paths.

## Verification

Re-run in this worktree (not taken from the implementer's Results):

- `./bin/dev build`: 0 warnings, 0 errors.
- web-spa-integration-tests `--filter-class`: Destinations_Should_ 8/8, CatalogAgent_Should 48/48 (includes the new `Site_Map_And_Navigate_Include_Design_System_Parts` and the in-app vs WebMCP parity test), CommandPalette_Should_ 45/45, AskReference_Should_ 9/9, AskTag_Should_ 10/10, DesignSystemPage_Should_ 6/6.
- `./bin/dev lint-skills`: 9/9 passed (tw-blazor SKILL.md changed).

Goal restatement vs original task: the restatement adds acceptance detail (single source, gating, caps, Design-region updates, tests) and matches the task's description, notes and checklist. No misread.

## Issues

None. The diff does what the task asks, the comments it rewrites are now true, and no finding reached `nit` worth recording (the per-parent title lookup in `DeepLinkLines` is a linear scan over a list with one parent today, which is not worth a change).
