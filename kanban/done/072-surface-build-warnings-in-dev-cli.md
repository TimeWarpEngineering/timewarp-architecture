# 072: Surface build warnings in dev CLI

Depends on: nothing. Blocks: 074.

## Why

CI/CD is green, but a full `dotnet build` emits **~1,000 warnings** (all from `tests/`; `source/` is
clean). Locally, `dev build` hides them: `build-command.cs` uses `CaptureAsync` and only prints
`result.Combined` with `--verbose` or on failure. Same pattern in `dev test` (output only on failure)
and parts of `dev workflow` (pack/push). You can't fix or enforce what you can't see.

## Goal

Stream MSBuild/test output by default in the dev CLI — same visibility as running `dotnet build` /
`dotnet test` directly. Keep an optional quiet mode if useful.

## Checklist

- [x] `dev build`: replace `CaptureAsync` + verbose-gated `ReportResult` with pass-through by default
      (`PassthroughAsync` — see `run-command.cs`). Replaced `--verbose` with `--quiet`.
- [x] `dev test`: stream per-project `dotnet test` output on success as well as failure; switched to
      `DotNet.Test()` + `PassthroughAsync`.
- [x] `dev clean`: same pass-through treatment; added `--quiet`.
- [x] `dev workflow`: inherits build/test/clean fixes; pack/push now use `PassthroughAsync`.
- [x] Verify: `dev build` shows compiler warnings without flags (~1,036 warnings); `dev build --quiet`
      hides them on success.

## Notes

Small, shippable change. Do this before 074 — warning cleanup needs visible output.

## Implementation Notes

- Added `endpoints/command-execution.cs` with shared `ReportPassthrough` / `ReportCapture` helpers.
- Default is live pass-through; `--quiet` / `-q` restores capture-and-hide-on-success on build, clean,
  and test.