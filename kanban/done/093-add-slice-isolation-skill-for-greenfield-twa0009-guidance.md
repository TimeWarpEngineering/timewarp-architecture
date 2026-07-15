# Add slice-isolation skill for greenfield TWA0009 guidance

## Description

Add a new **TIMEWARP skill** named **`slice-isolation`** so coding agents (and greenfield apps
generated from `dotnet new timewarp-architecture`) know **how and why** product slices work —
not only that **TWA0009** exists as a one-line AGENTS.md table row.

Today enforcement is complete (namespace-based `SliceIsolationAnalyzer`,
`[CrossSliceReference(typeof(T), reason)]`, platform `Applications` one-way). Docs cover
deletion (`HowToRemoveDemoFeatures`) and package wiring (`TimeWarpSliceRoot`). There is **no**
agent skill that teaches placement, sharing, opt-out, or greenfield scaffolding. Agents mostly
learn by failing TWA0009.

This skill closes that gap without inventing more packages or analyzers.

## Requirements

### Skill identity

| Field | Value |
|-------|--------|
| **Name** | `slice-isolation` |
| **Kind** | TIMEWARP skill (agent-facing procedure + placement rules) |
| **Location** | Same home as sibling architecture skills (`skills/slice-isolation/SKILL.md` in this repo; mirror/install path for Grok flow skills if that is how `web-api-contracts` / `blazor-layout` ship) |
| **Triggers** | new feature/slice/page, TWA0009, cross-slice, CrossSliceReference, SliceRoot, `features/`, greenfield product area, “where does this page/state live?” |

### Skill body must teach

1. **Why** — slices are independently removable vertical units; folders organize humans; **namespace under SliceRoot is the law**.
2. **Terms** — slice vs informal “feature” vs `IModule` (module ≠ product slice).
3. **SliceRoot** — default `{RootNamespace}.Features`; optional MSBuild `TimeWarpSliceRoot` / `CompilerVisibleProperty`.
4. **Tiers** — Outside (composition free) · Substrate (bare `…Features`) · Platform (`Applications`, product → platform free; reverse not) · Product (symmetric isolation).
5. **Nested slice ids** — full path under root (e.g. `Admin.Roles`).
6. **Placement matrix** — page/state/actions **in** the slice namespace; shell/layout **outside**; shared UI → `Components`; shared API shapes → **contracts** (other assembly free).
7. **Share vs opt-out** — prefer Components/contracts; deliberate edge:
   `[CrossSliceReference(typeof(ForeignType), "reason")]` (edge-scoped, `AllowMultiple`, non-empty reason).
8. **Limits** — same-assembly only; razor/generated trees not scanned (`GeneratedCodeAnalysisFlags.None`).
9. **Good/bad examples** — short code/namespace examples (illegal Counter→Weather; legal Components; StyleGuide opt-out pattern).
10. **Pointers** — AGENTS.md TWA table; `HowToRemoveDemoFeatures.md`; analyzer Design region in
    `slice-isolation-analyzer.cs`; attribute in foundation-contracts.

### Cross-links (small edits, same task)

- **AGENTS.md** — keep TWA0009 row; add pointer to skill `slice-isolation` (2–3 lines max).
- **web-api-contracts skill** — Related skills + one sentence: contracts assemblies are free under TWA0009; still align plural `…Features.*` with product slices.
- **blazor-layout skill** — one sentence: chrome/shell is platform/outside SliceRoot; product pages live in slice namespaces.

### Non-goals

- New analyzers or diagnostic IDs.
- Renaming `Features` → `Slices` in namespaces.
- Merging this content into `web-api-contracts` as the primary home.
- Full rewrite of conceptual `DirectoryStructure.md` (optional follow-up / P2 docs debt).
- Separate skills for opt-out-only / platform-only / nested Admin (keep one skill).

## Checklist

- [x] Author `skills/slice-isolation/SKILL.md` (frontmatter name + rich description triggers)
- [x] Cover why / terms / SliceRoot / tiers / placement / share / opt-out / limits / examples
- [x] Wire skill into the same distribution path used by other TIMEWARP skills (architecture repo + Grok flow install if applicable)
- [x] Update AGENTS.md with skill pointer
- [x] Cross-link from `web-api-contracts` skill
- [x] Cross-link from `blazor-layout` skill
- [x] Smoke: skill description would auto-invoke for “add a new clients feature page” / “TWA0009”
- [x] Commit skill + doc cross-links

## Notes

