# Scaffold TimeWarp.Identity package in monorepo

## Parent

104

## Description

Create the TimeWarp.Identity project(s), wire CPM/solution, AssemblyMarker, empty public surface. Build green under `dev build`. Home: foundation-style or source package path consistent with existing monorepo layout — pick the simplest place that can grow into a publishable package later.

## Requirements

- Project builds with warnings-as-errors
- Referenced only where needed (no mass-wire yet)
- Purpose regions on seed files

## Checklist

- [ ] Create csproj + folder
- [ ] Solution / Directory.Build wiring
- [ ] AssemblyMarker
- [ ] `dev build` includes package cleanly

## Notes

Package name locked: **TimeWarp.Identity**. Not Passwordless.dev wrapper.

### Depends on

None — start here.

## Session

- Created: 2026-07-16
