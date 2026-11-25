# Round 2 — general
**Date:** 2026-11-25
**Scope reviewed:** Delta since round 1, commit `157d2d5aa`. Product: `skills/tw-dotnet-diagnostics/SKILL.md`, `tools/dev-cli/services/template-smoke/template-smoke-generated-content.cs`. Kitchen: fix-pass text in `task.md` (`## Goal (implementer restatement)`, `## Learnings`, `## Results`, `### How to validate`).

## Summary

The fix pass does what M1–M3 asked. The skill says a generated app does not receive `.config/dotnet-tools.json`, shows the three `10.0.750501` entries with `rollForward: true`, and says to create the manifest when it is missing. `AssertSkillsShipped` requires `tw-dotnet-diagnostics/SKILL.md`. That list is the eleven `skills/*/` directories, and the success line prints the list length. Every capture path is under `/tmp`, and the rules name counters files and the 11 MB trace.

The counters section matches the trial capture `/tmp/t315/ms/counters.json` from the pinned exporter: `Events` with `name`, `tags`, `counterType`, and `value`; cumulative instruments as `Rate` with a `/ 1 sec` suffix; CPU split by `cpu.mode=user` and `cpu.mode=system`; gauges as `Metric`. The skill's averaging script on that file is the parse `## Results` records (user 1.34 plus system 0.65 cores, about 85 MB/s, pause 0.0116). That check was one targeted Python read of the existing capture, to test the suspected mismatch between the skill and the file. It matched. No build, test, audit, or hook was run. `## Results` records `dev build`, `dev template-smoke`, `dev lint-skills`, and `ganda repo audit` for this delta. The restatement mismatch (only `skills/**` is packed; the trial attached to a standalone `api-server`) is already under `## Learnings`.

## Issues

None.
