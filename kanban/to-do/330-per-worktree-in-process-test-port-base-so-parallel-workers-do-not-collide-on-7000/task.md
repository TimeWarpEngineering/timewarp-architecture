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

- [ ] Derived per-checkout base in `InProcTestPorts` (range/alignment), override precedence
- [ ] Collision message; `dev test` exports the resolved base to children
- [ ] Comments/help/skill text updated
- [ ] Tests + proof run with 7000 occupied
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve; found by the 319 worker)
