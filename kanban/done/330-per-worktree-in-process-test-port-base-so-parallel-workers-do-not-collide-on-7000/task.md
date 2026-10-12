# Per-worktree in-process test port base, so parallel workers do not collide on 7000

## Problem

The in-process test lane (`tests/common/timewarp-testing/in-proc-test-ports.cs`, `InProcTestPorts`)
defaults to fixed ports: web 7000, web-http 7001, api 7255, yarp 8443, yarp-http 8444. Only
`TIMEWARP_TEST_PORT_BASE` shifts them, and it shifts the whole set by fixed offsets. `dev test`
serializes projects *within one process tree*, but **two worktrees running `dev test` at the same
time both use 7000**.

On 2026-10-12, the 319 worker's `dev test` had timewarp-testing-tests, web-jaribu-tests and
web-server-integration-tests fail on port 7000, held by another concurrent run (most likely the
326 worker in its own worktree). Rerun with `TIMEWARP_TEST_PORT_BASE=47000`, everything passed.
Parallel workers are now normal (`ganda task work` on several tasks), so this recurs.

319 fixed the closed-box lane (dynamic Aspire ports). This task fixes the in-process lane.

## Requirements

- **Deterministic per-checkout default.** When `TIMEWARP_TEST_PORT_BASE` is unset, derive the base
  from the checkout's absolute root path (the directory holding the solution `.slnx`) with a
  simple stable hash. Map it into a safe range so base + the largest offset (+1444) stays below
  65535 and avoids common ports and the ephemeral range. Suggested: bases in 20000–40000, aligned
  to a step ≥ 1500 so two bases never overlap.
  - Put the derivation in `InProcTestPorts` itself, so every entry point gets it: `dev test`,
    plain `dotnet test` / `dotnet run` of a project, and Jaribu runfiles. Only fall back to doing
    it in `dev test` if the in-process resolution can't find the root; record the reason.
  - Do not call or reimplement ganda's `generate-port`. ganda is private; this is a small,
    self-contained hash.
- **Explicit override still wins.** `TIMEWARP_TEST_PORT_BASE` set means use it, exactly as today.
  Template-smoke keeps its explicit 17000 for generated apps, or switches to the derived default
  if that is cleaner; record the choice.
- **Collision fallback.** If the derived web port is already in use at host start, fail with a
  clear message naming the port, the derived base, the checkout path and the override variable.
  Never silently pick another port mid-run, because child processes must agree.
- **Child processes agree.** `dev test` resolves the base once and exports it to every child test
  process, so a suite and anything it spawns use the same set.
- **Update every comment and help text** that says "defaults 7000/7001/7255/8443":
  - `host-graph-factory.cs`, `web-test-server-application.cs`, `api-test-server-application.cs`;
  - `test-command.cs`, `template-smoke-harness.cs`;
  - the `tw-feature-placement` co-located-runfiles reference, and the AGENTS.md host-lane text if
    it names the defaults.
- **Tests:**
  - derivation is stable for one path and differs across two paths (fixture paths, no real ports);
  - every derived base lands in range and aligned;
  - the explicit override wins;
  - the collision message.
- **Proof:** run `dev test` in this worktree while a throwaway listener holds 7000. The in-process
  suites must pass on the derived base.

## Checklist

- [x] Derived per-checkout base in `InProcTestPorts` (range/alignment), override precedence
- [x] Collision message; `dev test` exports the resolved base to children
- [x] Comments/help/skill text updated
- [x] Tests + proof run with 7000 occupied
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve; found by the 319 worker)
- 2026-10-12 implementer (ganda task work): implemented, gates green, results below.
- 2026-10-12 review oracle (ganda task work, Claude Code headless): tw-implementation-review effort 2, roster general; disposition clean.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-12T16:23:30Z

## Results

