# Refactor Aspire AppHost and web-server program.cs over 500 lines

## Parent

469

## Description

Both composition roots have grown past 500 lines. Move resource and service registration into
cohesive extension or builder types grouped by concern, leaving `program.cs` as a short
composition root. Keep every `#if` flag region intact (TWA0008 / TWA0010); extracted code
keeps the same flag guards.

## Files

- source/container-apps/aspire/projects/aspire-app-host/program.cs (942)
- source/container-apps/web/projects/web-server/program.cs (679)

## Requirements

- `program.cs` 500 lines or fewer, ideally far less.
- AppHost resource names stay equal to ServiceNames.
- `dev template-smoke --matrix` passes, since both files sit under `#if` flags.
- Aspire tests and web-server integration tests pass. `dev run` still starts.

## Checklist

- [ ] Group AppHost registrations by concern into named types
- [ ] Group web-server pipeline setup by concern into named types
- [ ] Flag regions preserved
- [ ] dev build, template-smoke --matrix, dev run verified

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
