# Upgrade FluentUI to v5 RC + wire providers

Parent: 059. Pairs with 059-004 (must land together — the app won't build between).

## Goal

Bump FluentUI Blazor `4.14.2` → `5.0.0-rc.x` and update the app-level wiring to the v5 model.

## Tasks

- [ ] Update `Directory.Packages.props` (root): `Microsoft.FluentUI.AspNetCore.Components`,
      `.Emoji`, `.Icons` → the v5 RC version (crunchit uses `5.0.0-rc.3-26138.1`; pick the
      latest RC at implementation time). Confirm `.Emoji` / `.Icons` ship a matching v5.
- [ ] `program.cs` (web-spa): adopt the v5 registration. crunchit uses
      `builder.Services.AddFluentUIComponents(configuration => configuration.UseGlobalOverlay = false);`
      — verify the v5 options surface and set ours accordingly.
- [ ] Replace the per-feature provider components with the v5 **`<FluentProviders />`**
      single component placed in the layout (crunchit puts it at the end of `MainLayout`).
      Remove the individual `FluentToastProvider`/`FluentDialogProvider`/`FluentTooltipProvider`/
      `FluentMenuProvider`/`FluentMessageBarProvider`/`FluentKeyCodeProvider` usages and the
      `FluentUIRequiredFeatures` component (verify v5's initialization requirements).
- [ ] CSS/asset wiring: the v5 reference links only `css/tokens.css` + the scoped-css bundle
      (`<Project>.styles.css`) in `index.html`/`App.razor` — there is **no** separate
      `fluent.css` link. Determine how v5 ships its base CSS and update `App.razor`
      (`web-server/components/App.razor:18-19`) accordingly. Remove the `fluent.css` link if
      v5 bundles via scoped CSS.
- [ ] Remove the csproj dummy-CSS-file hacks (`CreateDummyFluentUICSS`,
      `CreateDummyQuickGridCSS`) — verify they're no longer needed under v5 (they were a v4
      scoped-bundle workaround). (Final removal coordinated with 059-006.)

## Notes

- Use the **FluentUI MCP server** for v5 docs/migration guidance (search docs:
  `mcp__aspire`-style tools are unrelated; the FluentUI library exposes a migration MCP per
  the maintainers). Migrate component-by-component using its "how to migrate" docs.
- v4 is supported until **Nov 2026** — no deadline pressure, but we're going clean now.
