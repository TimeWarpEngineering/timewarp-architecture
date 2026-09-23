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

- [x] Port base resolved once in timewarp-testing (env override, defaults unchanged)
- [x] All URL/port constants derive from it; no stray literals in tests/
- [x] template-smoke tiers 2–3 run generated apps on a different base; serial-only-for-ports comments removed
- [x] AGENTS.md + skill updated (one sentence each)
- [x] Concurrent `dev test` + `dev template-smoke` both green; `dev build` 0/0

## Notes

- Origin: 058-001 template-smoke fix loop (PR #394) where the two gates had to be run serially.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED
- Resume (2026-09-23): a first implementer pass (Cursor) left the change uncommitted and its walk
  died on result-line parsing. The second pass (Claude Fable 5.1, implement oracle) resolved the
  open item below, re-ran every gate in the foreground, and committed.
- Open item resolved: the `ReverseProxy` / `ServiceCollectionOptions` (web-server-integration-tests)
  and `Services` / `ReverseProxy` / `ServiceCollectionOptions` (web-spa-integration-tests) sections
  in the consumer `appsettings.json` files were DEAD config carrying port literals — Web.Server never
  calls `AddReverseProxy`, nothing binds `ServiceCollectionOptions`, and `ServiceUriHelper` reads
  Aspire `services__*` env vars, not the `Services` section. Deleted rather than derived (no
  consumer to feed). The consumed sections (Logging, WebAuthnOptions, AgentTokenOptions,
  SampleOptions, AzureAd) are untouched.

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implement (resume, ganda task work headless): 2026-09-23, Claude Fable 5.1
- Review oracle (ganda task work headless): 2026-09-23, Claude Fable 5.1 (general reviewer, effort 1; gate re-run delegated to a sonnet subagent)

## Results

`InProcTestPorts` (`tests/common/timewarp-testing/in-proc-test-ports.cs`) resolves the in-proc port
set once per process from `TIMEWARP_TEST_PORT_BASE` (default 7000; fixed offsets +0/+1/+255/+1443
→ web 7000, web-http 7001, api 7255, yarp 8443, so defaults are unchanged). The three
`*TestServerApplication` classes, `HostGraphFactory` (preflight + teaching error), and the YARP
cluster-destination rewrite all derive from it. `template-smoke-harness.cs` tiers 2 and 3 pass
`TIMEWARP_TEST_PORT_BASE=17000` to the generated app's `dotnet run` / `dotnet test` processes; the
"Serial — fixed port" comments are gone (files still run one at a time within a tier because they
share the smoke base — that is intra-tier, not a rule for humans). `dev test` still serializes
projects because they share one base inside the monorepo. AGENTS.md "Test host lanes" and the
`tw-feature-placement` C-create reference name the defaults and the variable. Purpose/Design
comments in `get-weather-forecasts-tests.cs`, `web-authn-options-application.cs`, and
`webauthn-relying-party.cs` no longer describe the ports as fixed.

**Port-literal grep survivors (`grep -nE "\b(7000|7001|7255|8443)\b" tests/`):** comments naming
the defaults; `InProcTestPorts.DefaultBase = 7000` (the single source); WebAuthn ceremony
`Origin`/`clientDataJSON` fixture strings in the host-free `timewarp-identity-tests` (parsed
bytes, no socket bind); `019f6a8b-0000-7000-…` Guid fixtures. Nothing binds a literal port.

**Gates (2026-09-23, foreground):**
- `dev build` → 0 Warning(s) / 0 Error(s).
- `dev test` (10:30:53–10:40:53) and `dev template-smoke` (10:31:28–10:40:59) run concurrently
  from the same worktree: 22 test projects all `failed: 0`; SmokeDefault / SmokeNoPostgres /
  SmokeNoApi OK with tiers 2 and 3 executing generated runfiles and aggregators; zero
  "already in use" in either log.
- Override proof: with a foreign listener bound on 127.0.0.1:7255, `dotnet run
  get-weather-forecasts-tests.cs` fails with the teaching error on the default base and passes
  with `TIMEWARP_TEST_PORT_BASE=17000`.
- `ganda repo audit` → passes all checks.

### Review disposition

- Rounds: 1; roster: general (effort 1). Artifacts: `review/review-framework.md`,
  `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Final counts: bug 0 / suggestion 0 / nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions** — M1 (nit): `TIMEWARP_TEST_PORT_BASE` is a second string
  literal in `template-smoke-harness.cs`; dev-cli cannot reference the testing library and a
  divergence is self-diagnosing via the concurrent-gate teaching error. Decided by review oracle.
- Reviewer re-ran gates in this worktree (2026-09-23): `dev build` 0/0; `yarp-integration-tests`
  under `TIMEWARP_TEST_PORT_BASE=17000` 4/4 (hosts on 17000/17001/18443, cluster rewrite → 17001 —
  the one path tier 3 never exercises); invalid value raises the teaching error; 7255-listener proof
  fails bare / passes on 17000 (5/5); `ganda repo audit` passes.

### How to validate

- **Smoke:** From the worktree, start `dev test`; while it is mid-run (any "Testing …" line
  printed), start `dev template-smoke` in a second shell. After both exit, run `dev build`.
  Then, with a scratch listener holding 7255 (e.g. `python3 -c "import socket,time;
  s=socket.socket(); s.bind(('127.0.0.1',7255)); s.listen(1); time.sleep(300)"`), run
  `dotnet run source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`
  once bare and once with `TIMEWARP_TEST_PORT_BASE=17000`.
- **Expect:** both concurrent gates exit 0 with no "In-proc test port … already in use" in either
  output; `dev build` reports 0 Warning(s) / 0 Error(s); the bare runfile fails with
  "In-proc test port 7255 … set TIMEWARP_TEST_PORT_BASE …" and the 17000 run passes (Grand Total
  0 failed).
