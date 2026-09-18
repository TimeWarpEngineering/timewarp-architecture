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

- [x] Catalog decision recorded: new opt-in (not `[TrackAction]`),
      fields `name` / `description` / `inputSchema` / `execute` /
      visibility; description SSOT is Purpose region or a small
      attribute — not Jev, not hollow XML (see **177**)
- [x] Repo split recorded: catalog **shape** + source-gen →
      **timewarp-state** child if we adopt; Ctrl-K UI +
      `navigator.modelContext` → this repo (web-spa)
- [x] Ranking experiment run (or blocked on missing
      `TYPESAFE_API_KEY` / gateway, blocker named). Client path
      follows taratibu **014**; do not fork a second SDK decision
- [x] Experiment compares Jev Choice + “is this a command?” Noul
      against labeled phrases; confidence log-only; auto-dispatch vs
      show-shortlist counted separately
- [x] Recommendation: adopt / defer / reject Jev as the palette/MCP
      ranker
- [x] If adopt: mint children (state catalog/source-gen; architecture
      Ctrl-K UI; WebMCP registerTool). Do not silently land those on
      238. Close or retarget GitHub **#102** from Results
- [x] Keep latency/quality numbers **inside this private kitchen**.
      TypeSafe MCA §2.3(f) forbids publishing service benchmarks

## Checklist

- [x] Snapshot current chrome (`TimeWarpPage` search field) and
      ActionSet generator (ctor params only, no descriptions)
- [x] Draft catalog contract vs `[TrackAction]` (busy-indicator, not
      user-invocable)
- [x] Hand-label ~30 web-spa phrases against ActionSets, including
      “just looking” negatives
- [x] Deterministic shortlist in C#, then Jev Choice + Noul gate
      (skill-suggestion shape; Choice max 255 — do not dump every
      `IAction`)
- [x] Write `research/findings.md` + `## Results` with recommendation
      and `### How to validate`
- [x] Mint follow-up children only if the recommendation is adopt
- [x] Implementation review (effort 1, general): `review/` kitchen, round 1, disposition **clean**

## Session

- Created: 3287755 (2026-09-18)
- Cockpit: grok flow session `01a0b298-4e32-7733-9d5b-1c7c09b95f02` (2026-09-18)
- Implementer: grok task-work session `01a0b3c7-6d34-7220-9f1e-b8fe9459db1a` (2026-09-18)
- Review oracle: grok task-work session `01a0b3d4-baec-78e1-a408-66cb39eccafa` (2026-09-18)

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

### Implementer 2026-09-18

Research, catalog contract, labeled corpus, dry-run spike, and **defer**
recommendation are in `research/`. Live Choice + Noul blocked on missing
keys (see Results). C# shortlist recalls all 25 command phrases; Noul is
still required for browse/ask vs execute. No children minted. #102 left
open.

### Review 2026-09-18

Effort 1, roster `general`. Kitchen: `review/`. Round 1 raised **zero**
findings. Disposition **clean** (`review/disposition.md`). Same task id;
no sibling apply-findings task.

## Results

**Recommendation: defer** Jev as the Ctrl-K / WebMCP ranker. Socket fit is
the skill-suggestion shape (code shortlist → Choice over names → Noul
“is this a command?” → code execute). Live quality is unproven because
this host has no TypeSafe or gateway key. No palette UI. No production
provider. No TimeWarp SDK. No children minted. GitHub **#102** stays
open (empty 2021 issue; retarget from a later architecture Ctrl-K child
if adopt).

