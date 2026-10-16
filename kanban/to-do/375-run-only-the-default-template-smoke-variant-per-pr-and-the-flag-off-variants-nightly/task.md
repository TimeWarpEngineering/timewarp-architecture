# Run only the default template-smoke variant per PR and the flag-off variants nightly

## Goal (implementer restatement)

PR and push CI run only the `SmokeDefault` template-smoke variant. A scheduled nightly run, also
manually dispatchable, runs the full flag-off matrix. Where the CI runner allows it, the nightly
also runs each variant's generated-app test suites, not only the build. Every flag then costs
nightly time, not PR time.

## Description

Every template flag adds a `SmokeMatrix` variant to `dev template-smoke` and roughly 2 minutes to
the slowest CI job, which gates every PR. Task 373 (PR #518) grew `SmokeMatrix` in
`tools/dev-cli/endpoints/template-smoke-command.cs` from 3 to 6 variants: `SmokeDefault`,
`SmokeNoPostgres`, `SmokeNoApi`, `SmokeNoWeb`, `SmokeNoGrpc`, `SmokeNoYarp`. CI speed is what
lets us iterate on functionality.

**Decision (Steve, 2026-10-16):** PR and push CI run only `SmokeDefault`. A nightly scheduled run
executes the full flag-off matrix. Rationale, in Steve's words: "Rework nightly for a specific
failure doesn't slow us down when we are working on functionality." A flag-off regression is
caught the next night instead of on the PR that caused it; that trade is accepted.

Second part of the decision: the nightly should run each flag-off variant's generated-app test
suites, not just build it. Task 374 (yarp-off Aspire test failures) was not caught by smoke
because `dev template-smoke` builds the generated solution and runs the co-located Jaribu family
aggregators (tiers 1 to 3), but does not run the generated app's Aspire/host test projects.

## Where to look

- `tools/dev-cli/endpoints/template-smoke-command.cs`: `SmokeMatrix` (around line 81), the
  `foreach` over it in `Handle` (around line 155), and the `[NuruRoute("template-smoke")]`
  declaration. The new option goes here. Supporting code: `tools/dev-cli/services/template-smoke-harness.cs`
  (`SmokeOneAsync`, tier 1 to 3 asserts).
- `.github/workflows/workflow.yml`: the `template-smoke` job (end of file) runs
  `dotnet run tools/dev-cli/dev.cs -- template-smoke`. Its `if:` already covers push, pull_request
  and `workflow_dispatch`. The header comment lists the jobs; update it.
- `tools/dev-cli/endpoints/workflow-command.cs`: `dev workflow` is mode-aware (`pr`, `merge`,
  `release`, auto-detected from `GITHUB_EVENT_NAME`; `--phase`, `--group`). It does not call
  template-smoke today; the CI job calls `dev template-smoke` directly. Confirm whether a nightly
  mode belongs in `dev workflow` or only in the `template-smoke` job, and keep one workflow file.
- `skills/tw-feature-placement/SKILL.md` (around line 248) and
  `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` (lines 114 to 120)
  describe `dev template-smoke` tiers. Search `skills/` again for any other mention.
- Task 373 (`kanban/done/373-*/task.md`, PR #518) and task 374 for the variants and known failures.

## Requirements

1. Add a `dev template-smoke` option that selects the variant set: default-only versus the full
   matrix. Choose the local default deliberately and record the reason in Notes; the PR/push CI
   job runs default-only. Filtering applies to the `SmokeMatrix` loop; the shared pack, install and
   dotted-name asserts run once either way. The Design region of `template-smoke-command.cs` and
   the route description change with the option.
2. Edit the single `.github/workflows/workflow.yml` (no second workflow file; mirror the repo CI
   pattern):
   - Add a `schedule:` cron trigger for the nightly.
   - Allow manual `workflow_dispatch` of the nightly matrix so a fix can be verified without
     waiting. Extend the existing `mode` input (`merge`, `probe`) or add a dedicated mode; the
     `release` job must not fire for a nightly or smoke-matrix dispatch (today any
     `workflow_dispatch` runs `release`; keep `probe` and `merge` behavior unchanged).
   - Scheduled and dispatched nightly runs pass the full-matrix option. Push and pull_request runs
     pass default-only. Scheduled runs only fire from the default branch; `detect-paths` and the
     other PR-only jobs must not run on a schedule.
   - Keep the smoke package eviction step and the NuGet cache key behavior.
3. Nightly test execution: for each flag-off variant, run the generated app's test suites in
   addition to the build. Confirm feasibility on the GitHub-hosted runner first (Docker for Aspire
   test containers, dev cert trust, run time) and record findings in Notes. If per-variant test
   execution is infeasible, build-only nightly is the floor and the gap is filed as a follow-up
   task. Coordinate with task 374 so its fix and this coverage agree.
4. Visibility: a failed scheduled run must notify (GitHub emails the workflow's last committer on a
   scheduled failure). Confirm that is enough, or record what else is needed. ganda is private and
   cannot run in CI, so CI does not auto-file kanban tasks; a human files the task from the
   failure.
5. Update the skills that describe `dev template-smoke` and the CI gates so they state the local
   default behavior, the option, the PR/push versus nightly split, and how to dispatch the nightly
   (`tw-feature-placement` references are known hits; check others under `skills/`). Skills are
   public: present tense, no history, no client names.
6. Template output does not change, so no version bump is expected; run `dev check-version` and
   follow `tw-pr` if the result says otherwise.

## Dependencies

- Lands after task 373 (PR #518), which edits the same `SmokeMatrix`.
- Runs serially with task 374 (yarp-off Aspire test failures); do not run workers in parallel.

## Checklist

- [x] Confirm task 373 is merged and rebase on it (373 = PR #518 and 374 = PR #519 are on master; merged origin/master)
- [x] Add the variant-set option to `dev template-smoke`; update its route description and Design region
- [x] Edit `workflow.yml`: `schedule:` cron, nightly dispatch, default-only on push/PR, full matrix on nightly; keep the `release` job unaffected
- [x] Assess per-variant test execution on the CI runner (Docker/Aspire); record findings in Notes
- [x] Implement nightly test execution per variant, or record build-only as the floor and file the follow-up
- [x] Confirm failed-nightly notification; record in Notes
- [x] Update the skills that document template-smoke and the CI gates
- [x] `dev build` 0 warnings and 0 errors
- [x] `dev template-smoke` default-only passes
- [x] Full-matrix `dev template-smoke` passes locally (run from a fresh `bin/dev`): every variant builds and passes tiers 1 to 3; `--app-tests` failures are tied to task 376
- [ ] PR CI run shows `template-smoke` running one variant; record the job duration (PR not open yet; the host opens it after this node)
- [x] Manually dispatched nightly run passes, or each failure is tied to a known task such as 374 (failures tied to task 376)
- [x] `ganda repo audit` clean

## Proof gates

- `dev build` 0 warnings and 0 errors.
- `dev template-smoke` default-only passes; full matrix passes locally.
- A PR CI run shows `template-smoke` running exactly one variant, with the job duration reported.
- A manually dispatched nightly run passes, or its failures are tied to known tasks (374).
- `ganda repo audit` clean.

## Session

- Created: 4014695 (2026-10-16)
- Decision recorded 2026-10-16 (Steve): PR/push CI runs `SmokeDefault` only; nightly runs the
  full flag-off matrix, with generated-app tests where feasible.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-16T07:07:20Z
- Review oracle: tw-implementation-review, effort 2, roster general (claude, model claude-opus-5-5), session not reported — 2026-10-16

## Notes

- `dev template-smoke` already runs co-located Jaribu family aggregators (tier 3). The gap is the
  generated app's Aspire/host test projects, which task 374 shows breaking under yarp-off.
- Options: `--matrix` (`-m`) runs every `SmokeMatrix` entry; without it the loop runs
  `SmokeDefault` only. `--app-tests` runs each generated app's test projects under the tier 3 MTP
  gate. The two options are independent. Pack, install, the dotted-name assert and the ingress-port
  assert run once either way. Default-only still makes two generations (Contoso.Shop plus
  SmokeDefault), so the ingress-port regeneration assert still has something to compare.
- Local default is default-only, on purpose: `dev template-smoke` with no options is the PR CI
  gate, so a local pass predicts the PR check. The full matrix costs several times longer, and the
  nightly covers it. AGENTS.md and the skill say to add `--matrix` when a change touches `#if flag`
  regions or template symbols.
- With `--matrix`, a failing variant no longer stops the loop. Every selected variant runs and the
  failures are reported together, so one nightly names every broken flag.
- `--app-tests` scope: every `tests/**/*.csproj` in the generated app, except tier 3 aggregators
  (already run) and `<IsTestProject>false</IsTestProject>` libraries (`timewarp-testing`). Runs one
  project at a time with the smoke port base, and runs all of them even after a failure.
- `dev workflow`: no nightly mode added. The `template-smoke` job calls `dev template-smoke`
  directly, and the nightly is that job alone, so a `dev workflow` mode would add nothing. One
  workflow file.
- workflow.yml: `schedule` cron `17 7 * * *` (07:17 UTC, off the top of the hour, when GitHub
  delays scheduled runs). New dispatch mode `nightly`. `release` skips on `mode == nightly`. `probe`
  is unchanged. A `merge` dispatch runs release as before and template-smoke default-only, the same
  as push. `detect-paths`, `ci-group`, `ci` and `skill-lint` are already restricted to
  push/pull_request, so a schedule runs only template-smoke. The NuGet cache key and the
  `2.0.0-smoke` eviction step are unchanged.
- CI runner feasibility for `--app-tests`: ubuntu-latest has Docker (the monorepo integration group
  already runs aspire-tests and Testcontainers there). The dev cert trust step and SSL_CERT_DIR are
  already in the job. Playwright installs its browser at run time (`playwright-browser-session.cs`),
  as the monorepo playwright group does without an install step. Measured run time is in Results.
- Notification: GitHub emails a failed scheduled run to the user who last changed the workflow's
  cron line, so committing this change makes Steve the recipient. That is enough. The repo is
  public, so GitHub disables a schedule after 60 days with no repository activity; that is not a
  risk at the current commit rate, and the email when it happens says so. CI cannot file kanban
  tasks (ganda is private), so a human files the task from the failure email.
- First full `--matrix --app-tests` run found new flag-off failures in generated web-spa test
  projects (SmokeNoApi: web-spa-integration and Playwright; SmokeNoYarp: web-spa-integration).
  They are template defects of the task 374 kind and out of this task's scope. Filed as task 376
  (id reserved with `ganda kanban reserve`, kitchen added on this branch). The nightly stays red
  until 376 lands.
- Task 374 agreement: task 374 fixed the yarp/api/postgres-off aspire-tests and decided that
  template-smoke would not run them per PR, leaving them to "the nightly flag-off matrix job". That
  is `--matrix --app-tests`.
- Pack scope: `tools/`, `.github/` and AGENTS.md are not in the template pack. `skills/` is packed,
  so the two `tw-feature-placement` edits reach generated apps (doc text only). `dev check-version`
  result is in Results.

## Results

PR and push CI now smoke `SmokeDefault` only. A nightly run smokes the full flag-off matrix and
also runs every generated app's test projects. The first nightly found new flag-off defects,
filed as task 376.

Changes:
- `tools/dev-cli/endpoints/template-smoke-command.cs`: `--matrix` / `-m` runs every `SmokeMatrix`
  entry; without it only `SmokeDefault` runs. `--app-tests` adds the generated-app test projects.
  The loop now runs every selected variant and reports all failures together. Route description
  and Design region updated.
- `tools/dev-cli/services/template-smoke-harness.cs`: `AssertGeneratedAppTestSuitesAsync` runs
  bare `dotnet test -c Release` in every generated `tests/**` project (not the tier 3 aggregators
  or `IsTestProject=false` libraries), one at a time, under the tier 3 MTP summary gate.
- `.github/workflows/workflow.yml`: `schedule` cron `17 7 * * *`; dispatch mode `nightly`;
  template-smoke runs `--matrix --app-tests` on schedule/nightly and default-only otherwise;
  `release` skips a nightly dispatch. Header comment updated.
- Skills: `tw-feature-placement` SKILL.md and `references/co-located-jaribu-runfiles.md` (default,
  options, PR versus nightly, dispatch command, failure email). AGENTS.md "Before a PR" row.
- `kanban/to-do/376-*`: follow-up for the failures the nightly found.

Measurements:

| Run | Scope | Duration | Result |
|-----|-------|----------|--------|
| PR CI before (run 38858670394, PR #519) | 6 variants, build plus tiers 1 to 3 | 10m13s | pass |
| Local `dev template-smoke` | SmokeDefault | 142s | pass |
| Local `--matrix --app-tests` | 6 variants plus app tests | 2035s | SmokeNoApi, SmokeNoYarp fail (task 376) |
| CI nightly dispatch [38860602411](https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/38860602411) | 6 variants plus app tests | 45m31s | SmokeNoApi, SmokeNoYarp fail (task 376); release skipped |

Per-variant app tests on the CI nightly: SmokeDefault 9/9 projects, SmokeNoPostgres 8/8,
SmokeNoWeb 2/2, SmokeNoGrpc 9/9 passed. SmokeNoApi and SmokeNoYarp each fail only
web-spa-integration-tests (task 376). Every variant builds and passes tiers 1 to 3, so the matrix
without `--app-tests` is green. The PR CI duration for the one-variant job is recorded once the
host opens the PR. Expect about 3 to 4 minutes based on the local 142s.

Gates: `dev build` (runfile) passed with 0 warnings and 0 errors. `dev check-version` passed
(2.0.0-beta.20 is unreleased, so no bump is needed). `ganda repo audit` passed.

### Review disposition

- Rounds: 1. Effort 2, roster: general.
- Final counts: bug 0, suggestion 0, nit 1 fixed (M1: default-only selection was positional
  `SmokeMatrix[..1]`; now the entry with no extra template args). Open 0, wontfix 0.
- Disposition: **clean**. Re-verified after the fix: `dev template-smoke` prints
  `Variants: SmokeDefault` and `Template smoke SUCCEEDED`.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

**Smoke:**

```bash
dotnet run tools/dev-cli/dev.cs -- template-smoke --help
dotnet run tools/dev-cli/dev.cs -- template-smoke
dotnet run tools/dev-cli/dev.cs -- template-smoke --matrix      # build + tiers 1-3, all variants
gh workflow run workflow.yml --ref <branch> -f mode=nightly      # CI nightly on demand
```

**Expect:**

- `--help` lists `--matrix,-m` and `--app-tests`.
- `template-smoke` prints `Variants: SmokeDefault (--matrix adds each flag off)` and
  `Template smoke SUCCEEDED`.
- `--matrix` prints all six variant names and `SmokeX OK` for each, then `Template smoke SUCCEEDED`.
- The nightly dispatch runs only `template-smoke` (release, ci, detect-paths and skill-lint
  skipped), and its step runs `template-smoke --matrix --app-tests`. Until task 376 lands, it fails
  with `Template smoke FAILED — SmokeNoApi, SmokeNoYarp`.
- On the PR for this branch, the `template-smoke` job log shows `Variants: SmokeDefault` only.

## Learnings

- Review found no misread: the implementer's Goal restatement matches the task's Description and Requirements.
