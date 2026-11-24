# Add --project and class-method filter options to dev test

## Description

Walk performance analysis (Task 514, `/tw-correct` Hand-off A) found **14+ occurrences across 4 tasks** (`495`, `453`, `490`, `489-001/002/004/005`) of agents failing when trying to run a targeted test project or test class:

- `./bin/dev test` (`tools/dev-cli/endpoints/test-command.cs`) currently supports `--group` only — it has no `--project`, `--filter-class`, or `--filter-method` option.
- Because `dev test` is repo-local (not a shared `TimeWarp.Nuru.DevCli` endpoint), any agent trying to run a single test project or test class drops down to raw `dotnet test <csproj>` from repo root.
- On .NET 11, root `global.json` intentionally omits the Microsoft.Testing.Platform runner (which lives per-project inside each test project's `global.json`), so running `dotnet test <csproj>` from repo root fails immediately with `"Testing with VSTest target is no longer supported"`.
- Agents also run multiple raw `dotnet test` commands in parallel without `--no-build`, causing `MSB3026` file-lock collisions on shared `obj/Debug` directories (`489-002`, `489-004`).

Per `/tw-correct` Level 1 (Structure): give agents **one** repo-root command (`./bin/dev test`) that supports targeted project and class/method runs while preserving the existing serial Release build, project-directory `cwd`, `--no-build --no-restore`, and port-base isolation.

## Requirements

1. **Add `--project` (`-p`) to `TestCommand` (`tools/dev-cli/endpoints/test-command.cs`):**
   - Filter discovered test `.csproj` files by project name (exact stem match or unambiguous substring/relative-path match; repeatable or comma-separated so multiple projects can run in one invocation).
   - Fail cleanly with a clear error naming available test projects when `--project` matches nothing.
   - When `--project` selects a subset and `--no-build` is not passed, keep the build fast and collision-free (serial build before running selected projects with `--no-build --no-restore`).
2. **Add `--filter-class` and `--filter-method` to `TestCommand`:**
   - Forward `--filter-class <value>` and `--filter-method <value>` to Microsoft.Testing.Platform (`DotNetTestArguments.PlatformArguments` / `BuildTestCommand`) so agents can target one or more classes/methods in a single Aspire/Jaribu run without dropping to raw `dotnet test`.
   - Keep existing `--group`, `--quiet`, `--no-build`, `--max-parallel`, and `--max-integration` behavior unchanged when the new options are omitted.
3. **Update repo guidance:**
   - Add a one-line example of `./bin/dev test --project <name> --filter-class <pattern>` to `AGENTS.md` (and `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` if it mentions targeted test runs).
4. **Tests & verification:**
   - Add unit tests in `tools/dev-cli` tests covering `--project` selection (exact, substring, unknown error) and forwarding `--filter-class` / `--filter-method` into MTP arguments.
   - Rebuild `./bin/dev` (`dotnet run tools/dev-cli/dev.cs -- self-install`), run `./bin/dev build`, `./bin/dev test --project dev-cli-tests` (dogfooding the new flag), and `ganda repo audit`.

## Goal (implementer restatement)

Agents need one repo-root command for a targeted test run, so they stop falling back to raw
`dotnet test <csproj>` (which fails on .NET 11 MTP from the repo root and races on `obj/`).

Acceptance:

- `dev test --project <name>` (`-p`, repeatable and comma-separated) runs only the matching
  projects under `tests/`. A name matches by exact project stem first, else by a substring of the
  stem or repo-relative path that hits exactly one project. No match fails with the list of test
  projects; an ambiguous substring fails with the candidates.
- With `--project` and no `--no-build`, the build phase builds only the selected projects, one at
  a time, in Release; every `dotnet test` stays `--no-build --no-restore`, cwd = project directory,
  one port base per worker.
- `--filter-class <value>` and `--filter-method <value>` go after `--` to Microsoft.Testing.Platform
  (Jaribu) for every selected project.
- With none of the new options, `dev test` (and `--group`, `--quiet`, `--no-build`,
  `--max-parallel`, `--max-integration`) behaves as before.
- `AGENTS.md` shows one example; dev-cli-tests pin project selection (exact, substring, unknown,
  ambiguous) and filter forwarding.

## Checklist

- [x] Add `--project` (`-p`), `--filter-class`, and `--filter-method` options to `tools/dev-cli/endpoints/test-command.cs`
- [x] Forward filter arguments cleanly to MTP and preserve serial build + per-project `cwd` + port isolation
- [x] Add unit tests for project selection and MTP filter argument forwarding
- [x] Add one-line usage example to `AGENTS.md` (and testing reference if applicable)
- [x] Run `self-install`, `./bin/dev build`, targeted `./bin/dev test`, and `ganda repo audit`

## Session

- Created: 2875948 (2026-11-24)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 82453250-7c5e-418e-b992-ca41b6d172e4, max-turns 500, Done — 2026-11-24T10:15:57Z
- Review oracle: general (grok, vendor xai, model grok-4.7), session 01a20d0e-44c9-7df2-b28b-c8838c79be5e, effort 2, roster general, max-turns 120, round 1, clean — 2026-11-24
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20d0e-44c9-7df2-b28b-c8838c79be5e, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-24T10:24:48Z

## Results

- `dev test` gains `--project` / `-p` (repeatable, comma-separated), `--filter-class`, and
  `--filter-method` (`tools/dev-cli/endpoints/test-command.cs`).
- Name matching is the new pure service `TestProjectSelection`
  (`tools/dev-cli/services/test-project-selection.cs`): exact stem (case-insensitive, `.csproj`
  suffix allowed) wins, else one unique substring of the stem or repo-relative path. No match fails
  listing every test project; several matches fail listing the candidates. `--project` applies
  after `--group`.
- `DotNetTestArguments.PlatformArguments` appends `--filter-class` / `--filter-method` after the
  coverage flags. `DotNetTestArguments.BuildPlan` decides the serial build phase: a `--project` run
  without `--no-build` builds each selected project (Release, cwd = project dir) instead of the
  whole solution; every `dotnet test` stays `--no-build --no-restore` with its own port base.
  Without the new options the plan is the old one (solution unless `--no-build`, then
  out-of-solution projects).
- A filter that selects no test in a project fails that project (MTP exit code 8, "Zero tests
  ran"): checked by hand with `--filter-class NoSuchClassAnywhere`. With several projects and a
  filter, `dev test` prints a yellow note up front to narrow with `--project`.
- `AGENTS.md` "Build, run, test" row shows the targeted-run example.
  `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` does not mention targeted
  runs, so it is unchanged.
- Audit boy-scout: `vscode-window-icon` (advisory) failed on a missing `.vscode/settings.json`;
  `ganda repo audit --fix` wrote it (gitignored, nothing to commit) and the rerun passes all checks.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (dev-cli changed): installed `./bin/dev`.
- `./bin/dev build`: Build succeeded, 0 warnings, 0 errors.
- `tests/tools/dev-cli-tests/test-project-selection-tests.cs` (new) and
  `tests/tools/dev-cli-tests/dotnet-test-arguments-tests.cs` (edited) are compile items of
  `dev-cli-tests.csproj`, not standalone runfiles (no `#if !JARIBU_MULTI` guard), so they ran as
  that project: `./bin/dev test --project dev-cli-tests` → passed, total 385, failed 0.
- `ganda repo audit`: passes (1 advisory `vscode-window-icon`); after `ganda repo audit --fix`, rerun
  `ganda repo audit` → "Repository passes all audit checks."

### How to validate

Smoke:

```bash
dotnet run --file tools/dev-cli/dev.cs -- self-install
./bin/dev test --project dev-cli-tests --filter-class TestProjectSelection_
./bin/dev test -p tools/dev-cli --no-build --filter-class BuildPlan_Given_ --filter-method SelectedRun
./bin/dev test -p no-such-tests --no-build
./bin/dev test -p web- --no-build
```

Expect:

- First run: "Test projects: dev-cli-tests", "Building dev-cli-tests (selected, Release)...", no
  solution build, MTP summary `total: 7`, `failed: 0`, exit 0.
- Second run: path substring selects dev-cli-tests, `total: 2`, exit 0.
- Third run: exit 1, "--project 'no-such-tests' matches no test project. Test projects: …".
- Fourth run: exit 1, "--project 'web-' matches several test projects: web-contracts-tests, …".

### Review

- Round 1, effort 2, roster general. Disposition: clean. The implementer restatement matches the original task; no misread.
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0/0/0; nit 0/0/0. Final open count: 0.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Source: Task 514 (`report/analysis.md`, §6 `/tw-correct` Hand-off A).
- Verified `dev test` is repo-local (`tools/dev-cli/endpoints/test-command.cs`), not a shared `TimeWarp.Nuru.DevCli` command (`clean`, `self-install`, `check-version` are shared).
- Pairs with Task 515 (which documents combined MTP filter syntax in repo testing docs).
