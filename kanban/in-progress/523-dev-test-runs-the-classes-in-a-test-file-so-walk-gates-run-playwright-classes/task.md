# dev test runs the classes in a test file so walk gates run Playwright classes

## Description

Task 501 took `ci (playwright)` off pull requests. Task 502 (`kanban/done/502-…/report/analysis.md`)
kept the suite and left it to the nightly `template-smoke --matrix --app-tests`.

That opens a gap when a change adds or edits a Playwright class. Every real bug the suite
found (tasks 351, 371, 295) came from running the new class on its own pull request. The
classes in `tests/container-apps/web/web-spa-playwright-tests/` are not standalone runfiles
(no `#if !JARIBU_MULTI` guard), so `dotnet run --file` cannot run one. `dev test` takes no
file arguments. The ganda walk gate says "`./bin/dev test <file…>` is the same run when this
repository's dev CLI accepts file arguments". Today it falls back to "not a standalone
runfile; CI runs it", and since task 501 no pull-request job runs it.

## Requirements

- `dev test` accepts one or more test file paths as arguments.
  - A file inside a csproj test project (any project under `tests/`, not only Playwright)
    runs that project. The run is filtered to the test classes declared in the file, using
    the same Jaribu MTP filter `--filter-class` uses. A file that declares several classes
    runs all of them.
  - A file with the `#if !JARIBU_MULTI` runfile guard runs as `dotnet run --file <file>`.
  - A file that is in no test project and is not a runfile fails with a message that names
    the file.
  - A file with no test class (a helper such as `drawer-page-asserts.cs`) fails with a
    message that names the file and says to pass the test class file.
- File arguments work alongside `--no-build` and `--quiet`. Combining them with `--group`,
  `--project`, or `--filter-class` is refused with a clear message.
- Update the comment in `tools/dev-cli/services/test-groups.cs` that says the playwright
  group is "local only". It runs nightly through `template-smoke --matrix --app-tests`, and
  it does not run on pull requests (task 502). Make the same correction in
  `tools/dev-cli/endpoints/workflow-command.cs`.
- AGENTS.md "Where to look" Tests row: one sentence saying that `./bin/dev test <file>` runs
  a Playwright test file, and that CI runs the suite only nightly.

## Goal (implementer restatement)

A walk gate must be able to run one Playwright (or any csproj) test file on its own pull
request, because CI no longer runs Playwright there. So `dev test` takes test file paths:

