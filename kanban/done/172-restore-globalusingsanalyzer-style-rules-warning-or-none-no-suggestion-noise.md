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
- [x] Promote fixed style IDs to **warning** (RCS1261, RCS1251, RCS1077, IDE1006) so TreatWarningsAsErrors fails regressions
- [x] RCS1261: `using` → `await using` for IAsyncDisposable DbContexts in aggregate/identity/profile model tests (methods made `async Task`)
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

### Self-install (dev CLI)

- Failure: `tools/dev-cli/global-usings.cs` IDE0005 (`System.Diagnostics` and other unused BCL globals).
- Fix: drop unused BCL globals only; keep `DevCli`, `DevCli.Services`, DI, Nuru/Amuru/Terminal.
- Verified: `dotnet run tools/dev-cli/dev.cs -- self-install` → installed to `bin/`.

### XML docs (follow-up)

- Full hollow/completeness RCS cluster silenced; decision to populate packages vs strip → **task 177**.

### RCS1261 (async dispose)

- Sites: `aggregate-db-context-tests.cs` (10), `identity-model-mapping-tests.cs` (3), `profile-model-mapping-tests.cs` (2).
- Change: local `using DbContext` → `await using`; methods `public static Task` + `return Task.CompletedTask` → `public static async Task`.
- Verified: Roslynk diagnostics show **0× RCS1261**; foundation-infrastructure-tests 11/11; web-infrastructure Map filter 5/5.

### Promote fixed IDs to warning

- `.editorconfig`: `RCS1261`, `RCS1251`, `RCS1077`, `IDE1006` = **warning** (with TreatWarningsAsErrors).
- IDE0005 already warning (task 170).

### CI green-up (post style PR #298)

Run `31560216427` (head `051c83c8`) failed on three independent clusters:

1. **foundation-infrastructure-tests** (2): RCS1170 get-only `Name` broke EF ctor binding — restore `private set` + suppress (`dd70fe65`).
2. **template-smoke** web-jaribu 34/54: `UntrustedRoot` — template-smoke job lacked `ci`'s `dotnet dev-certs https --trust` + `SSL_CERT_DIR` (`dd70fe65`).
3. **web-server-integration** (16, task 151): (a) abuse principal-registration ~10/min exhausted mid-class → 429; disable in `WebTestServerApplication`, re-enable in abuse co-located tests; (b) CreateRole validation expected 400 but got 403 — needs Administrator grant after passkey mint (`b39d3d81`).

Local verify: foundation 11/11; web-server-integration 116/0/1; abuse-rate-limiting 3/3.
