# Make in-proc test ports overridable so template-smoke tier 3 does not collide with dev test

## Description

The in-proc test lane binds fixed ports (web 7000, web-http 7001, api 7255, yarp 8443) as
constants in `tests/common/timewarp-testing/applications/*-test-server-application.cs`.
`dev test` is serialized for that reason. `dev template-smoke` tier 3
(`tools/dev-cli/services/template-smoke-harness.cs`, `AssertJaribuFamilyAggregatorsAsync`) runs a
generated app's aggregators with bare `dotnet test`, and tier 2 `dotnet run`s its runfiles; both
bind the same fixed ports from a different directory. Result (seen on task 058-001, 2026-09-23):
running `dev template-smoke` while `dev test` is active fails SmokeNoPostgres with
`Fixed test port 7255 … already in use`. The harness comments say "Serial — api binds :7255", i.e.
the collision is documented as a rule for humans to remember. That is a tooling defect, not a
workflow constraint (Steve, 2026-09-23): fix the ports, do not serialize people.

## Requirements

- `timewarp-testing`: replace the port constants with a single resolved base. Read an
  environment variable (name it once, e.g. `TIMEWARP_TEST_PORT_BASE`; default keeps today's
  values so nothing else changes) and derive web/web-http/api/yarp from it — or read four
  explicit overrides if a single base cannot express the current spread (7000/7001/7255/8443).
  Every URL constant (`WebHostUrl`, `WebHttpUrl`, `ApiHostUrl`, the yarp cluster destination
  rewrite) must derive from the same resolved values; no second copy of a port anywhere in
  tests/ (grep `7000|7001|7255|8443` afterwards and justify any survivor).
- `template-smoke-harness.cs`: tiers 2 and 3 pass a different base to the generated app's
  processes (the harness already has an `environment` parameter on its process runner). Remove
  the "Serial — fixed port" comments and any serialization that exists only for that reason.
- Generated apps: the override is read by the same timewarp-testing code that ships in the
  template, so a generated app inherits it; document the variable in the testing library's
  Design region and in `tw-feature-placement` (C-create host lifetime section) if it names the
  fixed ports.
- AGENTS.md "Test host lanes" line that lists the fixed ports: state the default values and the
  override variable in one sentence.
- Gate: `dev test` and `dev template-smoke` run **concurrently** to completion (start one,
  start the other while it is mid-run) — both green. Then `dev build` 0/0.

## Checklist

- [ ] Port base resolved once in timewarp-testing (env override, defaults unchanged)
- [ ] All URL/port constants derive from it; no stray literals in tests/
- [ ] template-smoke tiers 2–3 run generated apps on a different base; serial-only-for-ports comments removed
- [ ] AGENTS.md + skill updated (one sentence each)
- [ ] Concurrent `dev test` + `dev template-smoke` both green; `dev build` 0/0

## Notes

- Origin: 058-001 template-smoke fix loop (PR #394) where the two gates had to be run serially.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
