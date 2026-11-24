# Round 1 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:10
- Description: The skill says the three CLIs are already local tools in `.config/dotnet-tools.json`. That file is not in the template pack allow-list. `skills/**` is packed, and `AGENTS.md` is not, so a generated app's only copy of this guidance is a skill whose restore step points at a manifest it does not have. `AssertSkillsShipped` still requires the previous ten skills and would stay green if this skill were dropped. `## Results` does not record `dev template-smoke`.
- Suggestion: Tell the skill the pins are in this repository's manifest (10.0.750501, `rollForward: true`) and that a generated app must add those three entries before `dotnet tool restore` will see them. Add `tw-dotnet-diagnostics/SKILL.md` to the smoke required list and fix the "ten" wording. Record template-smoke once that list matches the pack. Leave the rest of `.config/` unpacked unless a later task decides otherwise.
- Source: general
- Disposition notes: Fixed in the fix pass. Skill shows the three manifest entries for a generated app; smoke list requires `tw-dotnet-diagnostics/SKILL.md` and prints its length; `dev template-smoke` recorded in task Results.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:43
- Description: `dotnet.process.cpu.time`, `dotnet.gc.heap.total_allocated`, and `dotnet.gc.pause.time` are cumulative counters (seconds, bytes, seconds). The skill calls the first "seconds per second" and the second "bytes per second", and says to read the last value. The trial's own MB/s and core figures came from a delta parse the skill no longer describes. The published `dotnet-counters` monitor example labels `dotnet.gc.heap.total_allocated` as `(By)`, not a per-second rate.
- Suggestion: Document the delta-over-interval parse, including CPU share and pause fraction. Name the JSON field the pinned 10.0.750501 exporter writes.
- Source: general
- Disposition notes: Fixed. Checked against the trial's 10.0.750501 JSON: the exporter writes per-interval deltas (`counterType: Rate`, name suffix `/ 1 sec`), CPU split by `cpu.mode`. Skill documents the fields, an averaging parse, CPU share, and pause fraction.

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:37
- Description: Example commands write trace, counters, speedscope, and gcdump files into the working tree. The Rules section says to use a temp directory. Those extensions are not gitignored. The trial's CPU trace was 11 MB.
- Suggestion: Use `/tmp` paths in every example, matching `### How to validate`.
- Source: general
- Disposition notes: Fixed. Every example writes under `/tmp`.

## Duplicates / conflicts

- None. One reviewer.
