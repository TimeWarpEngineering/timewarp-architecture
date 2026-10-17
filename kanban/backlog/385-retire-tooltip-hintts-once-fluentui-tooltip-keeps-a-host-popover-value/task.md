# Retire tooltip-hint.ts once FluentUI tooltip keeps a host popover value

## Description

`web-spa/source/features/tooltip-hint.ts` (task 371) wraps `fluent-tooltip`'s `showPopover` and
sets `popover="hint"` before each show, so a tooltip does not light-dismiss an open menu. Steve
accepts it **for now only** (2026-10-17). Patching library internals from script is not the
pattern this template wants (see `no-typescript-patches-of-library-internals`).

Task 383 proved that markup cannot do it in FluentUI 5.0.1. The tooltip template is
`<template popover aria-hidden="true">`, and FAST's first render on connect writes
`popover=""` (`auto`) over any value already on the host. Setting `hint` *after* connect does
stick. Full evidence and a JS repro are in archived task 383 (`## Results`, `### How to validate`).

## Requirements

- Watch upstream: microsoft/fluentui web-components tooltip (issue drafted for Steve; link below
  once posted) and `Microsoft.FluentUI.AspNetCore.Components` releases after 5.0.1.
- When a release lets a host `popover` value survive connect, or defaults the tooltip to
  `hint`/`manual`, or `FluentTooltip`/`TooltipOptions` exposes the popover type:
  - bump `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` to it;
  - express the hint in C#/markup at the nine menu-adjacent Ask icon buttons (Copy, Thumbs up,
    Thumbs down, New conversation, Expand, Close, Tag, Attach screenshot, Stop). Leave the
    DataGrid `TemplateColumn Tooltip=` admin cell hints alone (383 audit);
  - delete `tooltip-hint.ts` and its import in `web.spa.lib.module.ts`.
- Proof: `AskSurface_Given_Wasm` 50/50 under the 371 CPU-burner load loop on the final code.
- Not in scope unless Steve picks it: the C#-only second-render workaround (render the tooltip, then
  add `popover="hint"` in the next render so it lands after connect). Offered 2026-10-17 and not
  chosen.
- Gates: `dev build` 0/0, `dev test --group playwright`, `ganda repo audit`, `dev check-version`.

## Checklist

- [ ] Upstream issue posted; link recorded here
- [ ] A FluentUI release keeps a host popover value (or exposes the type)
- [ ] Bump + markup hint at the nine Ask buttons; `tooltip-hint.ts` removed
- [ ] 50/50 under load; gates green

## Session

- Created: 1693511 (2026-10-17), from Steve's decision on 383 (keep 371's script for now)

## Notes

- Spec is complete. It sits in backlog because it is waiting on upstream, not on elaboration.
- Upstream issue link: pending (Steve posts it).
