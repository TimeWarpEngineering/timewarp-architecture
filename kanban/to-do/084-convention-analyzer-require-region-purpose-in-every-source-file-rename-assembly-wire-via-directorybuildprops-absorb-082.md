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

- **TWPA0004** — compiled, non-generated `.cs` file lacks a `#region Purpose` block.
  Severity Warning (build-breaking under the repo's warnings-as-errors).

## Architecture / wiring (this is the other half of the task)

1. **Rename** `timewarp-architecture-contract-analyzers` → `timewarp-architecture-convention-analyzers`
   (the assembly now hosts contract rules TWPA0002/0003 *and* repo-wide convention rules; the
   "contract" name stops fitting). Update: csproj + folder, `.slnx`, test project reference,
   `web-contracts.csproj` reference (superseded by step 2), AnalyzerReleases files.
2. **Wire once in `source/Directory.Build.props`** — `<ProjectReference OutputItemType="Analyzer"
   ReferenceOutputAssembly="false">` with a condition excluding the analyzer/attributes projects
   themselves (self-reference guard). Remove the now-redundant per-csproj reference in
   web-contracts. Scope = `source/` only (tests deliberately excluded — a test's why is its name).
3. **This absorbs [[082-broaden-contract-nullability-analyzer-to-api-grpc-foundation-contracts]]**:
   TWPA0002/0003 reach api/grpc/foundation contracts through the same wiring (inert in projects
   without `AbstractValidator` usage). Close 082 into this task.
4. **Template effect:** generated `dotnet new timewarp-architecture` apps inherit both region and
   nullability enforcement out of the box.

## Recipe (080-proven: wire + reconcile in the same PR, tree never red)

- [ ] Write `PurposeRegionAnalyzer` (TWPA0004): syntax-tree check for a `#region` directive whose
      text is `Purpose`; skip generated code; register in AnalyzerReleases.
- [ ] Fixie tests: file with region = clean; without = flagged; generated file = clean;
      `#region Purpose` nested/after-namespace = decide (recommend: anywhere in file counts —
      placement is skill guidance, not analyzer scope).
- [ ] Rename assembly + rewire per Architecture above; full build to enumerate violations.
- [ ] Backfill one-line `#region Purpose` into every reported file (~140 trivial: assembly
      markers, global-usings, the 8 deliberate 050-010 skips incl. dead-code reference files).
      Mechanical fan-out or script; content must still be true, not filler.
- [ ] TWPA0002/0003 fallout in api/grpc/foundation contracts: fix same-axis (type + initializer
      only), per 077/080 precedent.
- [ ] `dev build` green (0/0); all analyzer + sourcegen tests green.
- [ ] Close 082 as absorbed; update the `agent-context-regions` skill (timewarp-flow): replace
      "skip trivial files" with "Purpose is universal — one line even for markers; Design only
      where there are decisions"; sync skills.

## Notes

- Freshness remains out of scope — no tool can verify a region is *true*; that stays with the
  AGENTS.md reconcile rule and PR review. This analyzer guarantees presence only.
- Consumers who want the softer stance can downgrade via
  `dotnet_diagnostic.TWPA0004.severity = suggestion` in `.editorconfig` — document, don't build,
  the escape hatch.
