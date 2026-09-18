# Evaluate Jev for Ctrl-K and WebMCP action catalog

## Description

Decide whether TypeSafe Jev should rank TimeWarp.State ActionSets for
**Ctrl-K** (humans) and **WebMCP** (browser agents), over **one shared
opt-in catalog**. Do **not** implement the palette UI, a production
`JevOracleProvider`, or a TimeWarp TypeSafe SDK on this id.

Jev ranks a closed catalog. It does **not** author descriptions, walk
the IR, generate JSON Schema, or replace ActionSet codegen.

Origin: flow cockpit, after taratibu **014** (classify oracles). GitHub
[issue 102](https://github.com/TimeWarpEngineering/timewarp-architecture/issues/102)
(2021, empty body) never became a kitchen. Ctrl-K in `TimeWarpPage` is
chrome only.

## Requirements

- [ ] Catalog decision recorded: new opt-in (not `[TrackAction]`),
      fields `name` / `description` / `inputSchema` / `execute` /
      visibility; description SSOT is Purpose region or a small
      attribute — not Jev, not hollow XML (see **177**)
- [ ] Repo split recorded: catalog **shape** + source-gen →
      **timewarp-state** child if we adopt; Ctrl-K UI +
      `navigator.modelContext` → this repo (web-spa)
- [ ] Ranking experiment run (or blocked on missing
      `TYPESAFE_API_KEY` / gateway, blocker named). Client path
      follows taratibu **014**; do not fork a second SDK decision
- [ ] Experiment compares Jev Choice + “is this a command?” Noul
      against labeled phrases; confidence log-only; auto-dispatch vs
      show-shortlist counted separately
- [ ] Recommendation: adopt / defer / reject Jev as the palette/MCP
      ranker
- [ ] If adopt: mint children (state catalog/source-gen; architecture
      Ctrl-K UI; WebMCP registerTool). Do not silently land those on
      238. Close or retarget GitHub **#102** from Results
- [ ] Keep latency/quality numbers **inside this private kitchen**.
      TypeSafe MCA §2.3(f) forbids publishing service benchmarks

## Checklist

- [ ] Snapshot current chrome (`TimeWarpPage` search field) and
      ActionSet generator (ctor params only, no descriptions)
- [ ] Draft catalog contract vs `[TrackAction]` (busy-indicator, not
      user-invocable)
- [ ] Hand-label ~30 web-spa phrases against ActionSets, including
      “just looking” negatives
- [ ] Deterministic shortlist in C#, then Jev Choice + Noul gate
      (skill-suggestion shape; Choice max 255 — do not dump every
      `IAction`)
- [ ] Write `research/findings.md` + `## Results` with recommendation
      and `### How to validate`
- [ ] Mint follow-up children only if the recommendation is adopt

## Session

- Created: 3287755 (2026-09-18)
- Cockpit: grok flow session `01a0b298-4e32-7733-9d5b-1c7c09b95f02` (2026-09-18)

## Notes

### Already known (2026-09-18)

**Surfaces today**

| Surface | Status |
|---------|--------|
| `TimeWarpPage` `FluentTextInput` + “Ctrl-K” badge | Decorative. No hotkey, overlay, or catalog |
| `IAction` (`TimeWarp.Mediator`) | Empty marker (`IRequest`) |
| ActionSet source generator | Emits `State.Foo(args)` from ctor params |
| `[TrackAction]` | Action-tracking spinner opt-in |
| `#region Purpose` | Agent SSOT in source; not harvested at runtime |
| architecture **104-019** | `/.well-known/mcp/...` with `tools=[]`, honest stub |
| software **010-014** WebMCP | Archived; static `timewarp.software` package tools; **not** this task |
| GitHub architecture **#102** | Open 2021; empty; not a kitchen |

**Jev role**

Same shape as TypeSafe
[skill suggestion](https://docs.typesafe.ai/cookbooks/skill_suggestion.md):
query → code shortlist → Choice over names with description criteria →
Noul “is this a command at all?” → code dispatches the generated
ActionSet method.

```text
query / agent intent
        │
        ▼
  code shortlist (name, route, fuzzy, permissions)
        │
        ▼
  Jev Choice + Noul gate
        │
        ▼
  code execute  ← State.Method(...) / WebMCP execute
```

Ctrl-K and WebMCP share the **same catalog**. Humans get a palette;
browser agents get `navigator.modelContext.registerTool`. Do not build
two rosters.

**Out of scope for Jev:** writing descriptions, palette chrome,
JSON Schema from ctor, walking the graph, `implement`-style
generation. Jaggedness: large unfiltered state hurts accuracy;
shortlist first.

**Related**

- taratibu **014** — C# client / OpenAPI / `TYPESAFE_API_KEY`; reuse,
  do not duplicate
- architecture **177** — Purpose over hollow XML for agents
- architecture **104-019** — later child may fill `tools[]` from the
  same catalog; this id does not ship MCP transport

### Experiment protocol

Spike only (runfile or throwaway tests). No Ganda bind, no SPA
production provider.

1. **Corpus:** ~30 labeled phrases (`dark theme`, `add a passkey`,
   `roles`, `sign out`, plus negatives that should suggest nothing).
2. **State pack:** query + shortlist of `{ name, description }` only.
   Description = Purpose one-liner or hand-written stand-in until the
   catalog exists.
3. **Questions:** Choice over shortlist names; Noul “does the user
   want to run a command?”. Optional per-candidate Nouls on the top 3.
4. **Score:** arm vs label; “would Gate / show list” when confidence
   is low; do not treat confidence as a law.
5. **Access:** `TYPESAFE_API_KEY` or OpenRouter / Vercel / Cloudflare
   gateway. If none, stop after research and name the blocker.

### Suggested later children (not this id)

- **timewarp-state:** opt-in catalog + source-gen (`name`,
  description from Purpose/attribute, `inputSchema` from ctor)
- **architecture:** Ctrl-K overlay + hotkey on `TimeWarpPage`;
  WebMCP `registerTool` from the catalog; retarget **#102**
- Fill 104-019 `tools[]` only after the catalog exists

## Results

*(fill when the research and experiment complete)*

### How to validate

*(required before done — commands a cold session can re-run)*
