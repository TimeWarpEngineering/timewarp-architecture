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

- [x] FluentSelect.razor.js 200 at the `_content` path WASM imports
- [x] `/Profile` WASM loads; no JSException
- [x] Dropdowns / catalogs / 205-004 labels unchanged
- [x] Results + How to validate (curl + live Profile)

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
- Implementer: Grok session 01a07eb6-abb1-7851-8acd-133554bcbeaf (2026-09-08)
- Review oracle: Grok `01a07ecb-e410-7190-8016-b49719b72366` (2026-09-08) — effort 1, round 1, disposition clean

## Results

Fluent UI `5.0.0-rc.5-26219.1` still calls `Microsoft.FluentUI.Blazor.Components.Select.Initialize` / `ClearValue` and WASM `import()`s collocated `Components/List/FluentSelect.razor.js`, but the nupkg does not pack that file (Fluent UI #5074 moved the source into `Core.Scripts` / `lib.module.js` and deleted the collocated `.razor.ts`). This task overlays that one module at the exact `_content` path `MapStaticAssets` serves. Profile still uses Fluent combobox/select (205-004 labels, 205-002 catalogs). No Fluent UI package downgrade; no `UseStaticFiles`.

**Files**

- `source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js` — rc.5 `FluentSelect.ts` body (`Initialize` writes combobox `_control` text + a11y; `ClearValue`); `export var Microsoft` plus `globalThis` publish. Local name is not `.razor.js` (BLAZOR106).
- `source/container-apps/web/projects/web-spa/web-spa.csproj` — `AddFluentSelectJsOverlayStaticWebAsset` maps the file to `/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js` as a Web.Spa discovered asset (project assets flow to web-server; a Package SourceId overlay was dropped at the host).
- `tests/container-apps/web/web-server-integration-tests/features/profile/fluent-select-js-overlay-tests.cs` — overlay source identifiers, host SWA endpoints list the path, in-proc GET 200 + `text/javascript` + `Select.Initialize` / `ClearValue`.

**Tests**

- `dotnet test -c Release -- --filter-class FluentSelectJsOverlay` in `tests/container-apps/web/web-server-integration-tests`: 3/3 passed.
- `dotnet run tools/dev-cli/dev.cs -- build`: 0/0.
- Live Aspire on this machine is still **master** (`:63611` / `arch.timewarp.work` still 404 for the missing nupkg file). Restart Aspire **from this branch** for `/Profile` WASM.

### Review disposition

- **Outcome:** clean (0 open)
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Counts (final, round 1):** bug 0/0/0; suggestion 0/0/0; nit 0/0/0 (open/fixed/wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`
- **Wontfix / escalations:** none
- Review re-ran the overlay tests (3/3) and confirmed web-server Release SWA endpoints list the unfingerprinted `_content/…/FluentSelect.razor.js` route as `text/javascript`. rc.5 C# uses `InvokeFluentVoidAsync` (globals already published by `lib.module.js`) rather than `import()` of the collocated file; the overlay still meets the GET-200 requirement.

### How to validate

**Automated**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded, 0 Warning(s), 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class FluentSelectJsOverlay
# expect: 3 passed (overlay source exports Initialize/ClearValue; web-server SWA
# endpoints list the _content path; in-proc GET 200 text/javascript)
```

**Smoke**

```bash
# After Aspire from this branch (not master):
curl -skI https://127.0.0.1:63611/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js
curl -skI https://127.0.0.1:63610/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js
curl -sk https://127.0.0.1:63611/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js | head -5
```

**Expect**

- HTTP 200, `content-type: text/javascript` (or `application/javascript`).
- Body starts with a JS comment or `export var Microsoft`, contains `Select.Initialize` and `Select.ClearValue`, not HTML, not 0-byte.
- Sibling Tooltip URL still 200.
- Open `/Profile` as WASM: no `Failed to fetch dynamically imported module: …/FluentSelect.razor.js`. Language / Region `FluentCombobox` and Theme `FluentSelect` still render and bind (205-004 labels).

**Depends on:** Aspire from this task branch (`dev run` / existing dcp). Master Aspire will keep 404 until this ships.

**Not in scope:** vendoring the Fluent nupkg; downgrading Fluent UI; replacing the dropdowns with text inputs.
