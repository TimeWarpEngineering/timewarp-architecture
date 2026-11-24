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

## Checklist

- [ ] Add `--project` (`-p`), `--filter-class`, and `--filter-method` options to `tools/dev-cli/endpoints/test-command.cs`
- [ ] Forward filter arguments cleanly to MTP and preserve serial build + per-project `cwd` + port isolation
- [ ] Add unit tests for project selection and MTP filter argument forwarding
- [ ] Add one-line usage example to `AGENTS.md` (and testing reference if applicable)
- [ ] Run `self-install`, `./bin/dev build`, targeted `./bin/dev test`, and `ganda repo audit`

## Session

- Created: 2875948 (2026-11-24)

## Notes

- Source: Task 514 (`report/analysis.md`, §6 `/tw-correct` Hand-off A).
- Verified `dev test` is repo-local (`tools/dev-cli/endpoints/test-command.cs`), not a shared `TimeWarp.Nuru.DevCli` command (`clean`, `self-install`, `check-version` are shared).
- Pairs with Task 515 (which documents combined MTP filter syntax in repo testing docs).
