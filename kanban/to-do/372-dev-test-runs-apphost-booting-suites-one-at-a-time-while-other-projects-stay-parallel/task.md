# dev test runs AppHost-booting suites one at a time, while other projects stay parallel

## Problem (2026-10-15, master PR #515, `feature/ci-speed`)

Task 370 made `dev test` run projects in parallel: up to 4 at once, and at most 3 in the integration
group. On #515 the **push** CI run failed `ci (integration)` while the **PR** run on the same commit
passed. In the failing run:
- `yarp-integration-tests` failed every test in setup ("The response ended prematurely",
  `EndOfStreamException`).
- Earlier, the Aspire DCP logged `Could not perform substitution for environment variable
  ASPNETCORE_URLS … portForServing "web-server-https"`, the same DCP failure mode the AppHost Design
  region records (tasks 155 and 270).

It was running `api-jaribu-tests`, `api-server-integration-tests` and `aspire-tests` at once, plus
`web-infrastructure-tests`. **More than one closed-box suite boots a real Aspire AppHost**
(`DistributedApplicationTestingBuilder`, via the `ClosedBoxAppHost` helper from 319). Concurrent
AppHost/DCP startups interfere, so the result is flaky.

This is part of the CI speed-up stack: it runs as a child of `feature/ci-speed`, and #515 picks it up.

## Requirements

- **Exclusive lane for AppHost suites.** Projects that boot an Aspire AppHost never run at the same
  time as each other. Everything else stays parallel with them and with each other.
  - **Find AppHost suites by derivation, not a hand list.** For example, a project references
    `Aspire.Hosting.Testing` or compiles `tests/common/closed-box-app-host/closed-box-app-host.cs`.
  - Add a guard test that every AppHost-booting project is classified exclusive.
- Keep 370's per-project port-base slots and Docker concurrency limits. Testcontainers suites
  (web-infrastructure) may stay parallel with AppHost suites unless evidence says otherwise; record it.
- Investigate whether `yarp-integration-tests` failed because of the AppHost interference or
  something else, such as its own standalone YARP host and port. Make it deterministic either way.
- **Proof:** run the integration group **10×** in CI (a throwaway loop job, or `workflow_dispatch`
  reruns), or 10× locally with `-j 4` on a 4-core-equivalent limit. Zero failures, with timings. The
  integration job may get slightly slower; record before/after.

## Checklist

- [ ] Derived AppHost-suite classification + guard test
- [ ] Exclusive lane in the parallel runner; other projects stay parallel
- [ ] yarp-integration root cause recorded and fixed
- [ ] 10× integration runs green; timing before/after
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-15 (cockpit; #515 push-run failure)
