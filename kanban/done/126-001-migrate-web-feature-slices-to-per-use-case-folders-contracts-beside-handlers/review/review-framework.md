# Review framework — tasks 126-001 + 126-002 (combined pass)

**Date:** 2026-07-26
**Host task:** kanban/in-progress/126-001-migrate-web-feature-slices-to-per-use-case-folders-contracts-beside-handlers/ (126-002 reviewed on the same diff; flat sibling task)
**Diff scope:** commits `4442ca65` (126-002 evacuations), `5fff1e27` (126-001 use-case folders), `40409ed7` (skill docs) — i.e. `257d0ad1..40409ed7`
**Plan / brief:** [../migration-manifest.md](../migration-manifest.md) + maintainer resolutions
(U1 hello/hello literal, U2 chat by-direction folders collapsed, U3 domain namespace
`TimeWarp.Architecture.Features.Profiles.Domain`) in ../task.md. Executor deviation to verify:
`agent-token-authentication-handler.cs` → `agent-token-authentication-scheme-server.cs`
(TWA0015 collision — `handler` is the application-layer function token; escape hatch used).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator Claude Fable (this session); planner/executor/reviewer Claude Sonnet subagents

## Gates already run (verified by orchestrator, not claims)

- `dev build` 0/0 at three checkpoints (post-4442ca65, post-5fff1e27, post-doc-edits)
- `dev test` — all projects green, run twice (executor + orchestrator re-run, exit 0)
- `dev template-smoke` — both matrices (`SmokeDefault`, `SmokeNoPostgres`) SUCCEEDED, exit 0

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
