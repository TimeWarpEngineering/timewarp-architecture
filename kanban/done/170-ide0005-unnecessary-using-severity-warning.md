# IDE0005 unnecessary using severity warning

## Description

Raise IDE0005 (unnecessary using directive) from suggestion to **warning** so it surfaces
in VS Code, Roslyn language server hosts, and agent tooling the same way — and becomes
build-breaking under TreatWarningsAsErrors.

## Checklist

- [x] `.editorconfig`: `dotnet_diagnostic.IDE0005.severity = warning`
- [x] `Directory.Build.props`: `GenerateDocumentationFile` + XML-doc NoWarn (Roslyn #41640)
- [x] Clean IDE0005 on web-spa dependency chain; SettingsPage redundant usings removed
- [x] Full-repo IDE0005 sweep: no remaining unused usings; `dev build` 0/0
- [x] Update Results + How to validate (solution-level, not web-spa-only)
- [x] `ganda kanban done 170`; kanban-only-or-product PR; STOP (do not merge)

## Session

- Implementation: grok (2026-08-06)
- Cockpit: Grok review+fix (2026-08-26) — policy already on master; remaining is full-repo sweep then board close
- Sweep + close: grok (2026-08-26) — unused `Foundation.Features` globals dropped; solution 0/0; board close

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
- Policy (already on `origin/master`): IDE0005 = warning in root `.editorconfig`; `GenerateDocumentationFile=true` so it runs on build (Roslyn #41640); CS1591 and other XML-doc IDs stay in `NoWarn`.
- Full-repo unused-using sweep: dropped unused `global using TimeWarp.Foundation.Features` from `api-server/global-usings.cs` and `web-server/global-usings.cs`. Kept `TimeWarp.Foundation.Behaviors` on api-server (`FluentValidationBehavior` via `typeof`). Purpose regions and `#if(postgres)` / `#if(api)` / `#if(grpc)` gates left intact. web-spa keeps its `#pragma warning disable IDE0005` (razor/@code false positives).
- Did **not** apply `dotnet format` import-order rewrites (`IMPORTS: Fix imports ordering` / GlobalUsingsAnalyzer0003 + `inside_namespace`) — out of scope unless they fail `dev build`.

### Files
- `source/container-apps/api/projects/api-server/global-usings.cs`
- `source/container-apps/web/projects/web-server/global-usings.cs`
- Policy files already on master: `.editorconfig`, `Directory.Build.props`
- Pre-PR `dev check-version`: `<Version>` + platform CPM pins `2.0.0-beta.16` → `2.0.0-beta.17` (`source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`, `Directory.Packages.props`)

### Key decisions
- Trust `dotnet build` over Roslynk/`dotnet format` IDE0005 on `global using` when the workspace compilation is incomplete: removing `TimeWarp.Foundation.Behaviors` from api-server failed with CS0246 on `FluentValidationBehavior<,>`.
- EF migrations stay `generated_code = true` (IDE0005 skipped there by design).

### Test outcomes
- `dotnet build timewarp-architecture.slnx -c Release` (clean + `--no-incremental`): **0 Warning(s) / 0 Error(s)** — no IDE0005.
- Extra csproj not in the sln (jaribu aggregators, timewarp-testing-tests, agent-identity-cli-tests) and file-based `tools/dev-cli/dev.cs` + `tools/agent-identity-cli/agent.cs`: **0/0**.
- `dotnet format style --diagnostics IDE0005 --verify-no-changes`: **no IDE0005 remaining**. Residual reports are `IMPORTS: Fix imports ordering` only (out of scope).

### How to validate

**Smoke**
```bash
dotnet build timewarp-architecture.slnx -c Release
```
(equivalent to `./bin/dev build` when the dev CLI is installed)

**Expect**
- `Build succeeded.`
- `0 Warning(s)`
- `0 Error(s)`
- No `IDE0005` in the build output.

**Automated gate**
```bash
dotnet build timewarp-architecture.slnx -c Release --no-incremental -v q
# expect: 0 Warning(s), 0 Error(s)

dotnet format style timewarp-architecture.slnx --diagnostics IDE0005 --severity warn --verify-no-changes --verbosity diagnostic
# expect: zero lines containing "IDE0005"
# note: exit may still be non-zero due to out-of-scope IMPORTS ordering; ignore those
```

**Not in scope:** GlobalUsingsAnalyzer0003 + `csharp_using_directive_placement = inside_namespace` (task 172 leftover); XML docs (task 177).
