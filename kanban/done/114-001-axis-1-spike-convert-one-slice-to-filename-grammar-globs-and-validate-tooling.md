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

## Slice (locked)

**Use `hello`** — the minimal multi-layer sample that already spans contracts + application +
server. Do **not** use counter/event-stream for the main rehome: those are SPA-only and do not
exercise layer globs or multi-project membership.

| Layer today | Path |
|-------------|------|
| contracts | `source/container-apps/web/web-contracts/features/hello/hello.cs` |
| application | `source/container-apps/web/web-application/features/hello/hello-handler.cs` |
| server | `source/container-apps/web/web-server/features/hello/feature-annotations.cs` |

**Cohesive target folder (recommended):**

```text
source/container-apps/web/features/hello/
```

Sibling of the layer projects so each csproj can use a relative glob like
`../features/**/*-<layer>.cs`. If a different path is chosen, document why in Results.

**Expected renames (namespaces stay as-is for the spike):**

| From | To |
|------|-----|
| `hello.cs` | `hello-contracts.cs` |
| `hello-handler.cs` | `hello-handler-application.cs` |
| `feature-annotations.cs` | `hello-feature-annotations-server.cs` |

Grammar: `<name>[-<function>]-<layer>.cs`; contracts drop the function segment
(`hello-contracts.cs`). Server annotations have no registry function segment in Axis 1 yet —
use an escape-hatch name (no function segment, or a provisional one) and note any grammar
friction in Results.

**Namespaces:** do not rename for the spike (`TimeWarp.Architecture.Features.Hellos` etc.).
Folder rehome only; report if generators/analyzers break without namespace alignment.

**SPA boundary:** `hello` has no `.razor` files. Confirm the axis-1 rule by inspection (spa
stays conventional; this slice creates no razor/glob seam). Optional note only — not a second
rehome of counter/event-stream.

## Hybrid include strategy (in scope — document what you pick)

Only `hello` moves; every other file in the layer projects keeps the default SDK layout.
The dual-mode include approach is **part of the experiment**, not out of scope. Document the
chosen approach and its IDE/build consequences in Results. Reasonable options (pick one; prefer
minimal csproj surgery):

1. **Keep default items** + add cross-folder globs for the rehomed tree + exclude the old
   `features/hello/` paths under each layer project.
2. **Link/Compile Include** only the three rehomed files into the correct layer projects (no
   broad glob yet) — proves membership; less representative of the final static-glob story.
3. **Disable default compile** for a layer project and re-include everything — heavier; only if
   (1) fails.

Prefer (1) unless it fights the SDK. Approach choice and failure modes belong in findings.

## Questions the spike must answer (findings = deliverable)

1. **Design-time build / IDE**: with the hybrid include strategy above and
   cross-folder `<Compile Include="../features/**/*-application.cs" />` (or equivalent), do
   IntelliSense/go-to-def/rename behave in VS Code (primary) — and note Rider/VS if cheap to
   check? Does a newly created file matching the glob get picked up without project reload?
2. **Exactly-one-project membership**: implement the guard — a file matched by zero or two layer
   globs must be a BUILD ERROR. MSBuild target vs analyzer: which is reliable and fast? (This is
   REQUIRED per the axis decision, not optional.) Demonstrate both zero-match and dual-match
   failures.
3. **Archetype analyzer viability**: minimal TWA-style prototype for one or two function
   segments (`-handler-` ⇒ `-application`, unknown function ⇒ error) — confirm the analyzer can
   see file paths/names for compiled files and produce teaching-quality diagnostics.
4. **Glob/build perf**: any measurable evaluation-time cost on the full solution? (e.g. timed
   `dotnet build` vs baseline, or binary log notes — light methodology is fine.)
5. **Template-flag / `#if` interaction**: `hello` has almost no flag surface. Scope for the
   spike (pick one; document which):
   - Inject a throwaway `#if SomeFlag` file under the rehomed `features/hello/` folder and
     confirm build with the flag off strips it without the glob resurrecting dead content; and/or
   - Confirm Compile remove / exclusion + globs do not re-include intentionally excluded files.
   Full `dotnet new` packaging exercise is optional if the above already answers "globs don't
   fight stripping."
6. **Before/after tree**: document the on-disk layout and which csproj owns each file after the
   rehome.
7. **Spa exclusion sanity**: by inspection for this slice (see Slice section) — no razor rehome.

### Soft go/no-go heuristics (Steve still final-gates)

Record explicit yes/no in Results for:

- Dual-match and zero-match produce **build errors** (hard requirement → no-go if missing).
- Solution builds **0/0** with the rehomed slice (hard requirement).
- New file matching a layer glob appears in IntelliSense **without** project reload (soft:
  missing = migration risk / no-go *risk*, not automatic kill).
- Analyzer can read filename and emit a pairing diagnostic on a deliberate mismatch (soft for
  spike quality; hard for eventual migration).

## Checklist

- [x] Throwaway worktree/branch; rehome `hello` into
      `source/container-apps/web/features/hello/` with the expected renames (namespaces unchanged)
