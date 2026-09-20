# Review framework — task 120

**Date:** 2026-09-21
**Host task:** kanban/in-progress/120-add-runtime-smoke-coverage-for-standalone-yarp-generated-routes/
**Diff scope:** branch `task/120-add-runtime-smoke-coverage-for-standalone-yarp-gen` vs `origin/master` (commits `8c2d5a6c`, `fa919546`)
**Plan / brief:** Add in-proc runtime smoke for standalone YARP generated `WebServerApiRoutePrefixes` (107 G3 follow-up). Boot Web.Server + standalone yarp without Aspire; assert `/api/Hello`, `/api/identity/session`, `/api/Roles` 401-not-404, and foreign-Host over the Development http cluster (`:7001`).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review 01a0bfe0-ebd8-7193-8446-0b19b43317fe (2026-09-21)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `tests/common/timewarp-testing/applications/web-test-server-application.cs`
- `tests/common/timewarp-testing/applications/yarp-test-server-application.cs`
- `tests/common/timewarp-testing/host-graph-factory.cs`
- `tests/container-apps/yarp/yarp-integration-tests/**`
- `.template.config/template.json`
- `timewarp-architecture.slnx`
- `AGENTS.md`
- `tools/dev-cli/endpoints/test-command.cs`
- `.gitignore` (oracle log ignore)

## Brief claims to re-verify

- Standalone gateway path (LoadFromMemory routes → config cluster `Web.Server`) is exercised at request time, not only at build.
- Development https→http cluster hop is covered (foreign-Host must not 502).
- Suite is in `dev test` glob; fixed ports web=7000, web-http=7001, yarp=8443.
- Template excludes the suite when `web` or `yarp` is off.
- Existing `CreateWebWithApiAsync` / `CreateWebApiYarpAsync` still compile and boot.
