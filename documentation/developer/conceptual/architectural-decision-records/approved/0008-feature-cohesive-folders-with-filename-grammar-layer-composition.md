# Feature-cohesive folders with filename-grammar layer composition

* Status: accepted
* Architect: Steven T. Cramer
* Consulted: reference-repo structural survey (ardalis modulith/RiverBooks/VerticalCleanModularMicroservices, FullStackHero dotnet-starter-kit, Jason Taylor CleanArchitecture, CASA, Trinsic); validated by tooling spike before adoption
* Date: 2026-07-22

Technical Story: kanban 114 (architecture direction study; axis decisions), 114-001 (validation spike), 114-002 (migration)

## Context and Problem Statement

A feature's code wants to live together (cohesion: one folder tells the whole story; agents and
grep navigate by feature), while layers and modules want compiler-enforced decoupling
(dependency direction, package hygiene, slice isolation). Conventional .NET layouts force a
choice because they assume folder = project: organize by layer (a feature smears across
projects) or by module (layer discipline lives inside one project and needs bolt-on
enforcement). Which structure should the template's product code use?

## Decision Drivers

* Feature cohesion on disk — the primary navigation axis for humans and agents
* Compiler-enforced layer/module discipline — conventions must not rest on memory or review
* Cheap slice extraction — promoting a module toward its own assembly/service must not require
  moving files
* Preserve the template's differentiator: build-time enforcement (TWA analyzers, generators)
* Day-one simplicity for template consumers (few projects, zero ceremony to add a feature)

## Considered Options

* **Project-per-module (+ `.Contracts` pair)** — the modular-monolith consensus (ardalis
  RiverBooks, FullStackHero): physical boundaries, but M×N project multiplication, ceremony per
  feature, and intra-module layering still needs extra enforcement (NsDepCop / arch tests)
* **Layer projects with feature folders inside each** (status quo; Jason Taylor): few projects
  and real layer direction via project references, but features smear across projects and
  cohesion is lost
* **Decouple disk layout from project membership** — feature-cohesive folders; layer projects
  include files by filename-grammar globs

## Decision Outcome

Chosen option: **decouple disk layout from project membership**, because folder = project is
only an SDK default glob, not a law — dropping that assumption delivers both goals at once.

* All of a web product slice's files live in `source/container-apps/web/features/<slice>/`,
  named `<name>[-<function>]-<layer>.cs` (contracts collapse the function segment:
  `<name>-contracts.cs`; escape hatch `<name>-<layer>.cs` for non-archetype files).
* Layer projects (contracts/application/domain/infrastructure/server) remain the unit of
  compilation and include feature files via static layer-suffix globs. Layer direction and
  package hygiene therefore stay ordinary project-reference/CPM facts — free, at compile time.
* Assembly granularity stays **single project per layer** by default; a module that earns
  isolation or extraction gets a per-module assembly by a glob split — files never move.
* The grammar is machine-enforced from a single registry
  (`feature-filename-grammar.json` → generated MSBuild props + analyzer constants):
  a membership guard fails the build for files matching no registered layer suffix (and rejects
  suffix nesting, making dual-membership impossible), TWA0015 fails function↔layer mismatches,
  TWA0016 fails near-miss function tokens. Diagnostics teach the grammar.
* The Blazor WASM spa stays conventional (`web-spa/features/**`): the Razor SDK's asset/codegen
  pipeline does not warrant cross-folder globbing.

### Positive Consequences

* One folder per feature — full-story navigation for agents and humans; project view degrades
  gracefully to linked files
* Layer discipline without NsDepCop or architecture tests; `internal` remains layer-wide until a
  module earns its own assembly
* Source generators and analyzers operate on glob-included files identically (validated by
  spike); the enforcement moat is preserved and extended
* Slice promotion/extraction is a csproj edit, not a file migration

### Negative Consequences

* Filenames carry the grammar (longer names; renames are two-segment decisions)
* Registry edits require a full rebuild (analyzer DLLs can go stale under incremental builds)
* Path-based tooling must respect the grammar's normalization pitfalls (analyzer file paths can
  arrive with parent-directory traversal); encoded in the shipped analyzer and its tests
* New files landing in a wrong folder surface at build (membership guard), not at creation

## Links

* Decision record with all seven axis outcomes: kanban 114 `axis-decisions.md`
* Agent workflow: `skills/tw-feature-placement/SKILL.md`; summary in `AGENTS.md` (Layout)
* Refined by the enforcement rules table: TWA0015/TWA0016 (AGENTS.md); registry SSOT
  `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
* Related: [ADR-0003](0003-endpoint-centric-api-with-interface-based-validation.md) (contracts
  as the seam), [ADR-0007](0007-http-endpoints-are-generated-fastendpoints-from-contracts-on-both-servers.md)
  (generate-don't-handwrite)