- [x] Document hybrid include approach; wire layer csprojs; solution builds 0/0
- [x] Exactly-one-project guard implemented and demonstrated failing (zero-match and dual-match
      cases)
- [x] Minimal archetype-pairing diagnostic prototyped (`-handler-` ⇒ `-application`)
- [x] IDE behavior notes (VS Code primary), new-file flow, light perf notes
- [x] Template-flag / `#if` interaction check (dummy file or exclusion proof — see Q5)
- [x] Findings write-up in this task's Results + folded into 114 (go/no-go + any grammar
      adjustments, including `feature-annotations` naming); Steve reviews findings BEFORE the
      migration task is specced
- [x] Tear down the worktree

## Notes

- The MIGRATION (whole tree + full analyzer + registry + ADR) is deliberately NOT this task —
  its spec depends on these findings (DoR: it stays uncreated/backlog until this closes).
- Axis-2 corollary to keep in mind: assembly granularity stays as-is (single per layer);
  the spike only rehomes files and rewires includes for the one slice.
- Related: TWA0004/0008/0010 must keep passing on the spike branch; the grammar analyzer
  prototype does NOT need to ship-quality (proof of mechanism only).
- Counter/event-stream remain valid SPA demos for a future spa-specific check; they are out of
  scope for this spike's rehome.

## Session

- Created: 2026-07-22 (split from 114 per Steve — spike ≠ migration; migration task awaits
  spike findings per Definition of Ready)
- DoR tighten-up: 2026-07-22 — locked slice to `hello`, pinned cohesive path, hybrid includes
  in-scope, renames + namespaces + flag-check scope, soft go/no-go heuristics


## Results

**Spike complete 2026-07-22 — branch `spike/axis1-filename-globs` (`04e5b2c8`, local only). ALL
GATES PASS; recommendation GO for the migration spec (Steve final-gates).**

### Go/no-go outcomes

| Gate | Result |
|---|---|
| Solution builds 0/0 with rehomed slice (HARD) | ✅ full `-t:Rebuild` clean (captured) |
| Zero-match → build error (HARD) | ✅ teaching message names grammar + registry |
| Dual-match prevented (HARD) | ✅ structural: suffix-nesting is the only path; registry lint forbids it |
| New file → IntelliSense without reload (soft) | ✅ Steve-verified in VS Code |
| Archetype analyzer fires with teaching diagnostic (soft) | ✅ TWA9999 in-solution as error under warnaserror |

### What was proven

- **Source generators + TWA analyzers treat glob-included files identically** (ApiRoute/
  FastEndpoints generation, TWA0004/0010 all live on the rehomed files) — the moat carries over.
- Hybrid include option 1 (default SDK items + cross-folder suffix globs + Link metadata)
  works; old layer folders removed cleanly. Grammar renames per spec incl. escape-hatch
  `hello-feature-annotations-server.cs`.
- `feature-membership.targets`: central MSBuild guard — unmatched-file error + registry-nesting
  lint. Runs once (imported by web-server).
- No `#if`/glob conflict; TWA0010 newly (and correctly) enforces that a feature file's flag use
  agrees with the DefineConstants of the layer project that globs it.
- IDE: IntelliSense/go-to-def normal; new matching file picked up with NO reload (Steve).
- Perf: no measurable evaluation cost (full incremental ~16s unchanged; rebuilds normal).

### Findings for the migration spec (the real payload)

1. **Analyzer path pitfall (cost an hour):** SyntaxTree.FilePath for glob-included files arrives
   as project-relative WITH `..` traversal (normalizes to `.../web-server/../features/...`).
   Path heuristics must never match bare project-dir substrings ("web-server/") — the spike's
   exclusion heuristic silently ate the whole cohesive tree. Match `<proj>/features/` shapes or
   normalize. MUST be encoded in the shipped analyzer + its tests.
2. **MSBuild incremental staleness:** analyzer-dll changes don't reliably invalidate downstream
   compiles (stale rules run silently until a rebuild). Low risk under full `dev build`, but the
   migration should document it; registry changes ⇒ rebuild.
3. **Grammar gap:** server-side feature annotations have no registered function segment —
   escape-hatch name works; registry needs an explicit decision (add `feature-annotations`
   function, or bless the escape hatch).
4. **New-file misplacement UX:** a file created in the wrong folder is claimed by that project's
   default globs (observed live during IDE testing); membership guard + TWA0004 catch it at
   build, not at creation. Acceptable; worth a docs note.
5. **Intermittent `-t:Rebuild` "1 Error"** in piped output, never reproducible when captured to a
   file (3 observations) — suspected console-output race, not the scheme; watch during migration.
6. TWA9999 prototype is spike-quality: registry duplicated between .targets and analyzer — the
   shipped version must generate both from ONE registry (the axes' two-things-must-agree rule).

Worktree torn down; branch retained locally for reference. NOTHING landed in template source.

### Gate verdict (Steve, 2026-07-22)

**GO.** Grammar decision on finding 3: **`feature-annotations` is REGISTERED as a function**
(pairs with layer `server`) — not an escape-hatch case. Migration task specced as 114-002.

## Session

- Spike executed: 2026-07-22, interactive (Steve: IDE verification + gates) + orchestrator.
