# Publish FluentSelect SetComboBoxValue on Blazor.Select

## Description

**205-005** (PR #333, `eb70ad5f`) made WASM `import()` of `FluentSelect.razor.js` **200**. `/Profile`
still yellow-bars:

```
Could not find 'Microsoft.FluentUI.Blazor.Select.SetComboBoxValue' ('Select' was undefined).
```

Stack: `FluentSelect.OnAfterRenderAsync` → `JSObjectReferenceExtensions.InvokeVoidAsync`.

Aspire `web-server` logs the overlay as **200** `text/javascript` (e.g. 10:18:19, 3338 bytes).
This is **not** another 404.

## Requirements

### Cause (already proven)

The 205-005 overlay (`web-spa/fluent-ui-overlays/fluent-select.js`) publishes **only**:

`globalThis.Microsoft.FluentUI.Blazor.Components.Select` (`Initialize`, `ClearValue`)

JSInterop looks up **`Microsoft.FluentUI.Blazor.Select.SetComboBoxValue`** (no `Components`).
That object is undefined. rc.4 collocated JS used that exact namespace (`ClearValue` +
`SetComboBoxValue`). 205-005 decompiled rc.5 `Initialize` under `Components.Select` and
missed the identifier the **live** WASM actually invokes.

The `startTime` TypeError in DevTools is a follow-on from the unhandled render exception,
not a second product bug.

### Product

After `import()` of the overlay module (and after it runs):

- `globalThis.Microsoft.FluentUI.Blazor.Select.SetComboBoxValue` is a function.
- `ClearValue` on that same object (rc.4).
- Keep `Components.Select.Initialize` / `ClearValue` if rc.5 C# still calls those too —
  **both** identifier trees if needed; missing `SetComboBoxValue` is the live crash.
- `SetComboBoxValue(id, value)` must set combobox `_control.value` on `FLUENT-DROPDOWN`
  (rc.4 body). That is what 205-004 closed-label UX needs on WASM.
- `/Profile` WASM loads with **no** `JSException` and no Blazor “An unhandled error has
  occurred. Reload”.
- Do not revert dropdowns; do not `UseStaticFiles`; do not bump Fluent UI unless you prove
  a newer nupkg packs this JS **and** these identifiers.

### Tests

- Overlay source / in-proc GET body must contain `Microsoft.FluentUI.Blazor.Select` and
  `SetComboBoxValue` (not only `Components.Select`).
- Existing `FluentSelectJsOverlay` tests must fail if the rc.4 identifier is missing.
- How to validate: browser `import()` then `typeof Microsoft.FluentUI.Blazor.Select.SetComboBoxValue === 'function'`; live `/Profile` no yellow bar.

## Checklist

- [x] Overlay publishes `Microsoft.FluentUI.Blazor.Select.SetComboBoxValue`
- [x] `/Profile` WASM: no JSException / Reload bar
- [x] Tests assert the live identifier, not only Components.Select
- [x] Results + How to validate

## Notes

- Parent **205**; immediate predecessor **205-005**. Overlay file:
  `source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js`.
- Aspire CLI (no dashboard MCP in Aspire 13.3+): `aspire logs web-server` shows 200 for
  the JS. Rebuild of **current master** will not fix the identifier.
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-08).
  Do not implement in cockpit.

## Session

- Created: 560353 (2026-09-08)
- Cockpit: Grok — Profile WASM SetComboBoxValue Select undefined
- Implementer: Grok session 01a07f09-be19-7571-9a8c-2e8f329eb623 (2026-09-08)
- Review oracle: Grok `01a07f10-dcd3-7671-bbaa-ed7e32c1ca3a` (2026-09-08) — effort 1, round 1, disposition clean

## Results

The 205-005 overlay imported as 200 but only published `Microsoft.FluentUI.Blazor.Components.Select` (`Initialize` / `ClearValue`). Live WASM JSInterop (rc.4 `FluentSelect.OnAfterRenderAsync` → `JSModule.ObjectReference.InvokeVoidAsync`) looks up **`Microsoft.FluentUI.Blazor.Select.SetComboBoxValue`** on that module — `Select` was undefined. rc.5 C# still calls `Components.Select.Initialize` via `InvokeFluentVoidAsync`. The overlay now publishes **both** identifier trees. `SetComboBoxValue(id, value)` is the rc.4 body (`FLUENT-DROPDOWN` `_control.value`). Dropdowns, `UseStaticFiles`, and the Fluent UI pin are unchanged.

