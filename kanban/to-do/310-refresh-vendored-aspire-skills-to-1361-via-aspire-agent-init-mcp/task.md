# Refresh vendored Aspire skills to 13.6.1 via aspire agent init --mcp

## Description

The vendored `.claude/skills/aspire*` and `.agents/skills/aspire*` skills still target Aspire 13.5
(hand-reconciled in task 209, commit b19ee730e), while the repo's Aspire SDK is 13.6.1. Regenerate
them with the 13.6.1 CLI and opt in to the AppHost-level MCP server.

## Checklist

- [ ] On the ganda task branch, bump the local Aspire CLI from 13.6.0 to 13.6.1 (matches the repo's Aspire SDK 13.6.1)
- [ ] Run `aspire agent init --skill-locations standard,claudecode --skills all --mcp`
- [ ] Review the diff before committing:
  - [ ] Task 209's 13.5 hand edits (b19ee730e) are replaced by the 13.6 output
  - [ ] New skills `aspireify` and `aspire-project-v2-migration` appear
  - [ ] `.mcp.json` still registers the AppHost-level `aspire` server (`aspire agent mcp`), not the dashboard MCP removed in 13.3

## Session

- Created: 938405 (2026-10-12)

## Notes

- The `.claude/.agents` `aspire*` folders are committed `aspire agent init` output. `ganda skills sync`
  does not touch them (no `.ganda-source` marker; not a sync target — sync targets resolve to Flow's
  `claude/`, `grok/`, `opencode/` skills dirs), so there is no collision with Flow's shared skills.
- Aspire 13.6 made MCP opt-in for init: "`aspire agent init` defaults to skills without MCP ... or you
  can pass `--mcp`" — https://aspire.dev/whats-new/aspire-13-6/
- Aspire 13.4 (https://aspire.dev/whats-new/aspire-13-4/) added `--search` filters (including field
  filters like `@http.status_code:500`) on `aspire logs` and `aspire otel logs/traces`, and raised MCP
  tool limits (traces/structured logs 200 -> 800, console logs 500 -> 2000). Check the refreshed skills
  mention these.
