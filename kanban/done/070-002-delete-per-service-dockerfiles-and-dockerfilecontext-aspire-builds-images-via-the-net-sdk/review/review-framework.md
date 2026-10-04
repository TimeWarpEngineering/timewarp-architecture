# Review framework

## Budget (by-diff)

- Lines changed: 128
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 070-002

**Date:** 2026-10-04
**Host task:** kanban/to-do/070-002-delete-per-service-dockerfiles-and-dockerfilecontext-aspire-builds-images-via-the-net-sdk/
**Diff scope:** branch `task/070-002-…` vs `master` (commit cd048355)
**Plan / brief:** delete grpc-server Dockerfile, VS Docker tooling props (`DockerfileContext`, `DockerDefaultTargetOS`) from four csproj; record SDK container-build choice in AppHost Design region; update task 272 Dockerfile step.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** headless ganda task-work review oracle (Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
