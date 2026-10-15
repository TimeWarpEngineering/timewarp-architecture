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
- [x] Combined publish path (3 publishes, 1 test run); single-target commands unchanged
- [x] dev-cli tests; CI timing before/after (local timing below; CI timing comes from the PR's `ci (publish)` job)
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15 implementer (ganda task work): `dev publish all [--no-build]`; `dev workflow` uses it.

## Results

- New `dev publish all [--no-build]` (`tools/dev-cli/endpoints/publish-all-command.cs`): runs
  `aspire publish` for compose, kubernetes and aca into their existing
  `artifacts/aspire-output/<target>` folders, then ONE aspire-tests `dotnet test` with
  `TIMEWARP_COMPOSE_OUTPUT`, `TIMEWARP_HELM_OUTPUT` and `TIMEWARP_ACA_OUTPUT` all set, then
  `helm lint` on the chart (same opportunistic behaviour as before).
- Jaribu's `--filter-class` takes one substring, so the combined run filters on `Publish_Given_`.
  A dev-cli test scans aspire-tests and pins that this selects exactly `ComposePublish_Given_`,
  `KubernetesPublish_Given_` and `AcaPublish_Given_`.
- `dev workflow` (all phases and `--phase publish`) runs `dev publish all --no-build` after its
  Release solution build (aspire-tests is in the .slnx, so `--no-build --no-restore` is safe).
- `dev publish compose|kubernetes|aca` unchanged in behaviour (still build aspire-tests themselves).
- Targets moved to pure `tools/dev-cli/services/aspire-publish-targets.cs` (compile-included in
  dev-cli-tests); helm lint moved into `AspirePublish.HelmLintAsync` and is shared.
- Docs: `skills/tw-deploy/SKILL.md` lists `dev publish all`; workflow.yml comment updated.

Timing (local, WSL, after a Release `dev build`, warm caches):

| Path | Wall time | Safety facts |
|------|-----------|--------------|
| Before: `dev publish compose` + `kubernetes` + `aca` (3 test runs, each builds) | 149 s | 9 + 10 + 10 |
| After: `dev publish all --no-build` (1 test run, no build) | 116 s | 29 |

CI before: the task baseline (~2 min for the three publishes). CI after: read the `ci (publish)`
job's "Run CI Pipeline" step on the PR.

Gates: `dev build` 0 warnings / 0 errors; dev-cli-tests 239/239; `dev test --no-build` fast and
playwright groups green; integration group green except a local Testcontainers Ryuk start timeout
in web-infrastructure-tests (untouched by this change), which passed 78/78 on rerun.

### How to validate

Smoke:
```bash
dev build
dev publish all --no-build
dev publish kubernetes
```

Expect:
- `dev publish all --no-build` prints three `aspire publish (<target>)` lines, then ONE
  `Checking compose, kubernetes, aca output for production safety...` test run reporting
  `total: 29, failed: 0`, then `helm lint` (or the skip notice), then
  `All publish outputs are production-safe:` with the three directories; exit 0.
- `dev publish kubernetes` still publishes only the chart, runs only `KubernetesPublish_Given_`
  (10 facts) and helm-lints it; exit 0.
- `dev workflow --phase publish` shows `Pipeline: clean -> build -> publish all`.
