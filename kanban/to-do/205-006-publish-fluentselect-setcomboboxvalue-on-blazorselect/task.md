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

- [ ] Overlay publishes `Microsoft.FluentUI.Blazor.Select.SetComboBoxValue`
- [ ] `/Profile` WASM: no JSException / Reload bar
- [ ] Tests assert the live identifier, not only Components.Select
- [ ] Results + How to validate

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
