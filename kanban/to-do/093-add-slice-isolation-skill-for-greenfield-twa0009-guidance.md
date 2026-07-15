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

- [ ] Author `skills/slice-isolation/SKILL.md` (frontmatter name + rich description triggers)
- [ ] Cover why / terms / SliceRoot / tiers / placement / share / opt-out / limits / examples
- [ ] Wire skill into the same distribution path used by other TIMEWARP skills (architecture repo + Grok flow install if applicable)
- [ ] Update AGENTS.md with skill pointer
- [ ] Cross-link from `web-api-contracts` skill
- [ ] Cross-link from `blazor-layout` skill
- [ ] Smoke: skill description would auto-invoke for “add a new clients feature page” / “TWA0009”
- [ ] Commit skill + doc cross-links

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

## Session

- Created: 2026-07-15 (post-release discussion: greenfield skill gap for TWA0009)
