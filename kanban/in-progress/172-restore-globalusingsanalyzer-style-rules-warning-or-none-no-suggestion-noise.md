# Restore GlobalUsingsAnalyzer; style rules: warning or none (no suggestion noise)

## Description

Restore the orphaned **GlobalUsingsAnalyzer** NuGet package (used by nuru/tui/flexbox) so file-level
usings are pushed into project `global-usings.cs`. Align editorconfig style severities with house
policy: **warning/error = we enforce** (TreatWarningsAsErrors), **silent/none = off for agents/CI**
— no lingering `:suggestion` noise.

## Checklist

- [x] Create / move task in-progress
- [x] CPM pin `GlobalUsingsAnalyzer` 1.4.0 + `Directory.Build.props` PackageReference
- [x] Confirm editorconfig GlobalUsingsAnalyzer0001/0002/0003 (filename `global-usings.cs`, enabled, severity warning)
- [x] Eliminate all `:suggestion` from root `.editorconfig` (binary: warning or silent/none)
- [x] Promote solid naming rules + predefined types + readonly field to **warning**
- [x] Silence soft expression-style taste until a deliberate promote pass
- [x] Silence IDE0060-style unused-parameter flood (module registration signatures)
- [x] Silence RCS1138/RCS1139 XML-summary nags (same class as CS1591 docs noise)
- [x] Agent note: IDE0005 → Roslynk `remove_unused_usings` (not unreliable `apply_code_fix(IDE0005)`)
- [x] Sample project builds: api-application, web-spa, web-application green after cleanup
- [ ] Residual: full-repo IDE0005 / GlobalUsingsAnalyzer backlog (web-server etc.) — follow-up sweep
- [ ] Confirm whether GlobalUsingsAnalyzer0003 fires with `csharp_using_directive_placement = inside_namespace`

## Session

### Findings (other repos)

- Package: **GlobalUsingsAnalyzer** 1.4.0 (BDSoftware) — “move usings into a single project file”
- Wired in: timewarp-nuru, timewarp-tui, timewarp-flexbox, timewarp-builder; docs in ganda/flexbox enforcement.md
- This repo: editorconfig keys already present; package **removed** in task 041 as “orphaned”

### Policy applied

| Rule | Severity |
|------|----------|
| IDE0005 unnecessary usings | warning (task 170) |
| GlobalUsingsAnalyzer0003 move to global-usings.cs | warning (package restored) |
| Naming (I*, Pascal types/fields/consts/locals/local funcs) | warning |
| predefined type keywords, readonly field | warning |
| Soft expression taste (coalesce, collection expr, …) | silent |
| unused parameters (IDE0060) | silent (module signatures) |
| TW0002, RCS1138/1139, CA1308 | none |

### Agent fix path

- List: Roslynk `get_diagnostics` (include errors/warnings)
- Unused usings: Roslynk **`remove_unused_usings`** (re-add `#region Purpose` if a global-usings rewrite drops it)
- Do **not** rely on `apply_code_fix(..., IDE0005)` (NotFound even when listed)

## Results

- Restored **GlobalUsingsAnalyzer** 1.4.0 repo-wide (CPM + Directory.Build.props).
- `.editorconfig`: zero `:suggestion`; style policy documented at top of coding conventions.
- Sample green: api-application, web-spa, web-application (IDE0005 cleanup on touched files).
- Follow-up: solution-wide IDE0005 / move-to-global sweep; verify GlobalUsingsAnalyzer0003 with inside-namespace usings.

## Notes

- GlobalUsingsAnalyzer **loads** into `csc` (`/analyzer:.../GlobalUsingsAnalyzer.dll` confirmed).
- IDE0005 on dead `global using` entries is intentional: unused globals still fail the build.
