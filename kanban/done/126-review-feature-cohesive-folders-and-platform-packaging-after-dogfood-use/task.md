# Review feature-cohesive folders and platform packaging after dogfood use

## Description

Retrospective review of the **folder structure** and **platform packaging** we shipped a few
commits ago and have since been dogfooding in real work (identity, golden/aggregate persistence,
ingress generators, beta.6/beta.7 package publishes).

This is not a re-litigation of the original architecture decision. Axis decisions from
[[114-architecture-direction-study-vertical-slice-vs-clean-architecture-reference-repo-survey-and-rfc]]
and ADR-0008 stand unless evidence from use shows a concrete friction or inefficiency.

**Two coupled surfaces under review:**

1. **Feature-cohesive folders + filename grammar (114 / ADR-0008)**
   - Product code under `source/container-apps/web/features/<slice>/`
   - Layer composition via `<name>[-<function>]-<layer>.cs` grammar + MSBuild globs
   - Registry SSOT: `feature-filename-grammar.json` → TWA0015/0016 + `feature-filename-grammar.g.props`
   - Membership guard, escape hatch (`<name>-<layer>.cs`), SPA exception
   - Agent skill: `skills/tw-feature-placement/`

2. **Platform packaging dual-mode (051, 092, 115, 124)**
   - `TimeWarp.Foundation.*`, `TimeWarp.Architecture.{Analyzers,Generators,Attributes}`,
     `TimeWarp.Identity` as published packages
   - Dual-mode: monorepo dogfoods `ProjectReference` when source trees exist; greenfield
     template apps use packages (`UseFoundationPackages` / `UseAnalyzerPackages` /
     `UseIdentityPackages`)
   - sourceName-safe package IDs via `msbuild/timewarp-platform-packages.props`
   - CPM pins equal release version (task 124 policy)

**Why now:** the design is no longer theoretical — we have enough implementation and publish
cycles to ask whether we are using it efficiently and what should improve.

Review artifacts live in this folder task (`findings.md`, then `rfc/`, optional `review/` /
`debate/` only if the switches below fire).

## Process (recommended)

Primary shape is a **post-dogfood efficiency retrospective with multiple independent
decisions**, not a single architecture fork and not Phase 4b “fix this PR until clean.”

| Phase | Skill | What happens |
|-------|--------|--------------|
| **0 — rails** | `tw-agent-collaboration` | Always on: folder kitchen, Notes, Session IDs, Results-before-done, same-task fold-in. Not the decision engine. |
| **1 — evidence** | analysis (no multi-agent skill) | Inventory tree + packaging seams; write `findings.md` (wins, frictions, risks, open questions). Evidence before votes. |
| **2 — decisions** | **`tw-rfc-ballot` (primary)** | Under `rfc/`: number independent keep / tweak / restructure decisions, author lean, parallel ballots, tally, maintainer resolve on dissent. |
| **3 — fold-in** | same host task **126** | Fold accepted resolutions into product truth (skill/docs/analyzers/trivial fixes) **on this id**. Defer or open **child** tasks only for real product breakdown — never a sibling “apply RFC” task. |

**Do not use as the main process:**

- **`tw-implementation-review`** — Phase 4b on a *diff* → severity findings → fix loop. Wrong default metaphor for a tree-wide efficiency review; default outcome here is findings + agreed follow-ups, not 0-open on host.
- **`tw-consensus-debate`** — one sequential fork only. Folders + packaging will spawn many independent questions; debate is too narrow as the primary skill.

**Switches (only if needed):**

| Switch to | When |
|-----------|------|
| `tw-implementation-review` (`review/`) | Evidence turns up **concrete, fix-now defects** in the machinery (broken globs, wrong TWA behavior, dual-mode MSBuild bugs) and a severity + fix loop on **that** delta is appropriate. |
| `tw-consensus-debate` (`debate/`) | Ballots collapse to **one hard architecture fork** (e.g. “drop dual-mode packages entirely?”). |

**Exit bar:** `findings.md` + `rfc/` tally (or documented “no decisions needed”), fold-in or explicit deferrals on this checklist, Results covering evidence + ballot + what landed / what was deferred.

## Requirements

- Inventory how current product code sits under the grammar (slices, escape hatches, SPA vs
  web features, any files that fight the convention).
