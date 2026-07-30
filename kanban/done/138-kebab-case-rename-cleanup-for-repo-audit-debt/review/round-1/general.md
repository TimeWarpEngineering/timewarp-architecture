# Round 1 — general
**Date:** 2026-07-30
**Scope reviewed:** branch Claude/2026-07-30/task-138-kebab-case-rename-cleanup vs dev

## Verification results (condensed — full detail in session transcript)

1. `ganda repo audit`: kebab-path-names → exactly 2 remaining (grpc-server/Dockerfile,
   timewarp-templates/NuGet.config). CONFIRMED.
2. Clean `dotnet build` slnx: 0/0 (~56s, ran twice). CONFIRMED.
3. Reference sweep (12+ old names, all batches): all live hits confined to kanban
   done/archived history + this task's own artifacts; SVG-internal sodipodi:docname metadata
   pre-existing/out of scope. CONFIRMED except Issue 1.
4. Case-only renames: history preserved via --follow; zero case-duplicate paths repo-wide.
   CONFIRMED (three overview.md files display as add/delete due to git similarity heuristic —
   content verified byte-correct; display artifact only).
5. Template pack: PackageIcon logo.png + None Include resolve; PackageLicenseExpression (not
   file) so license.md rename needs no csproj wiring; PackageIconUrl fixed forward from dead
   Assets/ path. CONFIRMED (static; implementer's template-smoke SUCCEEDED relied on for
   dynamic proof).
6. Pre-existing broken-link fixes: 3/3 spot-checked targets exist (incl. anchor match).
   CONFIRMED.
7. Kanban slugs: 7 R100 renames, no renumbering — but 8 dangling [[...]] inbound links across
   6 files (2 live in to-do/) still use old slugs; commit's "zero inbound links" claim
   incorrect. PARTIALLY REFUTED → Issue 1.
8. testEnvironments.json deleted; no build-file references. CONFIRMED.

Judgment: Dockerfile leave-in-place sound (DockerfileContext with no DockerfileFile override,
verified in csproj); .gitignore PDF path updates correct and necessary; Logo.png rename-pair
crossing harmless (byte-identical duplicates, same SHA-256); no meaning-changing renames.

## Summary

Solid, well-evidenced work: build 0/0, audit down to exactly the two documented tool-required
paths, Dockerfile reasoning verified against the csproj, sampled renames carry correct
reference updates, and three pre-existing broken links were fixed forward and verified. One
real defect: 8 [[...]] kanban cross-links (2 in live to-do files) still point at old
double-hyphen slugs, contradicting the task requirement and the batch-c commit claim.

## Issues

### Issue 1 — Severity: bug
- File: kanban/archived/034-…:177; kanban/done/058-…:56,148; kanban/done/075-…:84,92;
  kanban/done/078-…:54,82; kanban/done/081-…:78; kanban/to-do/061-…:30;
  kanban/to-do/070-001-…:5
- Description: [[…]] wiki-links still use pre-rename double-hyphen slugs for tasks 060, 070,
  079 after commit 0b1b9f52 renamed the targets; two source files are live (to-do), outside
  the history-remnant convention. Commit's grep-sweep claim incorrect.
- Suggestion: update all 8 occurrences to the new single-hyphen slugs.
- Status: open
