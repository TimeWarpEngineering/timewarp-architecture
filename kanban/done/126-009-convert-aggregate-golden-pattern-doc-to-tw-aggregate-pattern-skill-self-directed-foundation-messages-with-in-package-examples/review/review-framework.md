# Review framework — task 126-009

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-009-…/
**Diff scope:** commits `42f0808a` (skill + doc retirement + referrers) and `07f0c11f`
(foundation messages + XML examples)
**Plan / brief:** task.md is the spec (maintainer-decided constraints: public-skill style with
content parity; three-layer self-directed messages with NO file paths and verified-or-omitted
URL; XML `<example>` on IAggregateRoot/Entity<TId>; zero `aggregates/overview` hits outside
kanban).
**Effort:** 1 (general) + orchestrator gate verification
**Reviewer roster:** general
**Session IDs:** orchestrator Claude Fable (this session); implementer + reviewer Claude Sonnet
subagents.

## Gate status at review start (orchestrator-verified)

- `dev build` 0/0, `dev template-smoke` both matrices — green per implementer, tails quoted.
- `dev test`: 11 projects green in implementer run; 4 Docker-dependent projects
  (web-infrastructure, api-server-integration, web-spa-integration, aspire) failed in the
  implementer's sandbox with Docker-unhealthy errors. Docker verified healthy from the
  orchestrator shell; those four are being re-run by the orchestrator in parallel with this
  review — review disposition will not close until they pass.

## Ground rules

- Reviewer is read-only on product code; writes only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts open
- Do not invent issues; zero issues is a valid outcome
- Re-verify falsifiable claims against the repo (content parity, zero-hit greps, message text,
  public-skill style)