### Related work

| Task / artifact | Relation |
|-----------------|----------|
| **091** (done) | Namespace-based TWA0009 Option A — source of truth for rules |
| **088** (done) | Superseded folder-based feature isolation |
| **092** (done) | Packages ship the analyzer; greenfield gets TWA0009 via `TimeWarp.Architecture.Analyzers` |
| Analyzer | `source/analyzers/…/slice-isolation-analyzer.cs` |
| Attribute | `source/foundation/foundation-contracts/base/cross-slice-reference-attribute.cs` |
| Human how-to | `documentation/developer/how-to-guides/HowToRemoveDemoFeatures.md` |

### Design notes from skill gap review

- One skill, not a family — cognitive load over micro-skills.
- Prefer **proactive scaffolding** guidance; analyzer remains the **reactive** safety net.
- Optional later: `HowToAddAProductSlice.md` for humans; not required if skill is solid.



### Implementation plan (2026-07-15)

# 093 — slice-isolation skill

## Locked decisions

- Skill name: `slice-isolation` (single skill, no family, no references/ folder)
- Canonical path: `skills/slice-isolation/SKILL.md` in this repo
- Distribution: ganda skills add/sync to harnesses (no hand-copy into timewarp-flow)
- Mirror analyzer rules only (Applications platform, full nested ids, edge-scoped opt-out)
- Cross-links: AGENTS.md paragraph after TWA table; web-api-contracts Related skills; blazor-layout one sentence
- Skip HowToRemoveDemoFeatures edit and DirectoryStructure rewrite

## Files

| Path | Action |
|------|--------|
| skills/slice-isolation/SKILL.md | Create |
| AGENTS.md | Pointer after enforcement table |
| skills/web-api-contracts/SKILL.md | Related skills line |
| skills/blazor-layout/SKILL.md | Slice boundary sentence |

## Skill body sections

1. Why 2. Detection 3. Terms 4. SliceRoot 5. Tiers 6. Placement matrix 7. Greenfield scaffold workflow 8. Share vs opt-out 9. Limits 10. Good/bad examples 11. Checklist 12. Related skills/pointers

## Frontmatter

name: slice-isolation; description with TWA0009 / CrossSliceReference / feature page placement triggers; when-to-use keywords

## Order

1. Write SKILL.md from analyzer Design + living examples
2. AGENTS + sibling skill cross-links
3. ganda skills add + sync if available
4. Commit; mark done with Results

## Done criteria

Skill present; all 10 teaching points; markdown-only; AGENTS/sibling greps; triggers smoke-readable; 093 done with Results


## Results

### Summary

Added TIMEWARP skill **`slice-isolation`** teaching proactive product-slice placement for
TWA0009. Cross-linked from AGENTS.md and sibling skills. Registered with `ganda skills add`
against this worktree and `ganda skills sync` (wrote grok/claude/opencode harness copies).

### What was implemented

- New `skills/slice-isolation/SKILL.md` (why, detection, terms, SliceRoot, tiers, placement,
  greenfield workflow, share vs opt-out, limits, good/bad examples, checklist, pointers)
- AGENTS.md paragraph after enforcement table pointing at the skill
- `web-api-contracts` Related skills entry (contracts free under TWA0009; align namespaces)
- `blazor-layout` slice-boundary sentence (shell outside SliceRoot)

### Files changed

| Path | Change |
|------|--------|
| `skills/slice-isolation/SKILL.md` | created |
| `AGENTS.md` | skill pointer |
| `skills/web-api-contracts/SKILL.md` | Related skills |
| `skills/blazor-layout/SKILL.md` | slice boundary note |

### Key decisions

- Single skill, no `references/` family
- Mirror analyzer (platform id `Applications`, full nested slice ids, edge-scoped opt-out)
- Ganda source URI: `worktree://…/timewarp-architecture/dev/skills/slice-isolation` (dev worktree until master re-point after merge)
- Did not edit HowToRemoveDemoFeatures or DirectoryStructure (non-goals)

### Verification

- All 10 teaching points present (spot-checked key phrases)
- Markdown-only commit (`ad57f480`)
- Triggers include “clients feature page” language and TWA0009
- No C# / analyzer changes; no `dev build` required

### Review

Self-review of skill content against analyzer Design region and StyleGuide living example —
no issues.


## Session

- Created: 2026-07-15 (post-release discussion: greenfield skill gap for TWA0009)
- Implementation + review: 2026-07-15 (orchestrate-task 093)
