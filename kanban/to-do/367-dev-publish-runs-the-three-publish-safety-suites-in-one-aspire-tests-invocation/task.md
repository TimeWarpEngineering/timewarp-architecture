# dev publish runs the three publish safety suites in one aspire-tests invocation

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

`dev workflow` runs `dev publish compose`, then `kubernetes`, then `aca`. Each one runs its own
`aspire publish` **and** its own `dotnet test` of aspire-tests' safety facts, which rebuilds and
starts the test host three times. That takes about 2 min in CI.

## Requirements
- Run the three `aspire publish` commands first, writing to their existing output folders, then run
  the safety suites (`ComposePublish_Given_`, `KubernetesPublish_Given_`, `AcaPublish_Given_`)
  in **one** aspire-tests invocation, with the three `TIMEWARP_*_OUTPUT` variables set. Also offer
  a combined `dev publish all` (or similar) that `dev workflow` uses.
- `dev publish <target>` alone still works for a single target.
- Use `--no-build` for the test run when the solution is already built (pairs with 365).
- `helm lint` behaviour unchanged.
- Before/after timing in the PR.

## Checklist
- [ ] Combined publish path (3 publishes, 1 test run); single-target commands unchanged
- [ ] dev-cli tests; CI timing before/after
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
