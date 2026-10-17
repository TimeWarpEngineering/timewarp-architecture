# Review framework

## Budget (by-diff)

- Lines changed: 785
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 343-001

**Date:** 2026-10-17
**Host task:** kanban/to-do/343-001-webmcp-tools-register-on-page-load-and-are-discoverable-over-http/
**Diff scope:** commit `b3a18de22` (`ffc54646f..b3a18de22`). The seven task-371 commits on this branch before `ffc54646f` are out of scope (reviewed under 371).
**Plan / brief:** `## Goal (implementer restatement)` and `## Requirements` in `task.md`. Page-load `navigate` registration, `/.well-known/webmcp.json` generated from the page registry, WebMCP sections in `/llms.txt`, `/auth.md`, and the discovery skill, and auth.md agent registration wording.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
