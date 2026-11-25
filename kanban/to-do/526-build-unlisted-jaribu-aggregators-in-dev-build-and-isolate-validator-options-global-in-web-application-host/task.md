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

## Checklist

- [ ] Make `dev build` build unlisted test projects (Jaribu aggregators + `timewarp-testing-tests`) after `.slnx`, using dynamic discovery compatible with template flag-off variants
- [ ] Add/update unit tests in `tests/tools/dev-cli-tests/`
- [ ] Save and restore `ValidatorOptions.Global.PropertyNameResolver` and `DisplayNameResolver` (ref-counted under a lock) in `WebApplicationHost<TProgram>` (including startup-failure cleanup), and audit co-located runfiles for raw `UseFastEndpoints` leaks
- [ ] Add a regression test in `tests/common/timewarp-testing-tests/` proving `ValidatorOptions.Global` is restored after host disposal
- [ ] Verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, `./bin/dev test --project timewarp-testing-tests`, `./bin/dev test --project web-jaribu-tests`, `./bin/dev template-smoke`, and `ganda repo audit`

## Session

- Created: 1191026 (2026-11-25)

## Notes

- `/tw-correct` origin: Steven 2026-11-25 after tasks 520 (PR #692) and 521 (PR #693) both failed CI `web-jaribu-tests` after passing local `./bin/dev build` + standalone runfile runs.
- Prior workaround evidence: `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs:172-173`.
- Remember `tw-dev-cli`: after editing `tools/dev-cli/`, self-install `./bin/dev` before verifying with `./bin/dev build`.
