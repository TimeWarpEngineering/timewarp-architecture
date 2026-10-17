# Tooltips stop closing menus via popover hint in markup on FluentUI 5.0.1, drop tooltip-hint.ts

## Description

Task 371 found the cause correctly: a FluentUI tooltip shows as an `auto` popover on a timer, and
that light-dismisses any open `auto` popover, including the Ask edit-mode menu and the settings
menu. But the fix (`web-spa/source/features/tooltip-hint.ts`) patches `fluent-tooltip`'s
`showPopover` prototype from TypeScript. Steve rejected that approach (2026-10-17): no
monkey-patching library internals from script when the markup can say it.

Evidence that markup can say it: `fluent-tooltip`'s `connectedCallback` does
`this.popover ??= 'auto'` (microsoft/fluentui `packages/web-components/src/tooltip/tooltip.ts:88`;
same in the 5.0.0 `Microsoft.FluentUI.AspNetCore.Components.lib.module.js` bundle). A
`popover="hint"` attribute present at connect survives, because `??=` only fills a missing value.

`FluentTooltip` has no popover-type parameter, and the `Tooltip="…"` shortcut on buttons renders
through `ITooltipService`, so markup cannot add attributes there. Explicit
`<FluentTooltip Anchor="…" popover="hint">` passes the attribute through `AdditionalAttributes`.

## Requirements

- Bump `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` to **5.0.1** (latest; released
  2026-10-14) in `Directory.Packages.props`. Clear stale WASM assets if needed (`dev clean`).
- Replace the `Tooltip="…"` shortcut with explicit `<FluentTooltip Anchor="…" popover="hint">` at
  every site that can show while an `auto` popover (menu) is open. At minimum: `AskAnswerBar.razor`
  (Copy, Thumbs up/down), `AgentAsk.razor` (New conversation, Expand, Close, Tag, Attach
  screenshot), `AskSendButton.razor` (Stop). Audit the rest of web-spa for the same pattern and
  decide per site. If many sites repeat the same markup, a small shared component is fine
  (`tw-blazor`, `tw-feature-placement`).
- Keep the DataGrid `TemplateColumn Tooltip=` uses unless they can show over an open menu.
- Delete `tooltip-hint.ts` and its import in `web.spa.lib.module.ts`.
- Keep the 371 test assertions: show the Copy tooltip with the edit-mode menu open and with the
  settings menu open, and require the chosen item to stay visible.
- **Proof:** `AskSurface_Given_Wasm` 50/50 under the 371 CPU-burner load loop, on the final code
  (371 ran the loop only before its review fixes).
- **Exit:** if `popover="hint"` in markup does not hold under load (for example the attribute is
  lost on re-render, or the service path cannot be avoided), stop and report to Steve with
  evidence. Do not reintroduce a script patch.
- Record the upstream issue link in Notes once Steve posts it.
- Gates: `dev build` 0/0, `dev test --group playwright`, `ganda repo audit`, `dev check-version`
  (package pin changed).

## Checklist

- [ ] FluentUI + Icons 5.0.1; build 0/0
- [ ] Explicit `FluentTooltip popover="hint"` at menu-adjacent sites; audit recorded
- [ ] `tooltip-hint.ts` and its import removed
- [ ] `AskSurface_Given_Wasm` 50/50 under load on final code
- [ ] Gates: `dev build`, `dev test --group playwright`, `ganda repo audit`, `dev check-version`

## Session

- Created: 866285 (2026-10-17), from Steve's review of 371 (PR #531, merged onto
  feature/agent-surface-hardening)

## Notes

- Runs on `feature/agent-surface-hardening` after 308 so it lands before that branch's master PR.
- Upstream issue: draft prepared for Steve (microsoft/fluentui web-components tooltip should not
  light-dismiss open popovers). Link pending.