**What was implemented**
- Kitchen research under `kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/`
- Catalog contract + 20-entry stand-in roster (`catalog.json`)
- 34 labeled phrases (`corpus.json`: 25 command, 9 browse/question/navigate)
- Spike runfile `run-rank-experiment.cs` (C# shortlist, then Choice + Noul request shapes; dry-run default; `--live` requires a key)
- OpenAPI snapshot reused from taratibu **014** (`openapi.json`)
- `findings.md` with catalog vs `[TrackAction]`, repo split, client path, blocker, dry-run scores

**Catalog decision**
- New opt-in (`CatalogAction` / `CatalogTool`), **not** `[TrackAction]` (busy-indicator) or `[TrackEvent]` (analytics)
- Fields: `name` / `description` / `inputSchema` / `execute` / `visibility`
- Description SSOT: Purpose first sentence or a small attribute — not Jev, not hollow XML (177)

**Repo split**
- Catalog shape + source-gen → **timewarp-state** (child only if adopt)
- Ctrl-K UI + `navigator.modelContext.registerTool` → this repo (web-spa)
- One shared catalog; 104-019 `tools[]` stays empty until that catalog exists

**C# client (why — reuse 014)**
- **Spike:** raw `HttpClient` + snapshot. Two endpoints; keep artifacts private.
- **Later bind, if adopt:** NuGet `TypeSafeAI` 0.2.0 (Hawxy)
- **Skip:** Kiota; `TypeSafeAI.Sdk`; TimeWarp-owned package

**Experiment**
- Dry-run: 34 items, C# shortlist hit 25/25 and top-1 25/25 on commands; 3 negatives score none; 6 negatives still shortlist a near-miss (Noul’s job). Auto-dispatch vs show-shortlist vs none columns exist (`deterministic_preview_*` now; live `disposition` when a key appears).
- Live: **blocked**. Named blocker: missing `TYPESAFE_API_KEY` and `OPENROUTER_API_KEY` (and no Vercel/Cloudflare gateway key) in this WSL environment. `--live` exits 2. Do not fake numbers. `live-results.json` is gitignored (MCA §2.3(f)).
- Confidence is log-only (0.5 → show-shortlist, not a wrong arm). Noul gate 0.30. Choice max 255; this spike shortlists 8 from the opt-in catalog.

**Key decisions**
- Defer Jev; a later kitchen may ship catalog + C# shortlist without Jev — that is not an adopt of Jev.
- GitHub **#102** not closed and not retargeted (no child minted).

**Review disposition:** **clean**. Effort 1, roster `general`, 1 round, 0 open / 0 fixed / 0 wontfix. No issues raised. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

**Files changed**
- `kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/task.md`
- `kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research/*`
- `kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/review/*`

**Test outcomes**
- `dotnet run run-rank-experiment.cs` → exit 0, Items: 34, shortlist hit 25/25
- `dotnet run run-rank-experiment.cs -- --live` with keys unset → exit 2, blocker text
- No library/product test change (research kitchen only)

### How to validate

**Depends on:** .NET 10 SDK. Live path needs `TYPESAFE_API_KEY` or `OPENROUTER_API_KEY` (optional; expected missing here).

**Smoke**

```bash
cd kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research
test -f openapi.json && test -f catalog.json && test -f corpus.json && test -f findings.md && test -f run-rank-experiment.cs
python3 -c "import json; d=json.load(open('openapi.json')); print(d['openapi'], d['info']['version'], list(d['paths'])); c=json.load(open('catalog.json')); p=json.load(open('corpus.json')); print(len(c['items']), len(p['items']))"
# expect: 3.1.0 0.2.0 ['/v1/systemone', '/v1/models'] then 20 34
dotnet run run-rank-experiment.cs
# expect: exit 0, "Items: 34", "commands=25 negatives=9", "Shortlist hit=25/25"
env -u TYPESAFE_API_KEY -u OPENROUTER_API_KEY dotnet run run-rank-experiment.cs -- --live; echo exit=$?
# expect: stderr names BLOCKER TYPESAFE_API_KEY / OPENROUTER_API_KEY; exit=2
test ! -f live-results.json
# expect: no live-results.json (nothing faked)
```

**Expect**
- `research/findings.md` states **defer** and names the key blocker
- C# client path is raw `HttpClient` for the spike, `TypeSafeAI` NuGet if a later adopt, no TimeWarp SDK
- Catalog is a new opt-in, not `[TrackAction]`; one shared roster for Ctrl-K and WebMCP
- No palette UI, no `JevOracleProvider` in `source/`, no new child from this id
- GitHub #102 still open
- `TimeWarpPage` search field remains decorative
- `review/disposition.md` outcome is **clean** (effort 1, 0 open findings)

**Automated gate**

```bash
cd kanban/in-progress/238-evaluate-jev-for-ctrl-k-and-webmcp-action-catalog/research
dotnet run run-rank-experiment.cs
# expect: exit 0
```

**Not in scope:** live Jev quality, latency, or cost (MCA §2.3(f); needs a key). Palette chrome. Production provider. Filling 104-019 `tools[]`. Closing #102.
