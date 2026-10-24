# Review framework

## Budget (by-diff)

- Lines changed: 480
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 379-017

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-017-extend-styleguidepage-contract-for-upstreamed-portal-components/
**Diff scope:** commit e97d8530e on `task/379-017-extend-styleguidepage-contract-for-upstreamed-port`
(the only task commit; the rest of `master..HEAD` is merged sibling 379 work). Product paths:
`tests/container-apps/web/web-spa-integration-tests/features/style-guide/style-guide-page-contract-tests.cs`
(new) and the Design note in `source/.../features/style-guide/pages/StyleGuidePage.razor.cs`.
**Plan / brief:** task.md `## Description` and `## Goal (implementer restatement)`: a static render
pins every upstreamed card and sample on `StyleGuidePage`, in page order; only the two form
fields store a value; the drawer follows `ApplicationState`; confirm dispatches `ConfirmSample`.
**Effort:** 2, by diff size
**Reviewer roster:** general
**Session IDs:** review oracle claude-opus-5-5 (Claude Code, headless print mode); session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