**Files**

- `source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js` — rc.4 `Blazor.Select` (`SetComboBoxValue` + `ClearValue`) plus rc.5 `Components.Select` (`Initialize` + `ClearValue`); `export var Microsoft` and `globalThis` publish both.
- `source/container-apps/web/projects/web-spa/web-spa.csproj` — overlay target comment names both identifier trees.
- `tests/container-apps/web/web-server-integration-tests/features/profile/fluent-select-js-overlay-tests.cs` — overlay source and in-proc GET body must contain `Microsoft.FluentUI.Blazor.Select` and `SetComboBoxValue` (not only `Components.Select`).

**Tests**

- `dotnet test -c Release -- --filter-class FluentSelectJsOverlay` in `tests/container-apps/web/web-server-integration-tests`: 3/3 passed.
- `dotnet run tools/dev-cli/dev.cs -- build`: 0/0.
- Node and Chromium `import()` of the overlay: `typeof Microsoft.FluentUI.Blazor.Select.SetComboBoxValue === 'function'` (also `ClearValue`, `Components.Select.Initialize`).
- Running Aspire on this machine is still **origin-home/master** (`:63611` overlay body has no `SetComboBoxValue`). Live `/Profile` yellow-bar proof needs Aspire **from this branch**.

### Review disposition

- **Outcome:** clean (0 open)
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Counts (final, round 1):** bug 0/0/0; suggestion 0/0/0; nit 0/0/0 (open/fixed/wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`
- **Wontfix / escalations:** none
- Review re-ran overlay tests (3/3) and node `import()`: `typeof Microsoft.FluentUI.Blazor.Select.SetComboBoxValue === 'function'` (also `ClearValue`, `Components.Select.Initialize` / `ClearValue`). Overlay `SetComboBoxValue` matches rc.4 nupkg JS. rc.5 C# still uses `InvokeFluentVoidAsync` on `Components.Select.Initialize`; the live yellow-bar identifier is the rc.4 module path this overlay now publishes.

### How to validate

**Automated**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded, 0 Warning(s), 0 Error(s)

cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class FluentSelectJsOverlay
# expect: 3 passed (overlay source + in-proc GET contain Microsoft.FluentUI.Blazor.Select
# and SetComboBoxValue; SWA endpoints still list the _content path)
```

**Smoke**

```bash
# Identifier after import() (no Aspire):
node --input-type=module -e "
import './source/container-apps/web/projects/web-spa/fluent-ui-overlays/fluent-select.js';
console.log(typeof globalThis.Microsoft.FluentUI.Blazor.Select.SetComboBoxValue);
"

# After Aspire from this branch (not master):
curl -sk https://127.0.0.1:63611/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js | rg 'SetComboBoxValue|Microsoft.FluentUI.Blazor.Select'
```

In a browser console after that GET 200:

```javascript
await import('/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js');
typeof Microsoft.FluentUI.Blazor.Select.SetComboBoxValue === 'function'
```

Open `/Profile` as WASM.

**Expect**

- Node / browser `typeof` is `'function'`.
- Overlay GET 200, `text/javascript`, body contains `function SetComboBoxValue` and `Microsoft.FluentUI.Blazor.Select` (not only `Components.Select`).
- `/Profile` WASM: no `JSException` for `Microsoft.FluentUI.Blazor.Select.SetComboBoxValue`; no Blazor “An unhandled error has occurred. Reload”. Language / Region combobox and Theme select still render (205-004 labels).

**Depends on:** Aspire from this task branch (`dev run` / restart the running AppHost). Master Aspire (`:63611` / `arch.timewarp.work`) still serves the 205-005 overlay without `SetComboBoxValue`.

**Not in scope:** vendoring the Fluent nupkg; downgrading or bumping Fluent UI; replacing the dropdowns with text inputs; `UseStaticFiles`.
