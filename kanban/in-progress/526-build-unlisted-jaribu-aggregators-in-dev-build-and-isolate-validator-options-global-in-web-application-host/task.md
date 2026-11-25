# Build unlisted Jaribu aggregators in dev build and isolate ValidatorOptions.Global in WebApplicationHost

## Description

Two consecutive feedback tasks (520 / PR #692 and 521 / PR #693) passed every local gate the implementer ran (`./bin/dev build` + standalone `dotnet run --file ...`) and then failed `ci (integration)` and `template-smoke` inside `web-jaribu-tests`:

1. **Unlisted Jaribu aggregators are not built by `./bin/dev build`:** `tools/dev-cli/endpoints/build-command.cs` only builds `timewarp-architecture.slnx`, while `tests/container-apps/web/web-jaribu-tests`, `tests/container-apps/api/api-jaribu-tests`, and `tests/common/timewarp-testing-tests` are intentionally excluded from `.slnx` (`tools/dev-cli/services/dotnet-test-arguments.cs`). In task 520, a compile error (`CS1503`) in `feedback-owner-name-tests.cs` passed `./bin/dev build` locally and only broke in CI.
2. **In-proc FastEndpoints test hosts mutate static `ValidatorOptions.Global` across sibling tests:** `WebApplicationHost<TProgram>` boots FastEndpoints + `CommonServerModule`, which overwrite FluentValidation's static `ValidatorOptions.Global.PropertyNameResolver` (to camelCase) and `ValidatorOptions.Global.DisplayNameResolver` (to `Type:Member`). Any subsequent pure validator test running in the same `web-jaribu-tests` process sees camelCase `PropertyName` (`"pagePath"` instead of `"PagePath"`), even though the same test passes standalone. Evidence of two occurrences: `update-profile-tests.cs:172-173` (workaround comment) and task 521 / PR #693 (`feedback-owner-name-tests.cs:360`).

This is a `/tw-correct` structural fix so both failure classes are caught or eliminated locally before CI.

## Scope

1. **Build unlisted Jaribu aggregator projects in `dev build` (`tools/dev-cli/`):**
   - In `tools/dev-cli/endpoints/build-command.cs` (reusing the existing discovery/solution-diff helpers from `tools/dev-cli/services/dotnet-test-arguments.cs` and `test-command.cs` — do not hardcode project lists), after building `timewarp-architecture.slnx`, also build every test project under `tests/` that is not listed in `.slnx` (the Jaribu family aggregators and `timewarp-testing-tests`), one at a time (matching `dev test`'s build phase so shared `obj/`/`bin/` folders do not race).
   - Keep `timewarp-architecture.slnx` unchanged (aggregators stay out of `.slnx` by design; `dotnet-test-arguments-tests.cs` pins that).
   - If `--clean` is passed to `dev build`, clean those unlisted projects as well (or keep clean behavior consistent and tested).
   - Verify `dev build` remains 0 warnings / 0 errors and works in generated template apps where some aggregators are excluded by template flags (e.g., `--api false` / `--web false` — discovery must handle missing families cleanly).
   - Update unit tests in `tests/tools/dev-cli-tests/` covering the unlisted-project build plan for `dev build`.
2. **Isolate `ValidatorOptions.Global` in `WebApplicationHost<TProgram>` (`tests/common/timewarp-testing/web-application-host.cs`):**
   - Snapshot `ValidatorOptions.Global.PropertyNameResolver` and `ValidatorOptions.Global.DisplayNameResolver` under a static lock when the active host count transitions `0 → 1` (before `TProgram.ConfigureServices` / `ConfigureMiddleware` / `ConfigureEndpoints`), and restore both when the active host count drops back to `0` (in `DisposeAsyncCore` AND in the constructor `catch` cleanup path on startup failure).
   - Check whether any other co-located test file under `source/container-apps/{web,api}/` boots a raw `WebApplication` + `UseFastEndpoints` directly without `WebApplicationHost`; if any does, isolate it or route it through the same guard so `web-jaribu-tests` and `api-jaribu-tests` never leak `ValidatorOptions.Global` state between test classes.
   - Add a regression test in `tests/common/timewarp-testing-tests/` proving that after starting and disposing a `WebApplicationHost`, `ValidatorOptions.Global.PropertyNameResolver` and `DisplayNameResolver` return to their pre-host values (so a subsequent `new Validator().Validate(...)` sees default PascalCase `PropertyName`).
3. **Update `#region Design`** in `build-command.cs` and `web-application-host.cs` (and check if the workaround comment in `update-profile-tests.cs:172` can be cleaned up or updated once isolation holds).

## Goal (implementer restatement)

Catch two CI-only failure classes locally.

1. `dev build` builds the `.slnx` and then every `tests/**/*.csproj` the `.slnx` does not list
   (today: `web-jaribu-tests`, `api-jaribu-tests`, `timewarp-testing-tests`), one at a time, found
   by the same tests/ glob and `.slnx` diff `dev test` uses. No hardcoded list, so a generated app
   with a family flagged off just has fewer of them. `--clean` cleans them too. `.slnx` is unchanged.
2. `WebApplicationHost<TProgram>` snapshots FluentValidation's `ValidatorOptions.Global`
   `PropertyNameResolver` and `DisplayNameResolver` when the first live host starts (count 0 → 1,
   under a lock, before `TProgram.Configure*`) and restores both when the last live host goes away
   (dispose, or the constructor's startup-failure path). A pure validator test that runs after a
   host test in the same aggregator process then sees PascalCase `PropertyName` again.

Acceptance:

- Unit tests in `tests/tools/dev-cli-tests/` pin the unlisted-project list (`dev build` plan).
- A regression test in `tests/common/timewarp-testing-tests/` starts and disposes a real host and
  proves both resolvers are back to their pre-host values (validator `PropertyName` is PascalCase).
- Co-located test files under `source/container-apps/{web,api}/` that boot raw `UseFastEndpoints`
  are audited; Design regions updated; the `update-profile-tests.cs` workaround comment revisited.
- `./bin/dev build` 0 warnings / 0 errors, the changed test files pass, `ganda repo audit` clean.

## Checklist

- [x] Make `dev build` build unlisted test projects (Jaribu aggregators + `timewarp-testing-tests`) after `.slnx`, using dynamic discovery compatible with template flag-off variants
- [x] Add/update unit tests in `tests/tools/dev-cli-tests/`
- [x] Save and restore `ValidatorOptions.Global.PropertyNameResolver` and `DisplayNameResolver` (ref-counted under a lock) in `WebApplicationHost<TProgram>` (including startup-failure cleanup), and audit co-located runfiles for raw `UseFastEndpoints` leaks
- [x] Add a regression test in `tests/common/timewarp-testing-tests/` proving `ValidatorOptions.Global` is restored after host disposal
- [x] Verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev test --project timewarp-testing-tests`, `./bin/dev test --project web-jaribu-tests`, `./bin/dev template-smoke`, and `ganda repo audit`

## Session

- Created: 1191026 (2026-11-25)
- 2026-11-25 implement (ganda task work): dev build builds the unlisted test projects; ValidatorOptionsGuard
  in timewarp-testing; regression tests; update-profile workaround tightened to an exact PascalCase match.
  Audit: the first run failed `analyzer-suppressions` because the new runfile copied a neighbour's
  `#:property NoWarn=` line. Removed it; the file compiles clean without it (runfile and JARIBU_MULTI).

## Results

### Changes

- `tools/dev-cli/services/dotnet-test-arguments.cs`: `TestProjects` (tests/ glob, outside obj/ and
  bin/, empty when tests/ is missing) and `UnlistedProjects` (glob minus `.slnx`). `dev test` now uses
  `TestProjects` instead of its own inline glob.
- `tools/dev-cli/endpoints/build-command.cs`: after the `.slnx` build, builds each unlisted project
  one at a time (Release, cwd = project folder, the same shape as `dev test`'s build phase). `--clean`
  cleans them after the solution. Design region updated. `.slnx` unchanged. `dev workflow` (CI) calls
  this handler, so CI's build step now compiles the aggregators too.
- `tests/common/timewarp-testing/validator-options-guard.cs` (new): non-generic, lock-guarded,
  ref-counted snapshot/restore of `ValidatorOptions.Global.PropertyNameResolver` and
  `DisplayNameResolver`. It is non-generic because Web and Api hosts are different closed generic
  types and a HostGraph keeps several alive at once.
- `tests/common/timewarp-testing/web-application-host.cs`: `Enter` before `CreateBuilder`. `Exit` in
  `DisposeAsyncCore` (in a finally, made idempotent with a `Disposed` flag) and in a new outer catch,
  so a failure anywhere in configure, build, or start restores both resolvers. Design region updated.
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`: `RejectsField`
  is now an exact, ordinal PascalCase `PropertyName` match. The camelCase / ErrorMessage fallback is
  gone, so a regression in the guard shows up in web-jaribu-tests.
- Raw `UseFastEndpoints` audit: no co-located test file under `source/container-apps/{web,api}/`
  boots a raw host (only the two `program.cs` files call it). The only raw hosts are in
  `tests/container-apps/web/web-server-integration-tests/` (open-api-*, identity fixtures). That is a
  separate process from the Jaribu aggregators and has no PascalCase `PropertyName` assertions, so it
  was left alone. `ValidatorOptionsGuard` is public, so such a file can call `Enter`/`Exit` if one ever needs it.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed).
- `./bin/dev build`: passed, 0 warnings / 0 errors. The log shows the new steps: timewarp-testing-tests,
  api-jaribu-tests, web-jaribu-tests (not in the solution, Release).
- `tests/tools/dev-cli-tests/dotnet-test-arguments-tests.cs` (not a standalone runfile, no
  `#if !JARIBU_MULTI` guard): `./bin/dev test --project dev-cli-tests --filter-class DotNetTestArguments_`, 15/15 passed (5 new `UnlistedProjects_Given_`).
- `dotnet run --file tests/common/timewarp-testing-tests/validator-options-guard-tests.cs`: 4/4 passed (rerun after the NoWarn removal, 4/4).
- `dotnet run --file source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`: 16/16 passed.
- `./bin/dev test --project timewarp-testing-tests`: passed. `--project web-jaribu-tests`: passed. `--project api-jaribu-tests`: passed.
- `./bin/dev template-smoke` (default variant): SUCCEEDED. Generated web-jaribu-tests 496/496, api-jaribu-tests 9/9, timewarp-testing-tests 18/18.
- `ganda repo audit`: first run failed `analyzer-suppressions` (copied NoWarn, fixed above). Rerun passes;
  the one advisory is `vscode-window-icon` (warning, non-blocking; `.vscode/settings.json` is git-ignored).

### How to validate

- Smoke: `./bin/dev build`, then `dotnet run --file tests/common/timewarp-testing-tests/validator-options-guard-tests.cs`.
- Expect: the build log lists `Building timewarp-testing-tests / api-jaribu-tests / web-jaribu-tests (not in
  the solution, Release)...` after the `.slnx` and ends green. A compile error put into any co-located
  runfile under `source/container-apps/web/features/` now fails `./bin/dev build`. The runfile reports
  4/4 passed: the validator reports `pagePath` while an Api.Server host is alive and `PagePath` again
  after it is disposed or fails to start.

## Notes

- `/tw-correct` origin: Steven 2026-11-25 after tasks 520 (PR #692) and 521 (PR #693) both failed CI `web-jaribu-tests` after passing local `./bin/dev build` + standalone runfile runs.
- Prior workaround evidence: `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs:172-173`.
- Remember `tw-dev-cli`: after editing `tools/dev-cli/`, self-install `./bin/dev` before verifying with `./bin/dev build`.
