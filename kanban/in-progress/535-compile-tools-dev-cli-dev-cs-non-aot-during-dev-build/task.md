# Compile tools/dev-cli/dev.cs (non-AOT) during dev build

## Description

From the `/tw-walk-retro` on task 533 (PR #714): `BuildCommand` (`tools/dev-cli/endpoints/build-command.cs`) builds `timewarp-architecture.slnx` and the unlisted `tests/` Jaribu aggregators (added in task 526), but does not compile `tools/dev-cli/dev.cs`.

Because `tools/dev-cli/dev.cs` is a .NET 11 file-based app (not in `timewarp-architecture.slnx`), compile errors in `tools/dev-cli/` pass `./bin/dev build` silently. During task 533, the implementer had to run full NativeAOT `./bin/dev self-install` (~70s each) repeatedly just to check whether edits to `tools/dev-cli/` compiled, spending ~2m50s on compile-check cycles.

Add a non-AOT compile step for `tools/dev-cli/dev.cs` (for example `dotnet build tools/dev-cli/dev.cs -c Release -p:PublishAot=false`, or equivalent non-AOT build flag verified against `tools/dev-cli/dev.cs` and `tools/dev-cli/Directory.Build.props`) to `BuildCommand` so `./bin/dev build` catches `tools/dev-cli/` compile errors and warnings quickly without paying the NativeAOT link cost.

## Goal (implementer restatement)

`./bin/dev build` must also compile the dev CLI runfile `tools/dev-cli/dev.cs` (a plain `dotnet build`,
no NativeAOT link), after the solution and the unlisted `tests/` projects, so a compile error or
warning in `tools/dev-cli/` fails `dev build` in seconds instead of only at `dev self-install`.

Acceptance:
- `BuildCommand` runs `dotnet build tools/dev-cli/dev.cs -c Release` as its last step; `--clean`
  cleans it too. Output follows the same capture (default) / stream (`--verbose`) rules.
- The build writes to the runfile artifacts cache, never to `./bin/dev`.
- Warnings-as-errors and the repo analyzers apply (a broken file under `tools/dev-cli/` fails the step).
- Works in the template repository and in a generated app (`dev template-smoke` runs `dev build`
  there through the runfile).
- `dev-cli-tests` pin the runfile path and step text; `BuildCommand` Design region updated.

## Requirements

- [x] Extend `BuildCommand` (`tools/dev-cli/endpoints/build-command.cs`) to compile `tools/dev-cli/dev.cs` without NativeAOT after the solution and unlisted test projects build (and clean it under `--clean` if appropriate).
- [x] Verify the non-AOT compile runs in both the template repository and a generated app (`tools/dev-cli/dev.cs` ships to generated apps with `#if templateRepository` regions stripped, so both modes must compile cleanly with 0 warnings).
- [x] Ensure warnings-as-errors and analyzers configured for `tools/dev-cli/` still apply, and `BuildOutput` capture/stream modes (`--quiet` default vs `--verbose`) handle the new step consistently.
- [x] Check that building `tools/dev-cli/dev.cs` does not lock or overwrite the running `./bin/dev` binary (use non-AOT build into `obj/`/`bin/`, not `publish` to `./bin/dev`).
- [x] Add or update `dev-cli-tests` covering the new build step and verify `./bin/dev build` fails fast when `tools/dev-cli/` has a compile error.
- [x] Run `./bin/dev self-install`, `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `./bin/dev template-smoke`.

## Checklist

- [x] `BuildCommand` compiles `tools/dev-cli/dev.cs` (non-AOT) with 0 warnings/errors
- [x] Works while invoked via `./bin/dev build` (no file-lock conflict with `./bin/dev`)
- [x] Works in both template repo and generated app (`dev template-smoke` passes)
- [x] `dev-cli-tests` updated and passing
- [x] `BuildCommand` Design region updated

## Notes

- Origin: `/tw-walk-retro` on task 533 (PR #714), Finding 2. Task 526 closed the same blind spot for unlisted `tests/` Jaribu aggregators; `tools/dev-cli/dev.cs` is the remaining uncompiled entry point.
- Keep NativeAOT `dev self-install` separate for when an updated `./bin/dev` executable is actually needed.

## Session

- Filed from `/tw-walk-retro` on task 533 (2026-11-27).
- 2026-11-27 implement: `BuildCommand` builds then cleans `tools/dev-cli/dev.cs` as its last step
  (`dotnet build <runfile> -c Release`; no `-p:PublishAot=false`, because a plain build never runs the
  NativeAOT link and keeping PublishAot on keeps the AOT/trim analyzers). The runfile path and step
  text live in `BuildOutput` (pure, test-included); `TemplateSmokeDevCli.DevCliRunfile` now reads it.
  Measured: the new step takes ~5s incremental and ~24s cold. Output goes to
  `~/.local/share/dotnet/runfile/dev-<hash>/bin/release/`, never `./bin/dev`.

## Results

- `tools/dev-cli/endpoints/build-command.cs`: last build step `dotnet build tools/dev-cli/dev.cs -c Release`;
  `--clean` runs `dotnet clean tools/dev-cli/dev.cs -c Release` after the solution and unlisted projects.
  Both use the same `ExecuteAsync`, so capture (default) / stream (`--verbose`) behave as for the other steps.
  Design region updated.
- `tools/dev-cli/services/build-output.cs`: `DevCliRunfile` and `DevCliStepText` constants.
- `tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs`: `DevCliRunfile` reads `BuildOutput.DevCliRunfile`.
- `tests/tools/dev-cli-tests/build-output-tests.cs`: `DevCliRunfile_Given_` (path names the real entry point;
  step text names the runfile and says no NativeAOT link).

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (needed after the dev-cli edit).
- `./bin/dev build`: passed, 0 warnings, 76.1s; the new step line printed last. `./bin/dev` inode and mtime unchanged after the build.
- Fail-fast probe: a temporary `tools/dev-cli/services/zz-broken.cs` with CS0029 made `./bin/dev build` exit 1 with
  `error CS0029` and `Build failed: tools/dev-cli/dev.cs` (file removed after). A direct `dotnet build` of the runfile
  also failed on CS0219 (warning as error) and TWA0004, so analyzers and warnings-as-errors apply.
- `./bin/dev build --clean`: passed, 127.2s; `Cleaning tools/dev-cli/dev.cs (dev CLI runfile)...` printed.
- `tests/tools/dev-cli-tests/build-output-tests.cs`: not a standalone runfile (no `#if !JARIBU_MULTI`; part of the
  `dev-cli-tests` csproj). Ran `./bin/dev test --project dev-cli-tests` as the task requires: 469/469 passed.
- `./bin/dev template-smoke`: SUCCEEDED (6m17s). The generated app's `dev build` (run through the runfile, which
  now also builds that same runfile) passed.
- `ganda repo audit`: first run failed only on `kanban-landed` (task 534's live walk kitchen not landed on
  origin-home; not this task). `ganda repo audit --fix` landed 534, its live review committed again, and a second
  `ganda repo audit --fix` passed: 44 passed, 0 failed, 0 warnings.

### How to validate

- Smoke: `./bin/dev build`, then add a file under `tools/dev-cli/services/` with a compile error and run `./bin/dev build` again.
- Expect: the first run prints `Building tools/dev-cli/dev.cs (dev CLI runfile, Release, no NativeAOT link)...` and
  `Build completed successfully!`; the second exits 1 with the compiler error and `Build failed: tools/dev-cli/dev.cs`.
  `./bin/dev` is not rewritten by either run.
