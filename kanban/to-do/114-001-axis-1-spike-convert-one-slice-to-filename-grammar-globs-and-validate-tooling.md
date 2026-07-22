# Axis 1 spike — convert one slice to filename-grammar globs and validate tooling

## Description

Validation spike for the axis-1 decision in
[[114-architecture-direction-study-vertical-slice-vs-clean-architecture-reference-repo-survey-and-rfc]]
(`axis-decisions.md` Axis 1, Steve, 2026-07-21): feature-cohesive folders on disk, layer
projects composed by **static filename globs** — `<name>[-<function>]-<layer>.cs` — with
contracts collapsing to `<name>-contracts.cs`. The spike proves (or breaks) the tooling story on
ONE slice before the ADR commits the whole template and before any migration task is specced.

Spike code lives on a throwaway branch/worktree; findings are the deliverable, folded back into
114 (this task's Results + 114's Notes). NOTHING lands in template source from this task.

## Questions the spike must answer (findings = deliverable)

1. **Design-time build / IDE**: with `EnableDefaultCompileItems=false` (or exclusion) and
   cross-folder `<Compile Include="../features/**/*-application.cs" />` globs, do
   IntelliSense/go-to-def/rename behave in VS Code (primary) — and note Rider/VS if cheap to
   check? Does a newly created file get picked up without project reload?
2. **Exactly-one-project membership**: implement the guard — a file matched by zero or two layer
   globs must be a BUILD ERROR. MSBuild target vs analyzer: which is reliable and fast? (This is
   REQUIRED per the axis decision, not optional.)
3. **Archetype analyzer viability**: minimal TWA-style prototype for one or two function
   segments (`-handler-` ⇒ `-application`, unknown function ⇒ error) — confirm the analyzer can
   see file paths/names for compiled files and produce teaching-quality diagnostics.
4. **Glob/build perf**: any measurable evaluation-time cost on the full solution?
5. **dotnet-new engine**: do template flags (`#if` regions + sources.modifiers) interact sanely
   with glob-composed projects (generate with a flag off; confirm files strip and globs don't
   resurrect them)?
6. **Slice choice**: pick a real, small slice (candidate: the counter or event-stream demo
   feature) spanning contracts + application + server files; document the before/after tree.
7. **Spa exclusion sanity**: confirm the boundary — .razor/spa files stay conventional (axis-1
   decision) — creates no seam problems for the chosen slice.

## Checklist

- [ ] Throwaway worktree/branch; convert the chosen slice's files to the grammar in a
      feature-cohesive folder
- [ ] Layer csprojs gain the static globs; solution builds 0/0
- [ ] Exactly-one-project guard implemented and demonstrated failing (zero-match and dual-match
      cases)
- [ ] Minimal archetype-pairing diagnostic prototyped
- [ ] IDE behavior notes (VS Code primary), new-file flow, perf notes
- [ ] Template-flag interaction check
- [ ] Findings write-up in this task's Results + folded into 114 (go/no-go + any grammar
      adjustments); Steve reviews findings BEFORE the migration task is specced
- [ ] Tear down the worktree

## Notes

- The MIGRATION (whole tree + full analyzer + registry + ADR) is deliberately NOT this task —
  its spec depends on these findings (DoR: it stays uncreated/backlog until this closes).
- Axis-2 corollary to keep in mind: assembly granularity stays as-is (single per layer);
  the spike only rehomes files and rewires includes for the one slice.
- Related: TWA0004/0008/0010 must keep passing on the spike branch; the grammar analyzer
  prototype does NOT need to ship-quality (proof of mechanism only).

## Session

- Created: 2026-07-22 (split from 114 per Steve — spike ≠ migration; migration task awaits
  spike findings per Definition of Ready)
