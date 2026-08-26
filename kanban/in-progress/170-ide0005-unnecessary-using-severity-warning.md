# IDE0005 unnecessary using severity warning

## Description

Raise IDE0005 (unnecessary using directive) from suggestion to **warning** so it surfaces
in VS Code, Roslyn language server hosts, and agent tooling the same way — and becomes
build-breaking under TreatWarningsAsErrors.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.IDE0005.severity = warning`
- [x] `Directory.Build.props`: `GenerateDocumentationFile` + XML-doc NoWarn (Roslyn #41640)
- [x] Clean IDE0005 on web-spa dependency chain; SettingsPage redundant usings removed
- [ ] Full-repo IDE0005 sweep: no remaining unused usings; `dev build` 0/0
- [ ] Update Results + How to validate (solution-level, not web-spa-only)
- [ ] `ganda kanban done 170`; kanban-only-or-product PR; STOP (do not merge)

## Session

- Implementation: grok (2026-08-06)
- Cockpit: Grok review+fix (2026-08-26) — policy already on master; remaining is full-repo sweep then board close

## Notes

### Review (2026-08-26)

Policy already on `origin/master` (`98627e31` / `02781238`):

- Root `.editorconfig`: `dotnet_diagnostic.IDE0005.severity = warning` (TreatWarningsAsErrors)
- `Directory.Build.props`: `GenerateDocumentationFile=true` so IDE0005 fires on build (Roslyn #41640); CS1591 and other XML-doc IDs stay in `NoWarn`
- Agent note (task 172): Roslynk **`remove_unused_usings`**, not `apply_code_fix(IDE0005)`
- Task **172** leftover “full-repo IDE0005 sweep” is **this task’s remaining item** (204 treated 172 leftovers as follow-ups). Do **not** reopen 172.

**Must fix:** last checklist item. Results still say full-solution build may hit IDE0005 outside web-spa.

**Out of scope unless they fail `dev build`:** GlobalUsingsAnalyzer0003 + `inside_namespace` (172 leftover), XML docs **177**, TW0002 (**171** already done).

### Implement remaining

1. Stay in this claim worktree. Do not invent a sibling task.
2. Sweep unused usings repo-wide until `dev build` is 0/0 with no IDE0005.
   - Prefer Roslynk `remove_unused_usings` (or `dotnet format style --diagnostics IDE0005` if it does not drop Purpose regions).
   - Unused `global using` entries **are** IDE0005 — remove them.
   - If a global-usings rewrite drops `#region Purpose`, restore it.
   - Preserve template preprocessor regions (`<!--#if (flag)-->` / `#if flag`).
3. Do not change IDE0005 severity or undo GenerateDocumentationFile/NoWarn unless a defect is proven.
4. Rewrite `## Results` including `### How to validate` with copy-paste Smoke + Expect for `dev build` 0/0 (not web-spa-only).
5. `ganda kanban done 170`, commit, `tw-pr` / `gh pr create` with explicit `--head` and `--base`. STOP. Do not merge.

## Results

### What changed
- IDE0005 severity = warning in root `.editorconfig`
- Docs file enabled so IDE0005 runs on build; CS1591 and other pure XML-doc noise suppressed
- Fixed redundant usings / related fallout on web-spa graph (SettingsPage, foundation-contracts, analyzers, contracts globals)

### How to validate
- VS Code: redundant usings show as **warning** (IDE0005), not suggestion-only
- `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Debug` → 0/0
- Expect full-solution build may still hit IDE0005 outside web-spa until remaining format sweep
