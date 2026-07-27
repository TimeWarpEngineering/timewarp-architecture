# Generate AssemblyMarker and InternalsVisibleTo via MSBuild (kill checked-in boilerplate)

## Description

From the 126 platform/projects folder review (maintainer + orchestrator conversation,
2026-07-27): `assembly-marker.cs` is pure boilerplate whose only variable is
`$(RootNamespace)` — **26 checked-in copies** exist across source/ (all layer projects, api,
grpc, yarp, foundation, analyzers, identity, web-spa). Generate it per-project via MSBuild and
delete every checked-in copy. Same pass converts the hand-written assembly-attribute file
(`web-contracts/internals-visible-to-client-and-server.cs`) to SDK `<InternalsVisibleTo>` items.

**Why MSBuild, not a Roslyn generator:** markers are needed in EVERY assembly, and the repo's
attach-surface policy keeps the Generators package deliberately narrow (analyzers repo-wide,
generators only where they should run). An MSBuild target in root `Directory.Build.targets`
covers every project — including generated apps, since the root build files ship as template
content — without widening the Generators surface.

**Mechanism notes (implementer verifies):**

- `WriteCodeFragment` emits attributes only — it cannot emit an interface. Use a small target
  (`WriteLinesToFile` into `$(IntermediateOutputPath)`, add to `@(Compile)`, proper
  `Inputs/Outputs` for incrementality) emitting:
  `namespace $(RootNamespace); public interface IAssemblyMarker;` with an auto-generated
  header. Confirm TWA0004 (Purpose region) skips generated code — if not, emit a one-line
  Purpose region in the generated file.
- SDK-native `<InternalsVisibleTo Include="…" />` items replace the hand-written attributes
  file (AssemblyInfo generation emits them).
- **Normalization included:** `web-spa` uses `class AssemblyMarker` (`Web.Spa.AssemblyMarker`)
  while everything else uses `interface IAssemblyMarker` — the generated form is
  `IAssemblyMarker` everywhere; update consumers (`web-spa/program.cs:92` and any other
  `typeof(…AssemblyMarker)` sites; external packages like `TimeWarp.State.AssemblyMarker` are
  untouched).
- Consumers reference markers by explicit namespace (`typeof(TimeWarp.Architecture.Web.
  Server.IAssemblyMarker)` etc. in program.cs files) — verify every project's
  `$(RootNamespace)` matches the namespace its checked-in marker declares today BEFORE
  deleting (any mismatch = the generated marker lands in a different namespace and consumers
  break; fix RootNamespace or consumer accordingly, deliberately).
- Kills a convention-by-memory: "every assembly declares one" (AGENTS.md) currently has no TWA
  enforcement — after this, new projects get the marker for free; update AGENTS.md wording to
  "every assembly gets a generated IAssemblyMarker".

## Checklist

- [ ] Inventory: confirm the 26 marker files + any `typeof(*AssemblyMarker)` consumer sites;
      verify RootNamespace ↔ declared-namespace agreement per project (fix mismatches first)
- [ ] Add the generation target to root `Directory.Build.targets` (create if absent; confirm
      it ships as template content so generated apps inherit it); incremental-build-safe
- [ ] Convert `internals-visible-to-client-and-server.cs` to `<InternalsVisibleTo>` items on
      web-contracts.csproj; delete the file
- [ ] Delete all 26 `assembly-marker.cs` files; normalize web-spa consumers to `IAssemblyMarker`
- [ ] Verify TWA0004 behavior on the generated file (skip or embedded Purpose region)
- [ ] Update AGENTS.md AssemblyMarker line + any skill/doc that instructs creating the file
      manually (present tense, no history in public skills)
- [ ] Gates: `dev build` 0/0 (full), `dev test`, `dev template-smoke` both matrices via
      current-code path (`dotnet run tools/dev-cli/dev.cs -- template-smoke` or freshly
      self-installed dev — stale `./bin/dev` footgun); generated-app spot check: markers
      resolve, no checked-in marker files in output

## Notes

- Parent: 126. Origin: platform/projects folder review conversation (2026-07-27), question
  raised by Steve ("assembly-marker.cs seems like a possible candidate for source generation").
- Convergence effect: after this task, `web-domain/` contains only `global-usings.cs` + csproj —
  the "csproj as pure artifact definition" shape falls out of the existing layer folders
  without a separate `projects/` tree. Optional follow-up (NOT in scope): convert
  `global-usings.cs` to `<Using>` items to complete that shape.
- Related: 126-008 (platform clusters) drains more files from the same folders; order
  independent, but doing 126-008 first avoids touching moved files' paths here (markers don't
  move in 126-008, so overlap is nil — either order fine).

## Session

- Created: 2026-07-27 — filed from maintainer-approved proposal (task a of two).
