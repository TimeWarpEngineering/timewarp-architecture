# Serve missing FluentSelect.razor.js so Profile WASM loads

## Description

Opening `/Profile` (WASM) throws:

```
Failed to fetch dynamically imported module:
https://arch.timewarp.work/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js
```

`FluentSelect.OnAfterRenderAsync` → `FluentJSModule.ImportJavaScriptModuleAsync`. That URL is
**HTTP 404** on web-server (`:63611`), localhost ingress (`:63610`), and `arch.timewarp.work`.
Empty body. Not a Caddy-only issue.

Sibling `_content` **does** work: `…/Components/Tooltip/FluentTooltip.razor.js` is **200**.
`MapStaticAssets` is serving Fluent UI assets that exist in the nupkg.

## Requirements

### Cause (already proven — do not re-litigate hosting)

`Microsoft.FluentUI.AspNetCore.Components` **`5.0.0-rc.5-26219.1`** (Directory.Packages.props)
**does not pack** `staticwebassets/Components/List/FluentSelect.razor.js`.

That file **is** in rc.2 / rc.3 / rc.4. The rc.5 nupkg has no `List/` JS at all. rc.5 C# still
imports collocated `FluentSelect.razor.js` and invokes
`Microsoft.FluentUI.Blazor.Components.Select.Initialize` / `ClearValue`.

`/Profile` uses Theme `FluentSelect` plus Language/Region `FluentCombobox` (inherits
`FluentSelect`). InteractiveAuto can look fine on the **server** circuit; WASM hydrate
`import()`s the missing URL and yellow-bars.

Nuget.org latest as of 2026-09-08 is still `5.0.0-rc.5-26219.1` — there is no newer package
that restores the file.

### Product

- `GET /_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js`
  must **200** with a JS module (not HTML, not 0-byte).
- `/Profile` WASM must load without that `JSException`. Language / Region / Theme stay
  Fluent combobox/select (205-004 labels, 205-002 catalogs). Do **not** revert to text
  inputs to dodge the 404.
- The module must export the identifiers rc.5 C# actually calls (`Select.Initialize`,
  `Select.ClearValue` — decompile `FluentSelect` / `FluentJSModule`; rc.4 JS only has
  `ClearValue` + `SetComboBoxValue` under `Microsoft.FluentUI.Blazor.Select`, which may
  not match rc.5). Import succeeding then `Initialize` throwing is still a fail.
- Prefer a **static web asset at that exact `_content/…` path** (overlay / local file
  MapStaticAssets can see). Do not add `UseStaticFiles` (web-server Program forbids it —
  fingerprint / resource-collection). Do not blindly downgrade the whole Fluent UI package
  without proving Profile + other Fluent pages.
- Comment the overlay as a stopgap until Fluent UI packs the JS again. Do not vendor the
  entire Fluent nupkg.

### Tests / demo

- curl the `_content` URL (web-server and ingress) → 200, `application/javascript` (or
  equivalent), body starts with JS.
- Live `/Profile` after Aspire restart on this branch: no FluentSelect.razor.js fetch
  error; form renders.
- Theme / Language / Region still bind.

## Checklist

- [ ] FluentSelect.razor.js 200 at the `_content` path WASM imports
- [ ] `/Profile` WASM loads; no JSException
- [ ] Dropdowns / catalogs / 205-004 labels unchanged
- [ ] Results + How to validate (curl + live Profile)

## Notes

- Parent **205**. Trigger: cockpit demo after 205-004 merge (`f183c9c1`); Aspire from
  architecture master, ingress `https://arch.timewarp.work`.
- Package path: `~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.0-rc.5-26219.1/`
  — `find … -name FluentSelect.razor.js` empty; rc.4 has the file.
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-08).
  Do not implement in cockpit.

## Session

- Created: 440982 (2026-09-08)
- Cockpit: Grok — Profile WASM 404 FluentSelect.razor.js
