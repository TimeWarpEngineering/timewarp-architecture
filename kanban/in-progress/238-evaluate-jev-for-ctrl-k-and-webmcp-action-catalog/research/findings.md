# Findings — TypeSafe Jev for Ctrl-K and WebMCP action catalog

Kitchen research for architecture **238**. Live latency, token, cost, and Jev
accuracy numbers belong in gitignored `live-results.json` only. TypeSafe MCA
§2.3(f) forbids publishing benchmarks of the service. C# shortlist scores
below are this spike’s deterministic ranker, not a TypeSafe measurement.

## Recommendation

**Defer** Jev as the Ctrl-K / WebMCP ranker.

The socket fit is real: one opt-in catalog, code shortlist of `{ name,
description }`, Jev Choice over those names, Noul “is this a command?”, then
code executes the generated ActionSet method. That is the TypeSafe
[skill suggestion](https://docs.typesafe.ai/cookbooks/skill_suggestion.md)
shape. Jev must **not** author descriptions, walk the IR, generate JSON
Schema, or replace ActionSet codegen.

Defer, not reject: the protocol is ready and the C# shortlist already
recalls every labeled command on this 20-entry stand-in catalog, but this
machine has **no** `TYPESAFE_API_KEY`, **no** `OPENROUTER_API_KEY`, and **no**
Vercel / Cloudflare AI Gateway key (same blocker as taratibu **014**). Do
not fake live Choice/Noul numbers. Do not mint catalog, Ctrl-K UI, or
WebMCP `registerTool` children on this id. Do not close GitHub **#102**.

Re-open adopt only after `--live` runs against `corpus.json` and
auto-dispatch vs show-shortlist vs none are counted separately.

A later kitchen may ship the **catalog + C# shortlist** palette without Jev
(fuzzy search is already useful). That is still not an adopt of Jev.

## Catalog decision

**New opt-in.** Do not reuse `[TrackAction]` or `[TrackEvent]`.

| Attribute | Role today | Catalog? |
|-----------|------------|----------|
| `[TrackAction]` | Busy-indicator opt-in (`ActionTrackingState` / footer spinner) | **No** |
| `[TrackEvent]` | Analytics pipeline exemplar (counter increment) | **No** |
| `#region Purpose` | Agent SSOT in source (TWA0004 / architecture **177**) | **Description SSOT** (harvest first sentence) |
| Hollow `///` XML | Noise; 177 leaves it silenced | **No** |
| Proposed `[CatalogAction]` / `[CatalogTool]` | Palette + WebMCP roster | **Yes** |

Catalog fields (one roster for humans and browser agents):

| Field | Source |
|-------|--------|
| `name` | Stable id, e.g. `Credentials.AddPasskey` |
| `description` | Purpose first sentence, or a small `CatalogDescription` attribute if Purpose is too long. **Not Jev.** |
| `inputSchema` | Action ctor parameters (ActionSet generator already reads these) |
| `execute` | Generated `State.Method(args)` |
| `visibility` | `human` / `agent` / `both`. Permissions stay in code |

Not in the catalog: Fetch* page-load, Clear* sign-out internals, inbound hub
actions, Debug, template `FiveSecondTask` / `TwoSecondTask`, `ThrowException`.
`Application.ToggleMenu` is not even an `*ActionSet`, so it has no generated
dispatcher today.

Stand-in roster: `catalog.json` (20 opt-in ActionSets). Descriptions are
Purpose one-liners (or a hand-written stand-in for Plus `ThemeState.Update`,
which has no Purpose region).

## Repo split

| Piece | Repo |
|-------|------|
| Catalog **shape** + source-gen (attribute, Purpose harvest, `inputSchema` from ctor, catalog next to `*ActionSet_Method.g.cs`) | **timewarp-state** child, only if Jev (or a catalog-only follow-up) is adopted |
| Ctrl-K overlay + hotkey on `TimeWarpPage`; `navigator.modelContext.registerTool` from the same catalog; later fill 104-019 `tools[]` | **this repo** (web-spa) |

Do not build two rosters. Humans get a palette; browser agents get
`registerTool`. 104-019 stays an honest `tools=[]` stub until the catalog
exists. This id does not ship MCP transport.

## C# client decision (reuse taratibu 014)

**Do not fork a second SDK decision.**

| Path | Verdict |
|------|---------|
| Raw `HttpClient` + OpenAPI snapshot | **Spike (this id)** — same as 014 |
| Kiota from `openapi.json` | Skip — map/discriminator pain, no enum socket |
| `TypeSafeAI` 0.2.0 (Hawxy) | **Best community option after a live experiment** |
| `TypeSafeAI.Sdk` (saibimajdi) | Not on nuget.org |
| TimeWarp-owned package | Not justified |

OpenAPI snapshot copied from 014 (`openapi.json`, info 0.2.0, paths
`POST /v1/systemone` and `GET /v1/models`). Pretty-printed SHA-256
`72452d6951dbaadd1030af76434917ef103e470bf0cd6ac035b02b111bfd4d24`.

## API access (blocker)

| Channel | Status on this implementer host (2026-09-18) |
|---------|-----------------------------------------------|
| `TYPESAFE_API_KEY` / console.typesafe.ai | **Missing.** Waitlist product; no key in process env |
| `OPENROUTER_API_KEY` | **Missing in WSL.** 014 recorded it in Windows SecretManagement as `OpenRouter_Api_Key`, not exported here |
| Vercel AI Gateway / Cloudflare AI Gateway | **Missing** |

`--live` exits **2** and names this blocker. Dry-run needs no key.

## Snapshot (chrome + generator)

**Ctrl-K chrome** (`TimeWarpPage.razor`): `FluentTextInput` Placeholder="Search"
with EndTemplate “Ctrl-K”. No `@bind-Value`, no hotkey, no overlay, no
catalog. `TimeWarpFocusedPage` has no search. Footer spinner is
`ActionTrackingState.IsActive` (tracking, not a palette).

**ActionSet generator** (`timewarp-state`
`action-set-method-generator.cs`): nested `*ActionSet` + `Action` →
`State.Method(ctorParams…, CancellationToken?)`. First explicit ctor only
(primary ctors ignored). **No descriptions, Purpose, or XML harvested.**
Generated methods `#pragma warning disable CS1591`.

**GitHub [#102](https://github.com/TimeWarpEngineering/timewarp-architecture/issues/102):**
open 2021, title “Ctrl-K Search capabilities”, empty body. Not a kitchen.
Leave open; retarget from a later architecture child if Ctrl-K UI is
minted. Do not close it from this defer.

## Experiment

Spike runfile: `research/run-rank-experiment.cs`.

1. **Corpus:** 34 labeled phrases in `corpus.json` (25 command, 9
   browse/question/navigate negatives). Includes kitchen examples `dark theme`,
   `add a passkey`, `roles`, `sign out`, `just looking`.
2. **State pack:** `{ phrase, shortlist: [{ name, description }] }` only.
   Zero-score catalog rows are dropped so Choice is not 8 random `IAction`s.
3. **Questions:** Choice over shortlist names plus `none_of_the_above`
   (Choice max 255; this spike uses 8). Noul `is_command`. Optional
   `fits::{name}` Nouls on the top 3.
4. **Score:** C# shortlist vs label on dry-run. Live would count
   auto-dispatch vs show-shortlist vs none separately. Confidence is
   log-only (threshold 0.5 → show list, not a wrong arm). Noul gate 0.30.

### Dry-run (C# shortlist only — not Jev)

| Metric | Result |
|--------|--------|
| Items | 34 (25 command, 9 negative) |
| Catalog | 20 opt-in ActionSets |
| Shortlist hit | 25/25 |
| Shortlist top-1 | 25/25 |
| Shortlist miss | 0 |
| Deterministic preview auto-dispatch | 23 |
| Deterministic preview show-shortlist | 8 |
| Deterministic preview none | 3 (`just looking`, `hello`, `github`) |
| Deterministic preview miss | 0 |

The 8 show-shortlist rows are the reason Jev is still interesting:

- Near-twin commands: `add a passkey` ranks `AddPasskey` over
  `AddExistingPasskey` by one point — show the list, do not auto-dispatch.
- Negatives that still match a name/description: `roles` →
  `Principal.SetPrincipalRoles`; `settings` → `UpdateSiteSettings`;
  `weather page` → `FetchWeatherForecasts`; `what is a passkey` /
  `how do I add a passkey` → AddPasskey family; `explain dark theme` →
  `Theme.Update`. **C# cannot tell browse/ask from execute.** That is the
  Noul gate.

Live Choice + Noul: **blocked** (exit 2). No `live-results.json`.

## Why Jev still fits (after a live run)

- Closed catalog, not open generation. Code stays in control of execute.
- Companion Nouls compose in code (command vs browse; per-candidate fits).
- Confidence as a second axis: auto-dispatch vs show-shortlist. Do not
  treat confidence as a law.
- Jaggedness: large unfiltered state hurts; shortlist first. Do not dump
  every `IAction` (Choice max 255 is a hard cap, not a target).

## Out of scope (unchanged)

Palette chrome, production `JevOracleProvider`, TimeWarp TypeSafe SDK,
JSON Schema from ctor as a Jev job, walking the graph, `implement`-style
generation, filling 104-019 `tools[]`, WebMCP transport.

## Follow-up children

**None minted on this id.** Adopt would mint:

1. **timewarp-state:** opt-in catalog + source-gen
2. **architecture:** Ctrl-K overlay + hotkey; WebMCP `registerTool`;
   retarget **#102**
3. Later: fill 104-019 `tools[]` from the same catalog
