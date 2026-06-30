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

- [ ] `dev build`: replace `CaptureAsync` + verbose-gated `ReportResult` with pass-through by default
      (`PassthroughAsync` / `TtyPassthroughAsync` — see `run-command.cs` for the existing pattern).
      Retain `--verbose` only if it still adds value; otherwise repurpose or drop it.
- [ ] `dev test`: stream per-project `dotnet test` output on success as well as failure (today only
      dumps `result.Combined` when a project fails).
- [ ] `dev clean`: same pass-through treatment (currently capture-and-hide on success).
- [ ] `dev workflow`: inherits build/test fixes; review pack/push steps for the same capture pattern.
- [ ] Verify: `dev build` shows compiler warnings without `-v`; `dev workflow` (pr mode) logs are
      similarly visible in CI.

## Notes

Small, shippable change. Do this before 074 — warning cleanup needs visible output.