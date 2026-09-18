# Implement Ctrl-K command palette (GitHub issue 102)

## Description

Ship the **Ctrl-K / Cmd-K command palette** in web-spa. Today
`TimeWarpPage` shows a `FluentTextInput` with a “Ctrl-K” badge and no
hotkey, overlay, or catalog. GitHub
[issue 102](https://github.com/TimeWarpEngineering/timewarp-architecture/issues/102)
(open since 2021, empty body) never had a kitchen.

This is the **product** follow-up to architecture **238**, which only
evaluated Jev as a ranker and **deferred** it. v1 uses a **deterministic
C# shortlist** (238: that path is useful without Jev). Do **not** call
TypeSafe / OpenRouter on this id.

## Requirements

- [ ] Ctrl-K and Cmd-K open a palette overlay from `TimeWarpPage`
      (interactive shell only). Esc / click-outside close. Login /
      `TimeWarpFocusedPage` stay without search chrome (147-005)
- [ ] Appbar search field is a real affordance: focus or click also
      opens the same overlay (not a dead `FluentTextInput`)
- [ ] Results include **navigation** (NavMenu destinations) and
      **opt-in commands** from the 238 catalog contract. One roster
      shape: `name`, `description`, execute (navigate or dispatch)
- [ ] Ranking is C# shortlist / fuzzy filter on name + description.
      No Jev. No `[TrackAction]` as the opt-in (busy-indicator)
- [ ] Keyboard: type to filter, arrows, Enter runs the highlighted
      row, Esc closes. Do not auto-dispatch on a low-confidence
      near-miss — show the list
- [ ] Permissions stay in code: do not list commands the current
      principal cannot run
- [ ] Close GitHub **#102** from Results when the palette ships (or
      comment why it stays open). Do not close it from this kitchen
      create
- [ ] WebMCP `registerTool` and filling 104-019 `tools[]` are **not**
      this id — mint a child if the same catalog should feed agents

## Checklist

- [ ] Overlay + hotkey on `TimeWarpPage` (desktop and a mobile
      viewport check)
- [ ] Nav destinations indexed from the live menu, not a second
      hand-copied route list
- [ ] Opt-in command list (238 `catalog.json` as the stand-in until a
      timewarp-state catalog child exists)
- [ ] Tests: open/close, filter, Enter navigates, excluded internals
      (Fetch*, Clear*, template FiveSecondTask) stay out
- [ ] Implementation review; host `open-pr`

## Session

- Created: 3920681 (2026-09-18)
- Cockpit: grok flow session `01a0b298-4e32-7733-9d5b-1c7c09b95f02` (2026-09-18)

## Notes

**Not already tasked.** Kanban **102** is a different item (archived
Cloudflare edge profile). Architecture **238** is research-only: defer
Jev; `TimeWarpPage` search still decorative; GitHub #102 left open on
purpose.

**238 catalog contract (reuse, do not re-litigate)**

| Field | Source |
|-------|--------|
| `name` | Stable id, e.g. `Credentials.AddPasskey` or a route id |
| `description` | Purpose first sentence or a small attribute — not Jev, not hollow XML (177) |
| `execute` | Navigate, or generated `State.Method(args)` |
| visibility | human for this id; agent later |

Do not catalog: Fetch* page-load, Clear* internals, inbound hub
actions, Debug, template `FiveSecondTask` / `TwoSecondTask`,
`ThrowException`.

**Repo split (238)**

- Palette UI lives **here** (web-spa)
- Catalog **shape** + source-gen (`[CatalogAction]` / Purpose harvest)
  is a **timewarp-state** child if v1 outgrows the stand-in roster.
  Do not block v1 on that child if NavMenu + a hand-maintained command
  list can ship

**Out of scope**

- Jev / TypeSafe API (238 defer; taratibu 014 same key blocker)
- WebMCP / `navigator.modelContext` / 104-019 tools
- Changing `[TrackAction]` meaning

## Results

*(fill when the palette ships)*

### How to validate

*(required before done)*
