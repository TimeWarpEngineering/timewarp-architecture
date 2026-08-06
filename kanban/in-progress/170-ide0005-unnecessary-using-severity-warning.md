# IDE0005 unnecessary using severity warning

## Description

Raise IDE0005 (unnecessary using directive) from suggestion to **warning** so it surfaces
in VS Code, Roslyn language server hosts, and agent tooling the same way — and becomes
build-breaking under TreatWarningsAsErrors.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.IDE0005.severity = warning`
- [x] `Directory.Build.props`: `GenerateDocumentationFile` + XML-doc NoWarn (Roslyn #41640)
- [x] Clean IDE0005 on web-spa dependency chain; SettingsPage redundant usings removed
- [ ] Full-repo `dotnet format style --diagnostics IDE0005` / `dev build` sweep (remaining projects)

## Session

- Implementation: grok (2026-08-06)

## Results

### What changed
- IDE0005 severity = warning in root `.editorconfig`
- Docs file enabled so IDE0005 runs on build; CS1591 and other pure XML-doc noise suppressed
- Fixed redundant usings / related fallout on web-spa graph (SettingsPage, foundation-contracts, analyzers, contracts globals)

### How to validate
- VS Code: redundant usings show as **warning** (IDE0005), not suggestion-only
- `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug` → 0/0
- Expect full-solution build may still hit IDE0005 outside web-spa until remaining format sweep
