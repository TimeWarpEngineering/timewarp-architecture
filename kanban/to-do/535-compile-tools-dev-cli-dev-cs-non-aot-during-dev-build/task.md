# Compile tools/dev-cli/dev.cs (non-AOT) during dev build

## Description

From the `/tw-walk-retro` on task 533 (PR #714): `BuildCommand` (`tools/dev-cli/endpoints/build-command.cs`) builds `timewarp-architecture.slnx` and the unlisted `tests/` Jaribu aggregators (added in task 526), but does not compile `tools/dev-cli/dev.cs`.

Because `tools/dev-cli/dev.cs` is a .NET 11 file-based app (not in `timewarp-architecture.slnx`), compile errors in `tools/dev-cli/` pass `./bin/dev build` silently. During task 533, the implementer had to run full NativeAOT `./bin/dev self-install` (~70s each) repeatedly just to check whether edits to `tools/dev-cli/` compiled, spending ~2m50s on compile-check cycles.

Add a non-AOT compile step for `tools/dev-cli/dev.cs` (for example `dotnet build tools/dev-cli/dev.cs -c Release -p:PublishAot=false`, or equivalent non-AOT build flag verified against `tools/dev-cli/dev.cs` and `tools/dev-cli/Directory.Build.props`) to `BuildCommand` so `./bin/dev build` catches `tools/dev-cli/` compile errors and warnings quickly without paying the NativeAOT link cost.

## Requirements

- [ ] Extend `BuildCommand` (`tools/dev-cli/endpoints/build-command.cs`) to compile `tools/dev-cli/dev.cs` without NativeAOT after the solution and unlisted test projects build (and clean it under `--clean` if appropriate).
- [ ] Verify the non-AOT compile runs in both the template repository and a generated app (`tools/dev-cli/dev.cs` ships to generated apps with `#if templateRepository` regions stripped, so both modes must compile cleanly with 0 warnings).
- [ ] Ensure warnings-as-errors and analyzers configured for `tools/dev-cli/` still apply, and `BuildOutput` capture/stream modes (`--quiet` default vs `--verbose`) handle the new step consistently.
- [ ] Check that building `tools/dev-cli/dev.cs` does not lock or overwrite the running `./bin/dev` binary (use non-AOT build into `obj/`/`bin/`, not `publish` to `./bin/dev`).
- [ ] Add or update `dev-cli-tests` covering the new build step and verify `./bin/dev build` fails fast when `tools/dev-cli/` has a compile error.
- [ ] Run `./bin/dev self-install`, `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `./bin/dev template-smoke`.

## Checklist

- [ ] `BuildCommand` compiles `tools/dev-cli/dev.cs` (non-AOT) with 0 warnings/errors
- [ ] Works while invoked via `./bin/dev build` (no file-lock conflict with `./bin/dev`)
- [ ] Works in both template repo and generated app (`dev template-smoke` passes)
- [ ] `dev-cli-tests` updated and passing
- [ ] `BuildCommand` Design region updated

## Notes

- Origin: `/tw-walk-retro` on task 533 (PR #714), Finding 2. Task 526 closed the same blind spot for unlisted `tests/` Jaribu aggregators; `tools/dev-cli/dev.cs` is the remaining uncompiled entry point.
- Keep NativeAOT `dev self-install` separate for when an updated `./bin/dev` executable is actually needed.

## Session

- Filed from `/tw-walk-retro` on task 533 (2026-11-27).
