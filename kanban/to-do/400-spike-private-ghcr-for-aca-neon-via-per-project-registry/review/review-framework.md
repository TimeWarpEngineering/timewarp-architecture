# Review framework

## Budget (by-diff)

- Lines changed: 74
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/400-spike-private-ghcr-for-aca-neon-via-per-project-registry/
**Diff scope:** branch `task/400-spike-private-ghcr-for-aca-neon-via-per-project-re`, task commits
`c08c4e72c` and `c58e4ef1c` (`git diff e8f753ead..HEAD`). Local `master` is behind origin, so
`master...HEAD` also lists already-merged tasks 398, 402, and 403. Those are out of scope.
Product files: `skills/tw-deploy/references/azure-container-apps-neon.md` and
`source/container-apps/aspire/projects/aspire-app-host/program.cs` (Design region comment only).
**Plan / brief:** spike. Decide whether per-project `WithContainerRegistry(ghcr)` removes ACR Basic
from aca-neon. Record the failure in the guide's "Why the registry stays ACR" section.
**Effort:** 2. **Roster:** general.
**Session IDs:** review oracle claude-opus-5-5 (headless, session not exposed).
**Round 2 scope:** fix delta `94b1b38f1..28369a335`. Effort 1, roster general.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
