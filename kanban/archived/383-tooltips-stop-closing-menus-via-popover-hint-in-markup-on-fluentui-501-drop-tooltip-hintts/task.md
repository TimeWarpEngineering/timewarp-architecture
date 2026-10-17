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

## Goal (implementer restatement)

A FluentUI tooltip shows as an `auto` popover. Showing it light-dismisses any other open `auto` popover, including the Ask edit-mode menu and the settings menu. Task 371 stopped that by wrapping `fluent-tooltip.showPopover` in `tooltip-hint.ts`. Steve rejected that: the markup can set `popover="hint"` before the element connects, and `connectedCallback`'s `popover ??= 'auto'` leaves a value that is already set.

This task says it in markup, on FluentUI 5.0.1, and deletes the script.

Done when:

- `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` are 5.0.1, and `dev build` is 0 warnings and 0 errors.
- Every tooltip that can show while an auto menu is open is an explicit `FluentTooltip` with `popover="hint"` (Ask answer bar, header actions, Tag, screenshot, Stop). DataGrid column tooltips stay when they cannot show over an open menu. The audit is in Results.
- `tooltip-hint.ts` and its import are gone. If the attribute does not hold under load, stop and report it. Do not bring the script patch back.
- The Playwright checks still show the Copy tooltip with the edit-mode menu open and with the settings menu open, and the chosen item stays visible.
- `AskSurface_Given_Wasm` passes 50/50 under the CPU-burner load, on this final code.
- `dev test --group playwright`, `ganda repo audit`, and `dev check-version` pass.

## Checklist

- [ ] FluentUI + Icons 5.0.1; build 0/0
- [ ] Explicit `FluentTooltip popover="hint"` at menu-adjacent sites; audit recorded
- [ ] `tooltip-hint.ts` and its import removed
- [ ] `AskSurface_Given_Wasm` 50/50 under load on final code
- [ ] Gates: `dev build`, `dev test --group playwright`, `ganda repo audit`, `dev check-version`

## Session

- Created: 866285 (2026-10-17), from Steve's review of 371 (PR #531, merged onto
  feature/agent-surface-hardening)
- 2026-10-17: markup attempt stopped. `popover="hint"` is cleared when `fluent-tooltip`
  connects. Product edits (5.0.1 pin, `HintTooltip`, deletion of `tooltip-hint.ts`) were
  reverted. The 371 script is still the branch code. No new script was added.
  `dev check-version` passed (`2.0.0-beta.20` ahead of NuGet `2.0.0-beta.19`).
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Blocked — 2026-10-17T15:53:47Z

## Results

Stopped under the task exit. Markup cannot keep `popover="hint"` on `fluent-tooltip` in
FluentUI 5.0.1. The attempt was reverted, so this branch still has `tooltip-hint.ts` from
task 371. That script was left as it already was. It was not brought back as a new patch.

The 5.0.1 bundle still has `this.popover ??= "auto"` in `connectedCallback`
(`Microsoft.FluentUI.AspNetCore.Components.lib.module.js`). That assignment is not what
clears a hint. The tooltip template is:

```html
<template popover aria-hidden="true">
  <slot></slot>
</template>
```

FAST applies those template attributes to the host on the first render, which runs from
`connectedCallback`. The boolean `popover` attribute is `setAttribute("popover", "")`.
That write happens after a markup attribute is already on the element, and the empty
value is the `auto` state. Chromium then reports `getAttribute("popover") === ""` and
`element.popover === "auto"`.

Isolated Chromium (Playwright, FluentUI 5.0.1 static asset, `beforeStart` so
`fluent-tooltip` is defined):

- `setAttribute("popover", "hint")` before append, a property assignment before append,
  and `innerHTML` with `popover="hint"` all become `attr=""` / `prop=auto` on connect.
  The write stack is `setAttribute` ← FAST `updateTarget` ← `bind` ← `render` ←
  `renderClientSide` ← `renderTemplate`.
- `setAttribute("popover", "hint")` after the element is connected stays `hint`. A second
  `connectedCallback()` leaves it `hint`, because `??= "auto"` does not replace a value
  that is already set.

App run of the same attempt (`HintTooltip` rendered
`<FluentTooltip UseTooltipService="false" popover="hint">`, `dev build` 0 warnings and
0 errors, `tooltip-hint.ts` removed). `AskSurface_Given_Wasm` failed in about 10 seconds.
Every rendered tooltip was `attr=` empty and `prop=auto`: New conversation, Expand,
Close, Copy, Thumbs up, Thumbs down, Tag an element on the page, Attach a screenshot of
the page. Anchors matched the button ids (`ask-copy` and the rest). Showing the Copy
tooltip closed the open menu. The assertion value began `:closed id=ask-copy`.

`Tooltip=` audit on web-spa, from the branch before the attempt was reverted. Menu-adjacent
shortcuts are the nine Ask icon buttons: Copy, Thumbs up, Thumbs down, New conversation,
Expand, Close, Tag, Attach screenshot, Stop. DataGrid `TemplateColumn Tooltip="true"` on
`FeedbackAdminListPage` (Owner) and `PrincipalsPage` (Principal) are admin cell hints.
They stay. The shell profile menu has no `Tooltip=`.

`AskSurface_Given_Wasm` 50/50, `dev test --group playwright`, and `ganda repo audit` were
not run. The single unstressed assertion already met the exit. `dev check-version` passed:
source is `2.0.0-beta.20`, one prerelease increment ahead of NuGet `2.0.0-beta.19`, so the
FluentUI pin would not have required a `<Version>` bump.
Upstream issue link is still pending in Notes.

### How to validate

Smoke:

Load the 5.0.1 module
`Microsoft.FluentUI.AspNetCore.Components.lib.module.js`, call its `beforeStart`, then in
the page:

```javascript
const el = document.createElement("fluent-tooltip");
el.setAttribute("popover", "hint");
document.body.append(el);
```

Expect:

- Immediately after `append`, `el.getAttribute("popover")` is `""` and `el.popover` is
  `"auto"`.
- `el.setAttribute("popover", "hint")` after that append leaves both the attribute and
  the property as `"hint"`, including after another `el.connectedCallback()`.

The in-app check on the reverted attempt was `AskSurface_Given_Wasm` from
`tests/container-apps/web/web-spa-playwright-tests`. Expect the Copy tooltip's popover
mode to be `hint` while the edit-mode item and the settings item stay visible. On the
markup attempt the mode was `:closed` and every tooltip property was `auto`.

## Notes

- Runs on `feature/agent-surface-hardening` after 308 so it lands before that branch's master PR.
- Upstream issue: draft prepared for Steve (microsoft/fluentui web-components tooltip should not
  light-dismiss open popovers). Link pending.

## Blocked

2026-10-17: Steve must choose the next approach: fluent-tooltip's template render sets popover to auto on connect, so markup popover="hint" does not survive under FluentUI 5.0.1. A library change that keeps a host popover value would also unblock a markup retry.

## Decision

2026-10-17, Steve: keep task 371's `tooltip-hint.ts` for now. Do not use the C# second-render
workaround. Follow-up: backlog task **385** retires the script once FluentUI keeps a host popover
value. Archived: this task's spec ("drop tooltip-hint.ts via markup") can't be done on 5.0.1.
The evidence above is the record. The original task branch (tip `bfb802d99`, kanban notes only) was
deleted; its notes are folded in here.
