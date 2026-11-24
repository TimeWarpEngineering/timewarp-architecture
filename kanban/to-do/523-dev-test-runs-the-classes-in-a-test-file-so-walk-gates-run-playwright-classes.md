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

## Acceptance criteria

- [ ] `./bin/dev test tests/container-apps/web/web-spa-playwright-tests/dark-theme-playwright-tests.cs` runs only `DarkTheme_Given_Shell` and passes.
- [ ] A helper file, and a file that is in no test project and is not a runfile, each fail with a message that names the file.
- [ ] A co-located runfile (one with the `JARIBU_MULTI` guard) runs through `dotnet run --file`.
- [ ] Tests for the file → (project, classes) mapping in `tests/tools/dev-cli-tests/` (beside `test-project-selection-tests.cs`).
- [ ] `dev build` clean; `ganda repo audit` clean.

## Checklist

- [ ] File argument parsing and mapping
- [ ] Runfile path
- [ ] Comment and AGENTS.md updates
- [ ] Tests
- [ ] Gates

## Notes

- Origin: task 502, Recommendation 2.
- Path-gated or sharded Playwright on pull requests (task 502 option 3) is not in scope.
