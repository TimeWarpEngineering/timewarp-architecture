# Refresh vendored Aspire skills to 13.6.1 via aspire agent init --mcp

## Description

The vendored `.claude/skills/aspire*` and `.agents/skills/aspire*` skills still target Aspire 13.5
(hand-reconciled in task 209, commit b19ee730e), while the repo's Aspire SDK is 13.6.1. Regenerate
them with the 13.6.1 CLI and opt in to the AppHost-level MCP server.

## Guardrails (cockpit, 2026-10-17) — read before running anything

- **Global side effects.** `aspire agent init` previously wrote a `PostToolUse` hook
  (`~/.aspire/hooks/track-telemetry.sh`) into `~/.claude/settings.json` and touched
  `~/.agents/skills`. Before running it: snapshot `~/.claude/settings.json` and
  `$CLAUDE_CONFIG_DIR/settings.json` (cp to the task folder's scratch, not committed) and set
  `ASPIRE_CLI_TELEMETRY_OPTOUT=true`. After: diff both files and restore any change the init made.
  Report what it changed in Results.
- **Aspire CLI bump is machine-wide.** Bump only if `aspire --version` is below 13.6.1, using the
  official update path (`aspire update --self` or the documented installer), never a hand-copied
  binary. Record before/after versions.
- **Router skills are not CLI-managed.** `aspire agent init` only refreshes its bundle skills
  (`aspireify`, `dotnet-inspect`, now possibly `aspire-project-v2-migration`). The vendored
  `aspire`, `aspire-orchestration`, `aspire-monitoring`, `aspire-deployment`, `aspire-init` skills
  may be untouched by init; if so, update their 13.5-era content to 13.6.1 by hand against
  https://aspire.dev docs (13.4 `--search` filters, MCP limits, 13.6 changes) and say which files
  were hand-edited.
- **Do not run `aspire update` on the repo** (it reflows AppHost csproj attributes) — SDK is already
  13.6.1. Do not run `dev run` (worktree AppHosts share user secrets).
- Gates: `ganda repo audit`; `dev build` 0/0 only if non-skill files change; skills are public
  (no history, no client names).

## Goal (implementer restatement)

Bring the committed Aspire agent skills under `.claude/skills/aspire*` and `.agents/skills/aspire*`
from 13.5-era content to Aspire 13.6.1, using the 13.6.1 CLI's `aspire agent init --mcp` for the
CLI-managed bundle skills and hand edits (against aspire.dev) for any router skills init leaves alone.

Acceptance:
- Local `aspire` CLI is 13.6.1 (bump only if lower, via official path).
- `aspire agent init --skill-locations standard,claudecode --skills all --mcp` has been run; its repo
  output is reviewed and committed.
- Skills reflect 13.6.x: no stale "13.5" targeting; 13.4 `--search` filters and raised MCP limits mentioned.
- `.mcp.json` registers the AppHost-level `aspire agent mcp` server.
- Global side effects (`~/.claude/settings.json`, `$CLAUDE_CONFIG_DIR/settings.json`, `~/.agents/skills`)
  diffed, restored, and reported. No `aspire update` on the repo, no `dev run`.
- `ganda repo audit` passes; skills contain no history or client names.

## Checklist

- [x] On the ganda task branch, bump the local Aspire CLI from 13.6.0 to 13.6.1 (matches the repo's Aspire SDK 13.6.1)
- [x] Run `aspire agent init --skill-locations standard,claudecode --skills all --mcp`
- [x] Review the diff before committing:
  - [x] Task 209's 13.5 hand edits (b19ee730e) are replaced by the 13.6 output
  - [x] New skills `aspireify` and `aspire-project-v2-migration` appear
  - [x] `.mcp.json` still registers the AppHost-level `aspire` server (`aspire agent mcp`), not the dashboard MCP removed in 13.3

## Session

- Created: 938405 (2026-10-12)
- 2026-10-17: implement oracle (ganda task work) ran init, restored global side effects, hand-added MCP limits note.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-17T06:57:52Z
- Review oracle: tw-implementation-review round 1 (general, claude-opus-5-5), disposition clean — 2026-10-17

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

## Results

- **Aspire CLI:** already `13.6.1+3751e615a` before the walk, so no bump (before = after = 13.6.1).
- **Init:** `ASPIRE_CLI_TELEMETRY_OPTOUT=true aspire agent init --skill-locations standard,claudecode --skills all --mcp --non-interactive`.
  It refreshed **all** vendored skills, including the router skills (`aspire`, `aspire-orchestration`,
  `aspire-monitoring`, `aspire-deployment`, `aspire-init`), plus `playwright-cli`. It also added
  `aspireify`, `aspire-project-v2-migration`, `aspire/references/aspire-13-5-breaking-changes.md`, and
  `playwright-cli/references/pr-attachments.md`. `dotnet-inspect` did not change. `.claude/skills` and
  `.agents/skills` are byte-identical.
- **Task 209 (b19ee730e) hand edits:** replaced by the upstream 13.6 output. The upstream text now covers
  `describe --include-hidden`, `--isolated` worktree starts, and the 13.4 `--search` field filters
  (`aspire-monitoring/references/monitoring.md`).
- **Hand edit:** one file pair. `aspire-monitoring/SKILL.md` (both locations) gets a short "Aspire MCP
  server result limits" section (traces/structured logs 800, console logs 2000), because the upstream
  output does not mention the 13.4 limits. A later `aspire agent init` will overwrite it.
- **`.mcp.json`:** unchanged. It still registers `aspire agent mcp` (AppHost-level), not the dashboard MCP.
- **Global side effects (opt-out env var did NOT stop the hook install):**
  - `~/.claude/settings.json`: init added a `PostToolUse` hook to `~/.aspire/hooks/track-telemetry.sh`.
    **Restored** from the snapshot (diff clean).
  - `$CLAUDE_CONFIG_DIR/settings.json`: no change.
  - `~/.copilot/hooks/aspire-telemetry.json` (Copilot CLI telemetry hook): created by init. **Removed**,
    along with the empty `hooks/` dir.
  - `~/.copilot/mcp-config.json`: init rewrote it to register `aspire agent mcp`. No pre-snapshot, so it
    was left in place.
  - `~/.aspire/hooks/track-telemetry.{sh,ps1}`: written. Left in place; with the registrations removed
    nothing runs them.
  - `~/.agents/skills`: refreshed the aspire skills and added `aspire-project-v2-migration`. Left in place.
  - `.vscode/mcp.json` (gitignored, local only) written. "Installed Playwright CLI" also ran.
- No `aspire update` on the repo and no `dev run`. Only skill and kanban files changed, so `dev build`
  was not required.

### Review disposition

- **Rounds:** 1 · **Roster:** general · **Effort:** 3 (by-diff budget, 9837 lines)
- **Counts (final):** bug 0 · suggestion 0 · nit 0 (0 open, 0 fixed, 0 wontfix)
- **Disposition:** `clean` — all acceptance claims re-verified in the claim worktree; no findings.
- **Artifacts:** `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

- **Smoke:** `aspire --version && diff -r .claude/skills .agents/skills && ls .claude/skills && cat .mcp.json && grep -n "result limits" .claude/skills/aspire-monitoring/SKILL.md && ganda repo audit && grep -c track-telemetry ~/.claude/settings.json`
- **Expect:** version `13.6.1+…`; no diff output; listing includes `aspireify` and
  `aspire-project-v2-migration`; `.mcp.json` shows `aspire` → `agent mcp`; the limits heading is found;
  audit exits 0; the final grep prints `0`.
