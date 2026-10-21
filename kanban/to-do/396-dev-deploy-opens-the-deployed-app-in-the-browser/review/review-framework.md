# Review framework

## Budget (by-diff)

- Lines changed: 509
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/396-dev-deploy-opens-the-deployed-app-in-the-browser/
**Diff scope:** branch `task/396-dev-deploy-opens-the-deployed-app-in-the-browser` vs `master` (`git diff master...HEAD`, head ff9717e14)
**Plan / brief:** after a successful smoke, `dev deploy` opens the ingress through the opener `dev open` uses; `--no-browser` prints the URL; no terminal or no opener prints the URL; kubernetes without an external address prints the `dev open` hint (task.md `## Goal (implementer restatement)`)
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