- Inventory packaging seams: which projects dual-mode, pin policy, publish path, template
  symbols, and any residual vendor trees or dual paths that confuse agents or CI.
- Identify **efficiency friction** from dogfood use, for example:
  - Grammar / registry / rebuild-after-registry-edit cost
  - Wrong-layer placement or repeated escape-hatch use
  - Package vs ProjectReference mode surprises
  - Analyzer/generator attach surface (Analyzers vs Generators split)
  - CPM pin / release choreography pain
  - Documentation or skill drift vs actual layout
- Separate findings into numbered decisions: **keep**, **tweak (small follow-up)**,
  **restructure candidate** — then ballot them (`tw-rfc-ballot`).
- Prefer analyzer/generator/docs fixes over convention-by-memory when a gap is found.
- Do not change production structure in this task unless a fix is trivial and uncontroversial;
  default outcome is findings + RFC disposition + child tasks for approved non-trivial work.

## Checklist

### Phase 1 — evidence

- [x] Re-read ADR-0008, `skills/tw-feature-placement/SKILL.md`, and AGENTS.md packaging
      sections as the baseline contract under review
- [x] Survey live `web/features/` tree: slice list, grammar compliance, escape-hatch count,
      SPA exceptions, oddball files
- [x] Survey platform package surface: package IDs, dual-mode MSBuild props, Directory.Packages
      pins, template symbols (`foundationPackages` / `analyzerPackages` / `identityPackages`)
- [x] Walk 2–3 recent dogfood features (e.g. identity, profile, auth/admin) and note where
      agents or humans hesitated, mis-placed files, or worked around the system
- [x] Capture packaging dogfood: monorepo ProjectReference path vs `dev template-smoke` /
      package-mode path; pin-bump and publish friction from 124/beta.7
- [x] Write `findings.md` (efficiency wins, frictions, risks, open questions)

### Phase 2 — RFC ballot (`tw-rfc-ballot` + `tw-agent-collaboration`)

- [x] Create `rfc/rfc.md`: evidence matrix, numbered decisions (options + author lean), empty
      Reviewer opinions + ballot template
- [x] Run parallel ballots; optional adversarial reviewer; re-verify falsifiable claims
- [x] Tally decisions; Steve resolves dissent with recorded reasoning
- [x] Disposition table: keep / tweak / restructure + proposed follow-up task titles
      (rfc.md §7 tally + post-tally maintainer decisions)

### Phase 3 — fold-in (same task id)

- [x] Fold accepted resolutions on **126** (docs/skills/analyzers/trivial fixes) — commit
      `a3b5f1fd`: P1 pin-policy comments, P2 consumer table, D5 inert-CPM note, D2 domain
      headroom note
- [x] Open only agreed non-trivial follow-up / child tasks (do not auto-expand scope) —
      126-001..004, each maintainer-approved in conversation
- [x] Record final disposition in Results (evidence + ballot outcome + fold-in / deferrals)

### Optional switches

- [ ] ~~If machinery bugs found: open `review/` and run `tw-implementation-review` on that delta~~
      — not triggered: no fix-now machinery defects found (all frictions were doc drift or
      gate-coverage gaps)
- [ ] ~~If one hard fork remains: open `debate/` and run `tw-consensus-debate` on that single
      question~~ — not triggered: dissent (D1) was resolved directly by maintainer with a third
      option

## Notes

- Upstream decisions: 114 (axes + migration), 114-002 (cohesive folders), 114-003 (skill),
  ADR-0008; packaging lineage 051 / 092 / 115 / 124.
- Related but out of scope unless findings force a link: 104 program (agent-ready identity),
  113 golden persistence, 125 docs strategy.
- Process choice (2026-07-25): primary **`tw-rfc-ballot`**, rails **`tw-agent-collaboration`**,
  evidence first; **not** consensus-debate or implementation-review as the main path.
- Prefer short evidence + numbered decisions over a second architecture study.
- Implementation plan (2026-07-25): see [plan.md](plan.md) in this folder — operationalizes the
  three phases with verified paths, mechanical survey commands, the 104-002 `rfc/rfc.md` shape as
  precedent (2 parallel reviewers + 1 adversarial), and fold-in criteria. Hard constraints carried:
  Steve resolves ballot dissent; no production restructure on this id; registry edit ⇒ full rebuild.
  Early finding candidate already spotted: stale "never been published" TimeWarp.Identity pin
  comment in root `Directory.Packages.props` (predates task 124 beta.6 first publish).