- A file that carries the `#if !JARIBU_MULTI` guard is a runfile: `dotnet run --file <file>`.
- Otherwise the file must sit under a test project in `tests/` (the nearest csproj above it).
  The classes it declares that the project registers (`RegisterTests<T>()`) are its test
  classes. Each runs through the existing build-then-`dotnet test` path with
  `--filter-class <Namespace.Class>` (Jaribu's MTP option takes one value, so one run per class).
- A file in no project and not a runfile, and a file with no test class, fail naming the file;
  the second says to pass the test class file.
- Files combine with `--no-build` / `--quiet`; with `--group`, `--project`, or
  `--filter-class` the run is refused.
- Comments in `test-groups.cs` and `workflow-command.cs` say playwright runs nightly through
  `template-smoke --matrix --app-tests`, not on pull requests (task 502). AGENTS.md Tests row
  gets one sentence.

Fix pass for review round 1 M1: Jaribu's `--filter-class` is a contains match, so `Ns.Equality`
also selects `Ns.Equality_Operators` and the shorter class cannot run alone. `dev test <file>`
must refuse that file before starting `dotnet test`, with an error naming the file, the class,
and the other class; a mapping test pins the `Equality` / `Equality_Operators` pair.

Done when: the dark-theme file runs only `DarkTheme_Given_Shell` and passes; a helper file and
a stray file fail naming the file; a co-located runfile runs through `dotnet run --file`; pure
mapping tests sit in `tests/tools/dev-cli-tests/`; `dev build` and `ganda repo audit` are clean.

## Acceptance criteria

- [x] `./bin/dev test tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs` runs only `DarkTheme_Given_Shell` and passes.
- [x] A helper file, and a file that is in no test project and is not a runfile, each fail with a message that names the file.
- [x] A co-located runfile (one with the `JARIBU_MULTI` guard) runs through `dotnet run --file`.
- [x] Tests for the file → (project, classes) mapping in `tests/tools/dev-cli-tests/` (beside `test-project-selection-tests.cs`).
- [x] `dev build` clean; `ganda repo audit` clean.

## Checklist

- [x] File argument parsing and mapping
- [x] Runfile path
- [x] Comment and AGENTS.md updates
- [x] Tests
- [x] Gates

## Review

- Round 1: Blocked — open M1
- Fix pass: M1 fixed. A class whose `--filter-class` value is an ordinal ignore-case
  substring of another registered class's FullName in the same project now fails the file before
  any run, naming the file, the class, and the other class.
- Review round 2: clean. M1 re-verified fixed. No new findings. Disposition `review/disposition.md`.
- Round 2: Done
## Notes

- Origin: task 502, Recommendation 2.
- Path-gated or sharded Playwright on pull requests (task 502 option 3) is not in scope.

## Results

`dev test` takes test file arguments (task 523).

- `tools/dev-cli/services/test-file-selection.cs` (new, pure): maps each file. A line that is
  exactly the `JARIBU_MULTI` guard makes it a runfile. Otherwise the owning project is the
  deepest test project under `tests/` whose folder holds the file, and its test classes are the
  classes it declares that a `RegisterTests<T>()` call in that project registers (so a partial
  class part without its own registration still maps). Each class becomes one
  `--filter-class Namespace.Class` value (`+Class` when nested). Raw string literals are removed
  before the scan so fixture text never adds a class. Missing file, stray file, and helper file
  each return an error naming the file; the helper error says to pass the test class file.
- `tools/dev-cli/endpoints/test-command.cs`: catch-all `{*files}` parameter. With files, `--group`,
  `--project`, and `--filter-class` are refused; `--filter-method` is refused when a runfile is
  passed (it takes no MTP options). The build phase is the `--project` plan for the owning
  projects (`--no-build` keeps it to out-of-solution projects). Then, one at a time on the
  resolved port base, one `dotnet test --filter-class` per class, and `dotnet run --file` per
  runfile (cwd = repo root). `--quiet` and the summary table work as before. Port-base resolution
  and the pass/fail tail are shared helpers now (`ResolvePortBase`, `Finish`).
- Jaribu's MTP `--filter-class` takes exactly one value (ArgumentArity.ExactlyOne, checked in
  TimeWarp.Jaribu.TestingPlatform 1.0.0-beta.18), so a file with several classes runs its project
  once per class.
- Comments: `test-groups.cs` and `workflow-command.cs` now say playwright runs nightly through
  `template-smoke --matrix --app-tests` and not on pull requests (tasks 501, 502).
- AGENTS.md Tests row: one sentence on `./bin/dev test <file>` and nightly-only Playwright CI.
- Tests: `tests/tools/dev-cli-tests/test-file-selection-tests.cs` (11 tests; the nested-project
  test caught a bug where the deepest project was picked by csproj path length).
- Audit boy-scout: `ganda repo audit --fix` fixed the `vscode-window-icon` advisory by writing the
  gitignored `.vscode/settings.json` in this worktree (nothing to commit).

### Gates

- `./bin/dev build` → succeeded, 0 warnings, 0 errors (after `dev self-install` for the new verb).
- `tests/tools/dev-cli-tests/test-file-selection-tests.cs` → not a standalone runfile (no
  `JARIBU_MULTI` guard); run with `./bin/dev test tests/tools/dev-cli-tests/test-file-selection-tests.cs tests/tools/dev-cli-tests/test-project-selection-tests.cs --no-build`
  → `TestFileSelection_.Select_Given_` 11/11 passed, `TestProjectSelection_.Select_Given_` 7/7 passed.
