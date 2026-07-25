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

Review artifacts (notes, findings, recommended follow-ups) live in this folder task.

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
- Separate findings into: **keep**, **tweak (small follow-up)**, **restructure candidate
  (needs own task/RFC)**.
- Prefer analyzer/generator/docs fixes over convention-by-memory when a gap is found.
- Do not change production structure in this task unless a fix is trivial and uncontroversial;
  default outcome is a findings report + child tasks for approved changes.

## Checklist

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
- [ ] Write `findings.md` in this folder (efficiency wins, frictions, risks, open questions)
- [ ] Write recommended disposition table: keep / tweak / restructure + proposed follow-up
      task titles
- [ ] Review with Steve; open only the agreed follow-up tasks (do not auto-expand scope)
- [ ] Record final disposition in Results

## Notes

- Upstream decisions: 114 (axes + migration), 114-002 (cohesive folders), 114-003 (skill),
  ADR-0008; packaging lineage 051 / 092 / 115 / 124.
- Related but out of scope unless findings force a link: 104 program (agent-ready identity),
  113 golden persistence, 125 docs strategy.
- Review style: implementation-review / retrospective — evidence from the tree and recent
  commits beats theory. Prefer short findings over a second architecture study.

## Session

- Created: 2026-07-25 — filed after dogfood use of cohesive folders + platform packages.
