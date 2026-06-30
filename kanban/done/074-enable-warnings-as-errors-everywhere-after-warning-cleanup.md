# 074: Enable warnings-as-errors everywhere (after warning cleanup)

Depends on: 072. Parent scope: original 072 (split).

## Why

Root `Directory.Build.props` sets `TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors` to
`true`, so **source/ builds clean**. `tests/Directory.Build.props` deliberately relaxes both to
`false` — analyzers still run, but ~1,029 warnings are tolerated. CI stays green while warning debt
accumulates in test projects.

## Goal

Clear the test-tree warning backlog, then enable warnings-as-errors for `tests/` too (including NuGet
warnings where appropriate). Confirm `dev build` + CI stay green.

## Current inventory (Release build, 2026-06-30)

All warnings originate under `tests/`. Top codes by volume:

| Code | ~Count | Notes |
|------|--------|-------|
| CA2252 | 784 | Experimental API usage — suppressed in `source/container-apps/` but not tests |
| CA1707 | 416 | Underscores in identifiers — common in Fixie/Jaribu test naming |
| CA1515 | 208 | Types can be made internal |
| CA2007 | 184 | `ConfigureAwait` |
| CA1822 | 112 | Members can be marked static |
| CA2201 | 60 | Exception type too general |
| CA1303 | 40 | Localization |
| NU1608 | 14 | Transitive version conflicts (Oakton/Hosting, AutoFixture/FakeItEasy) |

Also present: nullable `CS86xx`, `CS1102`, and smaller CA counts.

## Policy decisions (resolve before bulk fixes)

- **CA1707**: fix vs. `NoWarn` for test naming conventions (see also 071).
- **CA2252**: suppress for tests (mirror source), enable preview features, or fix call sites.
- **Test bar**: full parity with product code, or a curated test-specific allowlist with commented
  `NoWarn` entries?

## Checklist

- [x] Re-run inventory after 072 lands (`dotnet build -c Release`; group by CA/IDE/CS/NU).
- [x] Resolve policy decisions above; document choices in this task's Notes.
- [x] Fix warnings rule-by-rule (or scoped `NoWarn` with justification) — tests are the bulk.
- [x] Resolve `NU1608` transitive conflicts (Oakton vs `Microsoft.Extensions.Hosting` 10,
      AutoFixture.AutoFakeItEasy vs FakeItEasy 9) — pin or upgrade.
- [x] Flip `TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors` to `true` in
      `tests/Directory.Build.props`; promote `NU*` via `WarningsAsErrors` / `WarningsNotAsErrors`
      as appropriate.
- [x] Acceptance: `dotnet build timewarp-architecture.slnx -c Release` reports **0 warnings**;
      `dev build` + `dev workflow` (pr mode) + CI green.

## Notes

Long tail — expect days, not hours. Do not start until 072 is done.
## DONE — 0 warnings, warnings-as-errors enforced on tests

Approach (chosen): **curated allowlist + fix the real bugs.**

**Suppressed in `tests/Directory.Build.props`** (each with a justification comment) — rules that don't
apply to test code: CA2252 (experimental API, mirrors source), CA1707 (Fixie/Jaribu underscore naming),
CA2007 (ConfigureAwait), CA1303 (localization), CA1515/CA1052/CA1051/CA1062 (visibility / null-check
of framework-supplied args), CA1822 (static — Fixie instance methods), CA1852/CA2201/CA1859/CA1861/
CA2234/CA2214/IDE0007/RCS1102, CA2000 (dispose — false positives; host/HttpClient lifecycle owned by the
framework, short-lived test scope), NU1608 (Oakton 6.3.0 / AutoFixture.AutoFakeItEasy 4.18.1 have stale
upper bounds vs .NET 10 / FakeItEasy 9; both are latest, resolved versions are runtime-compatible).

**Fixed (real):**
- CA2012 ValueTask: `WebApplication.DisposeAsync().AsTask().GetAwaiter().GetResult()`; `await ...DisposeAsync()`.
- CA1725: `scoped-sender.cs` param `streamRequest` → `request`.
- CA1012: `BaseTest` ctor `public` → `protected`.
- Nullable (~20): `Person` props `= null!`; `Validator` test fields `= new()`; deserialize round-trips
  null-forgiving (`!`); `BaseTest` uses `GetRequiredService`; `Send(object)` returns `Task<object?>`;
  `TestAnalyzerConfigOptions.TryGetValue` matches the base (`[NotNullWhen(true)] out string?`).

**Result:** `dotnet build timewarp-architecture.slnx -c Release` → **0 warnings, 0 errors**;
`tests/Directory.Build.props` now sets `TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors`
= `true`; `dev test` green. Was ~2,056 warning lines.
