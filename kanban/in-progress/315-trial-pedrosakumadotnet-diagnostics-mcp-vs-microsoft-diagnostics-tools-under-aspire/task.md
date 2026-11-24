# Trial pedrosakuma/dotnet-diagnostics MCP vs Microsoft diagnostics tools under Aspire

## Description

Time-boxed trial of the pedrosakuma/dotnet-diagnostics MCP server/CLI against Microsoft's own diagnostics CLIs
(dotnet-counters, dotnet-trace, dotnet-gcdump) for agent-driven performance investigation of Architecture running
under Aspire. Busara rated the trial GREEN and taking it as a product dependency KILL. We have never used the
Microsoft tools before, so installing them and teaching agents about them is part of the value either way.
Background brief (Akili): 2026-10-12 dotnet-diagnostics intel evaluation (no local file required).

## Requirements

1. **Install (dev-only, pinned).** Never as NuGet references in product code.
   - Add `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` to the local tool manifest `.config/dotnet-tools.json` with pinned versions.
   - Install the pedrosakuma/dotnet-diagnostics MCP server/CLI at a pinned version (tool manifest if it ships as a dotnet tool, otherwise a documented pinned install).
   - Check and record the licenses of all four.
2. **Make them known to agents.**
   - Register the MCP server in the repo's `.mcp.json`, alongside the Aspire MCP.
   - Add a short skill (or a brief AGENTS.md entry) saying when and how to use each tool. Keep it brief, per
     Flow task 135 (AGENTS.md hygiene); prefer a skill over AGENTS.md body text.
3. **Trial (time-boxed).** Run Architecture under Aspire, then have an agent answer one real question (e.g. where
   CPU or allocations go on a hot endpoint) first with the MCP, then with the Microsoft CLIs. Compare speed and
   clarity; record both in this task.
4. **Decide.** Keep the MCP only if clearly better and faster, with a quarterly maintainer-health recheck.
   Otherwise remove it and keep the Microsoft tools plus the skill.

## Goal (implementer restatement)

Give agents working in this repo (and in generated apps, since `.config/`, `.mcp.json`, and
`skills/` ship in the template) a dev-only way to answer "where does CPU / memory go" in a running
service, and find out whether the third-party MCP earns a place next to Microsoft's own CLIs.

Acceptance:

- `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` pinned in `.config/dotnet-tools.json`; no
  NuGet reference in product code.
- `dotnet-diagnostics-mcp` pinned for the trial; all four licenses checked and written down here.
- The MCP is registered next to the Aspire MCP for the trial.
- A short skill says when and how to use each tool; AGENTS.md gets at most one table row pointing
  at it.
- One real question is answered twice against a live Architecture service, once through the MCP
  and once through the CLIs, with time and output size recorded here.
- The keep/remove rule is applied: keep the MCP only if it is clearly better **and** faster;
  otherwise remove it from the manifest and `.mcp.json` and keep the CLIs plus the skill.
- `dev build` green, `dev lint-skills` green, `ganda repo audit` green.

## Out of scope (until the trial passes)

- BenchmarkDotNet diagnoser
- Nuru commands
- State dev tools integration

## Checklist

- [ ] Tools pinned in `.config/dotnet-tools.json`; MCP pinned; licenses recorded
- [ ] MCP registered in `.mcp.json` next to Aspire MCP
- [ ] Skill / brief AGENTS.md entry added
- [ ] Trial run and results recorded
- [ ] Keep/remove decision made and applied

## Learnings

## Session

- Created: grok-bot (2026-10-12)
