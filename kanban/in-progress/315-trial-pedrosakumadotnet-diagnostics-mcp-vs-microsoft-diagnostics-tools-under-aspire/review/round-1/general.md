# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/315-trial-pedrosakumadotnet-diagnostics-mcp-vs` vs `master`. Product files `.config/dotnet-tools.json`, `AGENTS.md`, `skills/tw-dotnet-diagnostics/SKILL.md`. Trial commits a4ffa32c7 (MCP registered) and 2d7b08eb0 (MCP removed). Kitchen `## Goal (implementer restatement)`, `## Trial`, `## Decision`, `## Results`, `### How to validate`.

## Summary

The keep/remove rule is applied correctly. The three Microsoft CLIs are pinned at 10.0.750501 with `rollForward: true` and a stated reason, licenses for all four tools are written down, the MCP was registered beside Aspire for the trial and then removed (`.mcp.json` matches master), and `AGENTS.md` gained one table row. The trial record is specific enough to support "not faster, and better only on allocation views the CLIs also reach."

The new skill is what generated apps actually receive (`skills/**` is packed; `.config/` and `.mcp.json` are not). It tells those apps the tools are already in a manifest they do not have, and it labels cumulative runtime counters as rates. Recorded gates cover `dev build`, `dev lint-skills`, and `ganda repo audit`. They do not cover template output.

No full build, test, audit, or hook was run for this review.

## Issues

### Issue 1 — Severity: bug
- File: skills/tw-dotnet-diagnostics/SKILL.md:10
- Description: The skill states that the three CLIs "are local tools in `.config/dotnet-tools.json`" and that `dotnet tool restore` then `dotnet <tool>` is how to run them. That is true in this repo. It is false in a generated app. `timewarp-architecture-template.csproj` packs `skills/**` (line 37) and does not pack `.config/dotnet-tools.json` or `.mcp.json` (root allow-list at lines 51-52). `AGENTS.md` says generated apps get `skills/` and that this `AGENTS.md` is not packed, so the new "Where to look" row does not reach them either. The only discovery surface they get is this skill, and its first instruction points at a file the template does not ship. `AssertSkillsShipped` still requires the previous ten skills and prints "ten SKILL.md files" (`template-smoke-generated-content.cs:5`, `:51-63`, `:84`). An eleventh packed skill does not fail that check, and the check never looks at the tool manifest. `## Results` records build, lint-skills, and audit, and says CI will run template smoke. It does not record `dev template-smoke`, which `AGENTS.md` names as the gate when template output changes. Template smoke as written would stay green either way.
- Suggestion: Make the shipped sentence true. In the skill, say the pins live in this repository's `.config/dotnet-tools.json` (version 10.0.750501, `rollForward: true`) and that a generated app does not receive that file. If `dotnet dotnet-counters --version` fails, add those three manifest entries and run `dotnet tool restore`. Add `tw-dotnet-diagnostics/SKILL.md` to `AssertSkillsShipped`'s required list and fix the "ten" wording so the smoke gate notices if the skill disappears from the pack. Record `dev template-smoke` (or the CI result) in `## Results` once that list matches what ships. Do not pack all of `.config/` unless that is a separate, explicit choice; the allow-list omits it today, and this task does not need the rest of that file in every generated app.
- Status: open

### Issue 2 — Severity: bug
- File: skills/tw-dotnet-diagnostics/SKILL.md:43
- Description: The skill tells an agent to read the last value of `dotnet.process.cpu.time` as "seconds per second" and of `dotnet.gc.heap.total_allocated` as "bytes per second". Both instruments are cumulative counters: CPU time in seconds, and approximate managed bytes allocated since process start (`GC.GetTotalAllocatedBytes`). The current `dotnet-counters` monitor example labels the allocation counter `(By)` and shows a value like `1.6947e+08`, not `(By / 1 sec)`. Rate labels in that tool look like `(Count / 1 sec)`. Dividing a cumulative `dotnet.process.cpu.time` by `dotnet.process.cpu.count` is not a share of the machine. The trial record itself says the counters JSON "needs a short parse for last values" and then reports 68-96 MB/s and about 2.3 cores, which are deltas. The skill drops that parse and states the wrong unit, so the next agent will report a lifetime total as a rate. `dotnet.gc.pause.time` is the same kind of cumulative seconds; the 1.4% pause figure in the trial is pause time over the window, not the raw counter.
- Suggestion: Say these three are cumulative. The rate is the delta between the last two samples divided by the sample interval (`--refresh-interval`, default 1s). CPU share is that `dotnet.process.cpu.time` rate divided by `dotnet.process.cpu.count`. Pause fraction is the `dotnet.gc.pause.time` delta divided by the same interval. Point at one JSON field name the 10.0.750501 exporter actually writes, taken from a capture this repo already produced, so the parse is not left as "read the last value".
- Status: open

### Issue 3 — Severity: suggestion
- File: skills/tw-dotnet-diagnostics/SKILL.md:37
- Description: The fenced commands write `counters.json`, `cpu.nettrace`, `alloc.nettrace`, `alloc.speedscope.json`, and `heap.gcdump` into the current directory (lines 37, 53, 65, 76, 85). The Rules section (lines 96-97) says to keep those files out of the repository and write them to a temp directory. Nothing in `.gitignore` matches `*.nettrace`, `*.gcdump`, or `*.speedscope.json`. An agent copies the fence. A 10-second CPU trace in the trial was 11 MB.
- Suggestion: Point every example `-o` path at `/tmp` (as `### How to validate` already does for the alloc trace). Keep the rule.
- Status: open
