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

- [ ] Re-read ADR-0008, `skills/tw-feature-placement/SKILL.md`, and AGENTS.md packaging
      sections as the baseline contract under review
- [ ] Survey live `web/features/` tree: slice list, grammar compliance, escape-hatch count,
      SPA exceptions, oddball files
- [ ] Survey platform package surface: package IDs, dual-mode MSBuild props, Directory.Packages
      pins, template symbols (`foundationPackages` / `analyzerPackages` / `identityPackages`)
- [ ] Walk 2–3 recent dogfood features (e.g. identity, profile, auth/admin) and note where
      agents or humans hesitated, mis-placed files, or worked around the system
- [ ] Capture packaging dogfood: monorepo ProjectReference path vs `dev template-smoke` /
      package-mode path; pin-bump and publish friction from 124/beta.7
- [ ] Write `findings.md` (efficiency wins, frictions, risks, open questions)

### Phase 2 — RFC ballot (`tw-rfc-ballot` + `tw-agent-collaboration`)

- [ ] Create `rfc/rfc.md`: evidence matrix, numbered decisions (options + author lean), empty
      Reviewer opinions + ballot template
- [ ] Run parallel ballots; optional adversarial reviewer; re-verify falsifiable claims
- [ ] Tally decisions; Steve resolves dissent with recorded reasoning
- [ ] Disposition table: keep / tweak / restructure + proposed follow-up task titles

### Phase 3 — fold-in (same task id)

- [ ] Fold accepted resolutions on **126** (docs/skills/analyzers/trivial fixes)
- [ ] Open only agreed non-trivial follow-up / child tasks (do not auto-expand scope)
- [ ] Record final disposition in Results (evidence + ballot outcome + fold-in / deferrals)

### Optional switches

- [ ] If machinery bugs found: open `review/` and run `tw-implementation-review` on that delta
- [ ] If one hard fork remains: open `debate/` and run `tw-consensus-debate` on that single question

## Notes

- Upstream decisions: 114 (axes + migration), 114-002 (cohesive folders), 114-003 (skill),
  ADR-0008; packaging lineage 051 / 092 / 115 / 124.
- Related but out of scope unless findings force a link: 104 program (agent-ready identity),
  113 golden persistence, 125 docs strategy.
- Process choice (2026-07-25): primary **`tw-rfc-ballot`**, rails **`tw-agent-collaboration`**,
  evidence first; **not** consensus-debate or implementation-review as the main path.
- Prefer short evidence + numbered decisions over a second architecture study.

## Session

- Created: 2026-07-25 — filed after dogfood use of cohesive folders + platform packages.
- Process updated: 2026-07-25 — recorded rfc-ballot primary + collaboration rails + switches.
