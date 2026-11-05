# Review framework

## Budget (by-diff)

- Lines changed: 167
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-05
**Host task:** kanban/in-progress/480-bump-outdated-third-party-packages-fastendpoints-beta7-blazilla-243-markdig-160-simple-icons-16360/
**Diff scope:** branch `task/480-bump-outdated-third-party-packages-fastendpoints-b` vs `master` (commit b3f6aa77d; product delta `Directory.Packages.props`)
**Plan / brief:** bump the five third-party pins listed in task.md and change nothing else
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and its surrounding call sites. Re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