**Derivation (`tests/common/timewarp-testing/in-proc-test-ports.cs`).** Precedence:
`TIMEWARP_TEST_PORT_BASE` (exact, as before) → base derived from the checkout root → legacy 7000.
The checkout root is the nearest ancestor holding a `*.slnx`, searched from the runfile's source
directory (`AppContext` `EntryPointFileDirectoryPath`), then `AppContext.BaseDirectory`, then the
current directory. So `dev test`, plain `dotnet test`, and Jaribu runfiles (built in the runfile
cache) all find it in-process, and the `dev test`-only fallback was not needed. Hash: FNV-1a over the
normalized absolute path (separators `/`, trailing separator trimmed, upper-cased on Windows). Bases:
20000 + n·1500, n in 0..12 (13 slots; the highest port is 38000+1444 = 39444 < 40000). Step 1500 is
larger than the 1444 spread, so two different bases never share a port. Collisions between checkouts
are possible (1 in 13) and fail loudly.

**Collision.** `HostGraphFactory.EnsurePortIsFree` now reports the port, the host, `InProcTestPorts.Describe()`
(base, source, checkout path, and the derived base when an override is set) and the override variable.
It never moves a port.

**`dev test`.** Compiles `in-proc-test-ports.cs` in (linked from `tools/dev-cli/Directory.Build.props`,
so there is one source), resolves the base once, prints it, and exports `TIMEWARP_TEST_PORT_BASE` to every
child `dotnet test`.

**Template-smoke choice.** Dropped the fixed 17000. Tiers 2–3 now pass the generated app's own derived
base (from its `artifacts/template-smoke/work/<app>` path), bumped one slot if it equals this checkout's
base. Reason: a fixed 17000 collided when two worktrees ran template-smoke at the same time.

**Text.** Comments and help updated in host-graph-factory, web/api/yarp test server applications,
test-command, template-smoke-harness, the co-located-runfiles skill reference, and the comments that
mentioned :7000/:7255/:8443. AGENTS.md does not name the defaults, so it was not changed.

**Tests.** `tests/common/timewarp-testing-tests/in-proc-test-ports-tests.cs` (11): stable per path,
different across two paths, 500 fixture paths in range/aligned and covering all 13 slots, next-slot wrap,
override wins, derive when unset, fallback without root, override range rejection, `*.slnx` root
discovery, collision message (real held port 0) and derived-describe text.

**Gates (this worktree).** `dev build` 0 warnings / 0 errors. `dev test` with a python listener holding
127.0.0.1:7000: all 23 projects passed on derived base 32000. Plain `dotnet test` in timewarp-testing-tests
(14/14) and `dotnet run` of get-weather-forecasts-tests.cs from `/tmp` (5/5) passed with 7255 held.
`dev template-smoke` SUCCEEDED. `ganda repo audit` passes. `dev check-version` passes (2.0.0-beta.20 is
already ahead of NuGet).

**Review disposition.** `clean` after 1 round (effort 2, roster: general). Final counts: bug 0, suggestion 0,
nit 2 (both fixed, 0 open, 0 wontfix). M1: the Design region now states that slots 32000+ overlap Linux's ephemeral
range and why 13 slots are kept. M2: the override `Describe()` text no longer says `dev test` exports the derived base.
Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:
```bash
./bin/dev self-install   # or: dotnet run tools/dev-cli/dev.cs -- self-install
python3 -c "import socket,time;s=socket.socket();s.bind(('127.0.0.1',7000));s.listen();time.sleep(3000)" & H=$!
./bin/dev test --quiet; kill $H
dotnet run tests/common/timewarp-testing-tests/in-proc-test-ports-tests.cs
TIMEWARP_TEST_PORT_BASE=47000 ./bin/dev test --quiet 2>&1 | head -1
```

Expect: the first line of `dev test` is `In-proc test ports: base <20000..38000, step 1500> derived from checkout <this worktree path>`,
and "Tests completed successfully!" while 7000 is held. The runfile reports 11/11 passed. With the override,
the first line reads `base 47000 from TIMEWARP_TEST_PORT_BASE (...)`. A second worktree prints a different
base (or, 1 time in 13, the same base, and a held port then fails with the port, base, checkout path and
`TIMEWARP_TEST_PORT_BASE` in the message).