- `./bin/dev test tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs --quiet`
  → built web-spa-playwright-tests, ran `DarkThemePlaywright_.DarkTheme_Given_Shell` only, passed (84 s);
  rerun with `--no-build` reported total 4, succeeded 4 (the class's four methods).
- `./bin/dev test source/container-apps/web/features/admin/roles/delete-role/delete-role-validator-tests.cs`
  → ran through `dotnet run --file` (group `runfile`), 2/2 passed.
- `./bin/dev test tests/container-apps/web/web-spa-playwright-tests/drawer-page-asserts.cs tools/dev-cli/dev.cs`
  → exit 1, one error per file, each naming the file (helper: "declares no test class … pass the
  test class file instead"; stray: "is in no test project under tests/ and is not a Jaribu runfile").
- `./bin/dev test <file> --group fast` → exit 1, "Test file arguments choose the project and the classes; drop --group."
- `ganda repo audit` → passes (one advisory `vscode-window-icon`); `ganda repo audit --fix` →
  fixed it, "Repository passes all audit checks."

### Fix pass (review round 1 M1)

- `test-file-selection.cs`: `TestClasses` returns each registered class's FullName (nesting from
  indentation, joined with `+`) and its filter. Per project, `ProjectTestClasses` holds the
  registered names and every registered class's FullName. A file whose class filter is an ordinal
  ignore-case substring of another FullName in that project gets one error per collision, and the
  file adds no run. `ClassFilters` was folded into `TestClasses`.
- `test-file-selection-tests.cs`: `ClassWhoseFilterIsInAnotherClassName_Should_FailNamingFileClassAndOtherClass`
  (same project and namespace, `Equality` and `Equality_Operators`) and
  `LongerClassName_Should_StillRunAlone`.
- Gates:
  - `dotnet run --file tools/dev-cli/dev.cs -- self-install` then `./bin/dev build` → succeeded, 0 warnings, 0 errors.
  - `tests/tools/dev-cli-tests/test-file-selection-tests.cs` (not a standalone runfile) →
    `./bin/dev test tests/tools/dev-cli-tests/test-file-selection-tests.cs` → 13/13 passed.
  - `./bin/dev test tests/foundation/foundation-domain-tests/entity-tests.cs --no-build` → refused
    before any run: "…entity-tests.cs: test class TimeWarp.Architecture.Foundation.Domain.Tests.Equality
    cannot run alone … also selects TimeWarp.Architecture.Foundation.Domain.Tests.Equality_Operators in
    foundation-domain-tests."
  - `./bin/dev test tests/foundation/foundation-domain-tests/enumeration-tests.cs --no-build --quiet` → 14 of 14 class runs passed.
  - `ganda repo audit` → "Repository passes all audit checks."

### How to validate

Smoke: refresh the AOT binary (`dotnet run --file tools/dev-cli/dev.cs -- self-install`), then run
`./bin/dev test tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs`,
then `./bin/dev test tests/container-apps/web/web-spa-playwright-tests/drawer-page-asserts.cs`.

Collision smoke: `./bin/dev test tests/foundation/foundation-domain-tests/entity-tests.cs` exits 1
before any run, naming `entity-tests.cs`, class `…Domain.Tests.Equality`, and `…Domain.Tests.Equality_Operators`.

Expect: the first builds web-spa-playwright-tests, runs one row
`web-spa-playwright-tests [DarkThemePlaywright_.DarkTheme_Given_Shell]` (4 tests) and passes;
the second exits 1 with "…/drawer-page-asserts.cs declares no test class … pass the test class
file instead."

### Review disposition

- Rounds: 2. Roster: general. Effort: 2.
- Counts: bug 1 fixed, 0 open; suggestion 0; nit 0.
- Disposition: clean (`review/disposition.md`). Round 1 M1 is fixed. Round 2 found no new issues.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Session

- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session da2cde51-352e-4f81-9fba-e3223c82937e, max-turns 500, Done — 2026-11-25T19:07:20Z
- Review oracle: Grok, session 01a2141b-1557-72b3-8bda-73ac5306b0b0 (2026-11-26). Effort 2, roster general. Round 1 open: M1. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2141b-1557-72b3-8bda-73ac5306b0b0, max-turns 120, artifacts general.md, merged.md — 2026-11-25T19:20:15Z
- Review round 2: Grok 4.7, session 01a2142c-f7f0-76c2-b949-ab6cbc99068b (2026-11-26). Effort 2, roster general. M1 re-verified fixed. No new findings. Disposition: clean. Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.
