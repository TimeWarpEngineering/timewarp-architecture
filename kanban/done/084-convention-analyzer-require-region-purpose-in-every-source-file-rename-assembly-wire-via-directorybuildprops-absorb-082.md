# Convention analyzer: require `#region Purpose` in every source file (rename assembly, wire via Directory.Build.props, absorb 082)

## Description

Enforcement backstop for the agent-context-regions convention (follow-up to
[[050-010-add-region-purpose-and-region-design-to-all-migrated-source-files]]), built on the
080 lesson: a convention nobody enforces decays (regions sat at 4/304 for a year).

**Key design decision (maintainer, 2026-07-02): the rule is UNIVERSAL, not threshold-based.**
Every compiled `.cs` file requires `#region Purpose` — trivial files get a one-line region
("Assembly marker enabling typed assembly references.") instead of an exemption. Rationale:

- A "non-trivial file" heuristic (member counts, line thresholds) misclassifies in both
  directions and pushes complexity into the analyzer + suppressions. A universal rule is
  **exact**: no threshold to tune, no judgment to encode, no escape hatches to manage.
- Even trivial files benefit — "why does this AssemblyMarker exist" is a real question a
  one-line Purpose answers; a dead-code reference file gets "kept as reference for X".
- Cost is one 3-line insertion per trivial file (~140 files), paid once, mechanical.
- Generated code is excluded structurally (`GeneratedCodeAnalysisFlags.None`), not by judgment.

`#region Design` is **not** analyzer-enforced: "has design decisions worth recording" cannot be
judged mechanically. Design stays governed by the AGENTS.md maintenance rule and review.

## Diagnostics

- **TWA0004** — compiled, non-generated `.cs` file lacks a `#region Purpose` block.
  Severity Warning (build-breaking under the repo's warnings-as-errors).

## Architecture / wiring (this is the other half of the task)

1. **Rename** `timewarp-architecture-contract-analyzers` → `timewarp-architecture-convention-analyzers`
   (the assembly now hosts contract rules TWA0002/0003 *and* repo-wide convention rules; the
   "contract" name stops fitting). Update: csproj + folder, `.slnx`, test project reference,
   `web-contracts.csproj` reference (superseded by step 2), AnalyzerReleases files.
2. **Wire once in `source/Directory.Build.props`** — `<ProjectReference OutputItemType="Analyzer"
   ReferenceOutputAssembly="false">` with a condition excluding the analyzer/attributes projects
   themselves (self-reference guard). Remove the now-redundant per-csproj reference in
   web-contracts. Scope = `source/` only (tests deliberately excluded — a test's why is its name).
3. **This absorbs [[082-broaden-contract-nullability-analyzer-to-api-grpc-foundation-contracts]]**:
   TWA0002/0003 reach api/grpc/foundation contracts through the same wiring (inert in projects
   without `AbstractValidator` usage). Close 082 into this task.
4. **Template effect:** generated `dotnet new timewarp-architecture` apps inherit both region and
   nullability enforcement out of the box.

## Recipe (080-proven: wire + reconcile in the same PR, tree never red)

- [x] `PurposeRegionAnalyzer` (TWA0004): syntax-tree action; region-name match; generated code
      excluded via `GeneratedCodeAnalysisFlags.None`; registered in AnalyzerReleases.
- [x] Fixie tests (5): without = flagged; with = clean; **anywhere-in-file counts** (placement is
      skill guidance); Design-only still flagged; generated clean. All 21 analyzer tests green.
- [x] Assembly renamed `timewarp-architecture-convention-analyzers`; wired once in
      `source/Directory.Build.props` (self-excluded; `IsAspireProjectResource="false"` to keep the
      Aspire AppHost from treating the analyzer ref as an app resource — ASPIRE004).
- [x] Backfill: 51 formulaic files scripted (assembly markers, global-usings, identical one-liners);
      88 small files via 8-agent fan-out (one-line Purpose, Design only where genuine — includes
      the 050-010 skips: dead-code files now say *why they're kept*). **304/304 coverage.**
- [x] TWA0002/0003 broadening caught **one real violation outside web-contracts**:
      `web-server/configuration/sample-options.cs` `SampleOption = string.Empty` (+ `NotEmpty()`
      via the options validator) → `= null!`. api/grpc/foundation contracts were clean.
- [x] `dev build` green (0/0); analyzer 21/21, sourcegen 14/14, web-server integration 11 passed.
- [x] 082 closed as absorbed; `agent-context-regions` skill updated (universal Purpose, no
      triviality exemption, TWA0004 noted) — flow commit `25e288b`, synced.

## Result — analyzer defect found & fixed via the test framework's hidden suppression pass

The testing framework prepends `#pragma warning disable TWA0004` and verifies the diagnostic
disappears. The first implementation reported at `TextSpan(0,0)` — a position *before* any leading
pragma takes effect — making the diagnostic **unsuppressable** (a real defect, not a test
artifact). Fix: anchor on the file's **first token**; trivia-only files (fully commented out /
`#if false`) have no active code and are structurally exempt.

## Notes

- Freshness remains out of scope — no tool can verify a region is *true*; that stays with the
  AGENTS.md reconcile rule and PR review. This analyzer guarantees presence only.
- Consumers who want the softer stance can downgrade via
  `dotnet_diagnostic.TWA0004.severity = suggestion` in `.editorconfig` — document, don't build,
  the escape hatch.
