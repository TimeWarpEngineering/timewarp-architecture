# Architecture direction study — vertical slice vs clean architecture, reference repo survey, RFC

## Description

Macro-architecture decision for the template, raised 2026-07-21 alongside (and upstream of) the
golden persistence work
[[113-golden-persistence-implementation-postgres-first-aspire-wired-with-actor-model-evaluation]]:
settle the template's architectural identity — Vertical Slice, Clean Architecture, modular
monolith, or a deliberate hybrid — because it affects at minimum folder and project structure,
and the persistence shape (113's RFC) follows from it.

Timing context: .NET 10 is current, .NET 11 is ~5 months out (Nov 2026) — decisions land in the
10→11 window, so avoid structures that fight where the platform is heading.

**Where the template stands today** (the incumbent to be judged against, not a blank slate):

- Slice-flavored already: product features live under `features/<slice>` with TWA0009
  compiler-enforced slice isolation; platform `Applications` one-way free.
- Endpoint-centric contracts (source-generated FastEndpoints from `[ApiRoute]`/`[ApiEndpoint]`
  contracts) — the "endpoint is the unit" idea from FastEndpoints/REPR, not controller-layer
  clean architecture.
- Layered foundation packages (`TimeWarp.Foundation.*`: contracts/application/domain/server) —
  a clean-architecture-ish horizontal axis UNDER the vertical slices.
- Golden aggregate pattern (106) + mediator pipeline (TimeWarp.Mediator) + Fixie/Shouldly.

So the real question is not "adopt VSA or CA from scratch" but: **which hybrid is the golden
one, what do the reference implementations do better, and what structural debt should be paid
now** (folder/project shape, module boundaries, contracts placement, per-slice vs per-layer
projects).

## Reference repos under survey (agents dispatched 2026-07-21)

| Source | Path | Why it matters |
|--------|------|----------------|
| Peter Morris — CASA | `/home/steve/reference-code/CASA` (private) | Respected Blazor authority's example app |
| Cramer — Trinsic app | `worktrees/.../TimeWarpEngineering/trinsic/master` | Own earlier thinking; shows what evolved into this template |
| FullStackHero starter kit | `worktrees/.../fullstackhero/dotnet-starter-kit/main` | Popular production-grade template; direct competitor shape |
| ardalis — modulith | `worktrees/.../ardalis/modulith/main` | Modular monolith template |
| ardalis — RiverBooks | `worktrees/.../ardalis/RiverBooks/main` | Modular monolith course sample |
| ardalis — VerticalCleanModularMicroservices | `worktrees/.../ardalis/VerticalCleanModularMicroservices/main` | His synthesis — likely his current recommendation |
| Jason Taylor — CleanArchitecture | `worktrees/.../jasontaylordev/CleanArchitecture/main` | THE canonical CA dotnet-new template |

Survey artifacts land in this folder (`survey-*.md`), synthesized into `survey-synthesis.md`.

## Checklist

- [x] Per-repo structural surveys DONE 2026-07-21 (5 parallel agents) — `survey-trinsic.md`,
      `survey-casa.md`, `survey-jasontaylor.md`, `survey-ardalis.md`, `survey-fullstackhero.md`
      in this folder.
- [x] Synthesis DONE 2026-07-21 — `survey-synthesis.md`: comparison matrix, seven convergence
      findings (slices won everywhere; MediatR abandoned industry-wide; contracts-project seam
      is standard; DbContext+schema per module is the modular persistence consensus; NOBODY else
      has compile-time enforcement — the incumbent's moat), steal-list by source, confirmed
      incumbent positions.
- [ ] RFC ballot decisions enumerated in `survey-synthesis.md` §"Refined RFC decision axes"
      (7 axes: slice project granularity, contracts placement, async cross-slice channel,
      intra-slice layering enforcement, persistence shape [joint w/113], template flag
      mechanics, .NET 11 posture) — write the ballot RFC in `rfc/` from these.
- [ ] Run tw-rfc-ballot; fold resolutions into this task (no separate apply-task)
- [ ] Sequence the fold-out: which resolutions gate 113's persistence RFC, which spawn
      structural-migration child tasks
- [ ] ADR(s) recording the architectural identity decision

## Notes

- 113's persistence RFC should WAIT on (or run jointly with) this RFC — actor-model and
  event-sourcing questions read differently under a modulith vs layered shape.
- The incumbent's differentiator to protect: conventions enforced by Roslyn (TWA-series) and
  source generators, not by discipline. Any adopted structure must remain analyzer-enforceable.
- .NET 11 is 5 months out; prefer structures aligned with minimal-API/source-gen direction over
  reflection-heavy patterns.

## Session

- Created: 2026-07-21 — surveys dispatched same session.