## Results

**Evidence (Phase 1)** — [findings.md](findings.md), gathered by two parallel read-only survey
agents; every claim cites a path/count/commit. Headlines: the grammar design is holding (zero
grammar-caused renames in the tree's entire history; registry SSOT shows no drift; SPA exception
intact; 6 of 36 non-contract files use the escape hatch), and the packaging machinery is sound
(composed-property sourceName-safety verified twice; `dev template-smoke` real and CI-wired;
task-124 pin policy proven against nuget.org). Real frictions: contracts-subfoldered-vs-flat
asymmetry (undocumented/unenforced), three stale pin-policy comments, stale Generators consumer
table (3 listed vs 8 actual), template-smoke structurally blind to stale-published-pin breaks,
and the sourceName-literal bug class having repeated once (`a251980f`).

**RFC ballot (Phase 2)** — [rfc/rfc.md](rfc/rfc.md): 5 balloted decisions, 3 parallel reviewers
(2 independent + 1 adversarial), all cast blind. Adversarial reviewer re-derived every
load-bearing claim from scratch; none failed. Tally: D2–D5 unanimous with author lean; D1 split
2–1 → maintainer resolved with a **third option** (per-use-case folders, unconditional). The
adversarial pass materially improved D3 (flatcontainer-not-search-index precision) and D4 (the
existing smoke helper excludes `.cs` — naive reuse would miss the historical bug class).

**Post-tally maintainer decisions (2026-07-26, recorded under rfc.md §7):**
- D1 third option: operation files group side by side in `<use-case>/` folders; shared files at
  slice root; `commands/`/`queries/` folders removed; rule unconditional → **126-001**.
- Category-4 migration: feature code living in layer project folders (Profile aggregate —
  which corrects the F4/D2 "empty domain layer" premise — principal store, chat hub,
  WebAuthn/agent-token pieces) migrates to `features/`; migrated files adopt `…Features.<Id>`
  namespaces (namespace declares slice membership) → **126-002**.
- D3 with **block** semantics: post-publish release gate generating the published template
  against real nuget.org; failed gate = release not done → **126-003**.
- Drop **all three** source-mode template symbols (`foundationPackages`/`analyzerPackages`/
  `identityPackages`) — generated apps always package-mode; eject story rejected (clone covers
  it); D4's literal scan folds in as a now-simple unconditional check → **126-004**.

**Folded in on 126** (commit `a3b5f1fd`, doc/comment-only): three `Directory.Packages.props`
pin-policy comments corrected; `HowToUpgradeToAnalyzerPackages.md` consumer table fixed to 8 +
producer-grep caution; `timewarp-templates/Directory.Packages.props` vestigial CPM comment
corrected; `skills/tw-feature-placement/SKILL.md` domain-headroom note added (present-tense,
public-skill safe).

**Child tasks filed (all maintainer-approved):** 126-001 (use-case folder migration), 126-002
(layer-folder feature-code evacuation + slice namespaces), 126-003 (post-publish nuget.org gate,
block semantics), 126-004 (drop source-mode symbols + sourceName-literal scan).

**Explicit deferrals:** `platform/` home for shared seams and `projects/` pure-selector-csproj
restructure — deliberately parked until 126-001/002 land, since the post-migration residue in
the layer folders is the real input to both. Re-examine then.

**Process notes:** optional switches (implementation-review / consensus-debate) not triggered.
Reviewer/evidence rosters and session trail live in rfc.md §6 and the git history of this
folder (evidence `0b2e985a`, RFC draft `0235f46e`, ballots+tally `74f3ab98`, fold-in
`a3b5f1fd`, resolutions `d165108e`/`9f3c2076`/`5a7e6908`/`67158f78`/`5569313c`).

## Session

- Created: 2026-07-25 — filed after dogfood use of cohesive folders + platform packages.
- Process updated: 2026-07-25 — recorded rfc-ballot primary + collaboration rails + switches.
- Plan recorded: 2026-07-25 — plan.md added; orchestration session moving to Phase 1 evidence.
- Executed: 2026-07-25/26 — evidence (2 agents) → RFC (5 decisions) → ballots (3 reviewers) →
  maintainer resolutions in-conversation → fold-in + 4 child tasks. Orchestrated by Claude
  (Fable), workers Claude Sonnet subagents.
