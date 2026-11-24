# Review framework — task 315

**Date:** 2026-11-25
**Host task:** kanban/in-progress/315-trial-pedrosakumadotnet-diagnostics-mcp-vs-microsoft-diagnostics-tools-under-aspire/
**Diff scope:** branch `task/315-trial-pedrosakumadotnet-diagnostics-mcp-vs` vs `master` (4 commits). Product: `.config/dotnet-tools.json`, `AGENTS.md`, `skills/tw-dotnet-diagnostics/SKILL.md`. `.mcp.json` is unchanged vs master (registered in a4ffa32c7, removed in 2d7b08eb0). Kanban: flat to-do card moved to the in-progress folder and expanded.
**Plan / brief:** Time-boxed trial of pedrosakuma/dotnet-diagnostics MCP against `dotnet-counters`, `dotnet-trace`, and `dotnet-gcdump`. Keep the MCP only if it is clearly better and faster; otherwise remove it and keep the Microsoft CLIs plus a short skill.
**Effort:** 2 (general only; by-diff). Round 1 budget was 357 lines. Round 2 budget is 235 lines (below).
**Reviewer roster:** general
**Session IDs:** implementer claude `da67d941-9e83-4098-ae56-cac8b39ecc25`; review round 1 grok `01a20f55-6ee6-7e13-896f-e20bdede3a55`; review round 2 grok `01a20f6a-8b0b-7e00-8c18-618ad7c4773d`

## Budget (by-diff)

- Lines changed: 235
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-11-25
**Diff scope:** delta since round 1, commit `157d2d5aa` (fix pass). Product files: `skills/tw-dotnet-diagnostics/SKILL.md`, `tools/dev-cli/services/template-smoke/template-smoke-generated-content.cs`. Not a second pass over the whole branch.
**Session:** grok `01a20f6a-8b0b-7e00-8c18-618ad7c4773d`
