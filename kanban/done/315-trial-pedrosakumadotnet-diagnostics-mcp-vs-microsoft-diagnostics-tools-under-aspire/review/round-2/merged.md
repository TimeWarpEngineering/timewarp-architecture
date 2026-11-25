# Round 2 — merged findings
**Date:** 2026-11-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:10
- Description: Round 1: the skill told every app the CLIs were already in `.config/dotnet-tools.json`, which the template does not pack, and `AssertSkillsShipped` did not require the new skill.
- Suggestion: (round 1) Show the three manifest entries for a generated app and require the skill in the smoke list.
- Source: general
- Disposition notes: Re-verified on `157d2d5aa`. The skill says the manifest is not packed, shows the three `10.0.750501` entries with `rollForward: true`, and says to create the manifest if it is missing. `requiredSkillFiles` includes `tw-dotnet-diagnostics/SKILL.md` and matches the eleven `skills/*/` directories. The success line prints `requiredSkillFiles.Length`. `## Results` records `dev template-smoke` ("11 SKILL.md files").

### M2 — Severity: bug — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:52
- Description: Round 1: the skill called cumulative counters rates and said to read the last value.
- Suggestion: (round 1) Document the interval delta, CPU share, and pause fraction, and name the JSON fields the pinned exporter writes.
- Source: general
- Disposition notes: Re-verified. The skill describes `counterType` `Rate`, the `/ 1 sec` name suffix, the `cpu.mode` split, and an averaging parse. Targeted check, not a gate: Python over `/tmp/t315/ms/counters.json` (the capture `## Results` cites). Field names match, and the script's averages are the recorded 1.34 + 0.65 cores, about 85 MB/s, and pause 0.0116.

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-dotnet-diagnostics/SKILL.md:47
- Description: Round 1: example commands wrote traces and dumps into the working tree.
- Suggestion: (round 1) Use `/tmp` in every example.
- Source: general
- Disposition notes: Re-verified. Every `-o` path and the speedscope read are under `/tmp`. The rules name counters files and the 11 MB size.

## Issues

None.

## Duplicates / conflicts

- None. One reviewer. No new ids.
