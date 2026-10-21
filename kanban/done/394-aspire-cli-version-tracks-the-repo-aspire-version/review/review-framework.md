# Review framework

## Budget (by-diff)

- Lines changed: 357
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 394

**Date:** 2026-10-21
**Host task:** kanban/to-do/394-aspire-cli-version-tracks-the-repo-aspire-version/
**Diff scope:** branch `task/394-aspire-cli-version-tracks-the-repo-aspire-version` vs `master` (commit f78d8b41e), product paths: workflow.yml, AGENTS.md, Directory.Packages.props, tw-deploy SKILL.md, aspire-app-host.csproj, dev-cli services/endpoints, dev-cli-tests
**Plan / brief:** task.md `## Goal (implementer restatement)` and the original task (master copy of the flat task file)
**Effort:** 2 (by-diff budget); roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), session not reported

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

### Round 2

**Date:** 2026-10-21
**Diff scope:** fix delta `06881a0dc..949eb3dd8` (aspire-run.cs, aspire-cli.cs, aspire-run-tests.cs). Re-verify M1 and M2.
**Effort / roster:** 2, general
**Session IDs:** review oracle round 2 (claude, claude-opus-5-5), session not reported
