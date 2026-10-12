# Trial pedrosakuma/dotnet-diagnostics MCP vs Microsoft diagnostics tools under Aspire

## Description

Time-boxed trial of the pedrosakuma/dotnet-diagnostics MCP server/CLI against Microsoft's own diagnostics CLIs
(dotnet-counters, dotnet-trace, dotnet-gcdump) for agent-driven performance investigation of Architecture running
under Aspire. Busara rated the trial GREEN and taking it as a product dependency KILL. We have never used the
Microsoft tools before, so installing them and teaching agents about them is part of the value either way.
Background brief (Akili): `/workspace/ideas/2026-10-12-dotnet-diagnostics-intel.md` on the Grok Bot box.

## Requirements

1. **Install (dev-only, pinned).** Never as NuGet references in product code.
   - Add `dotnet-counters`, `dotnet-trace`, `dotnet-gcdump` to the local tool manifest `.config/dotnet-tools.json` with pinned versions.
   - Install the pedrosakuma/dotnet-diagnostics MCP server/CLI at a pinned version (tool manifest if it ships as a dotnet tool, otherwise a documented pinned install).
   - Check and record the licenses of all four.
2. **Make them known to agents.**
   - Register the MCP server in the repo's `.mcp.json`, alongside the Aspire MCP.
   - Add a short skill (or a brief AGENTS.md entry) saying when and how to use each tool. Keep it brief, per
     Flow task 135 (AGENTS.md hygiene); prefer a skill over AGENTS.md body text.
3. **Trial (1-2 hours).** Run Architecture under Aspire, then have an agent answer one real question (e.g. where
   CPU or allocations go on a hot endpoint) first with the MCP, then with the Microsoft CLIs. Compare speed and
   clarity; record both in this task.
4. **Decide.** Keep the MCP only if clearly better and faster, with a quarterly maintainer-health recheck.
   Otherwise remove it and keep the Microsoft tools plus the skill.

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
