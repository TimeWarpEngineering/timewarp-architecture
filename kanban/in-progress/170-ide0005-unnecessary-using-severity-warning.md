# IDE0005 unnecessary using severity warning

## Description

Raise IDE0005 (unnecessary using directive) from suggestion to **warning** so it surfaces
in VS Code, Roslyn language server hosts, and agent tooling the same way — and becomes
build-breaking under TreatWarningsAsErrors.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.IDE0005.severity = warning`
- [ ] Build/cleanup: fix or accept fallout from TreatWarningsAsErrors on IDE0005

## Session

- Implementation: grok (2026-08-06)

## Results

### What changed
- Root `.editorconfig` sets IDE0005 severity to warning (task 170).

### How to validate
- Open a file with a redundant `using` (e.g. namespace already in `global-usings.cs`).
- Expect IDE0005 as **warning** in VS Code Problems (not suggestion-only).
- `dotnet build` on a project with that file should fail under TreatWarningsAsErrors until the using is removed.
