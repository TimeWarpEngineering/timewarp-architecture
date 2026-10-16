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

- [ ] Confirm task 373 is merged and rebase on it
- [ ] Add the variant-set option to `dev template-smoke`; update its route description and Design region
- [ ] Edit `workflow.yml`: `schedule:` cron, nightly dispatch, default-only on push/PR, full matrix on nightly; keep the `release` job unaffected
- [ ] Assess per-variant test execution on the CI runner (Docker/Aspire); record findings in Notes
- [ ] Implement nightly test execution per variant, or record build-only as the floor and file the follow-up
- [ ] Confirm failed-nightly notification; record in Notes
- [ ] Update the skills that document template-smoke and the CI gates
- [ ] `dev build` 0 warnings and 0 errors
- [ ] `dev template-smoke` default-only passes
- [ ] Full-matrix `dev template-smoke` passes locally (run from a fresh `bin/dev`)
- [ ] PR CI run shows `template-smoke` running one variant; record the job duration
- [ ] Manually dispatched nightly run passes, or each failure is tied to a known task such as 374
- [ ] `ganda repo audit` clean

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

## Notes

- `dev template-smoke` already runs co-located Jaribu family aggregators (tier 3). The gap is the
  generated app's Aspire/host test projects, which task 374 shows breaking under yarp-off.
