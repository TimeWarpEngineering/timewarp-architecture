# Round 1 — general
**Date:** 2026-07-29
**Scope reviewed:** branch spike/134-jaribu-co-located-integration-tests vs dev

## Verification results

| # | Claim | Command | Observed | Result |
|---|---|---|---|---|
| 1a | `dev build` 0/0 with `-tests.cs` files present | `dotnet build timewarp-architecture.slnx` (no `bin/dev` AOT binary in reviewer worktree; fallback per framework) | Build succeeded, 0 Warning(s), 0 Error(s), 42s. Note: `jaribu-spike-tests.csproj` does **not** appear in the build output — not a member of the `.slnx`, so this run never compiled the aggregator or exercised analyzers against it. | CONFIRMED (as stated — solution build is clean) |
| 1b | Contracts runfile 5/5 | `dotnet run source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` | `Total: 5, Passed: 5` (3 classes, 8s incl. compile) | CONFIRMED |
| 1c | Integration runfile 2/2, real host on :7255 | `dotnet run source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs` (port verified free beforehand via `ss -tln`) | `Total: 2, Passed: 2`; log shows `Now listening on: https://localhost:7255`, real FastEndpoints/mediator requests (200 and 400) served over HTTP. Port released after process exit. 14.3s. | CONFIRMED |
| 1d | `dotnet test tests/container-apps/jaribu-spike-tests/` 7/7, sdk pin matches root | `cd tests/container-apps/jaribu-spike-tests && dotnet test` | `total: 7, succeeded: 7, failed: 0`, 605ms. Local `global.json` sdk pin `10.0.301` + `rollForward: latestFeature` — **identical** to root `global.json`; the "silently switched SDKs" risk did **not** materialize in what was committed. | CONFIRMED |

Two additional falsifiable checks beyond the brief, both refuting implicit safety claims:

| # | Claim (implied) | Command | Observed | Result |
|---|---|---|---|---|
| 2a | Runfiles survive `dotnet new` template processing unmodified | Installed spike branch as template, `dotnet new timewarp-architecture -n GenTest`, diffed generated files | Templating engine **strips** the `#if !JARIBU_MULTI` / `#endif` directive lines from both files (unrecognized symbol defaults to unset in dotnet-new's C-style conditional processor, so `!JARIBU_MULTI` evaluates true and the guarded `return await ...RunAllTests();` is kept — unconditionally, in every generated app). | REFUTED — see Issue 1 |
| 2b | `dev build` ignoring the aggregator via `.slnx` is sufficient; `dev test` unaffected | Read `tools/dev-cli/endpoints/test-command.cs`; reproduced its exact invocation (`dotnet test <path-to-csproj> -c Release` from repo root) against `jaribu-spike-tests.csproj` | `dev test` globs `tests/**/*.csproj` directly (not via `.slnx`) and **does** pick up `jaribu-spike-tests.csproj`. Its invocation form fails: `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later.` (also with `--project`). Bare `dotnet test` from inside the project directory works (per 1d) — but that is not the form `dev test` uses. | REFUTED — see Issue 2 |

## Summary

The spike proves the two hard claims it set out to prove: a host-free contracts round-trip runfile and a real-host integration runfile both run standalone via `dotnet run` with correct pass counts, and both are discoverable by `dotnet test`/M.T.P. via a hand-rolled `JARIBU_MULTI` aggregator project. `dev build` is genuinely unaffected (0/0), and the sdk-pin risk the implementer flagged did not make it into the committed `global.json`. However, two verified defects would actively mislead the follow-up adoption task if not called out: the `JARIBU_MULTI` conditional-compilation trick is not template-safe (breaks the generated app's own aggregator build), and the aggregator project — despite being invisible to `dev build` — is *not* invisible to `dev test`, which currently fails against it. Both are fixable in follow-up work, but as committed they are evidence the spike's findings.md must capture, not omit.

## Issues

### Issue 1 — Severity: bug
- File: `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs:34,36` and `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs:18,20` (the `#if !JARIBU_MULTI` / `#endif` pair in each file)
- Description: These files live inside template content trees. `dotnet-new`'s built-in C-style conditional processor scans `#if`/`#endif` in `.cs` files for *any* symbol name, not just registered template.json parameters; an unrecognized symbol (`JARIBU_MULTI` — a real MSBuild DefineConstants value, not a template flag) is treated as unset/false, so `#if !JARIBU_MULTI` evaluates true and the engine removes the directive lines but keeps the guarded body — every generated app gets `return await TimeWarp.Jaribu.TestRunner.RunAllTests();` unconditionally. Verified empirically: generated app's aggregator build fails with `error CS8802: Only one compilation unit can have top-level statements.`
- Suggestion: findings.md must record this as a confirmed template-safety gap. Durable fix candidates: route the mode switch through a template-recognized symbol, a `cnd:noEmit`-style escape, or exclude co-located `-tests.cs` from the template engine's conditional-processing file-type list. Regression gate: extend `dev template-smoke` once the permanent mechanism lands.
- Status: open

### Issue 2 — Severity: bug
- File: `tests/container-apps/jaribu-spike-tests/jaribu-spike-tests.csproj`; `tools/dev-cli/endpoints/test-command.cs:64-69`
- Description: `dev test` discovers via `Directory.GetFiles(testsDirectory, "*.csproj", AllDirectories)` — independent of `.slnx` — and runs `dotnet test <absolute-csproj-path> -c Release` from repo root. That form fails against MTP projects on .NET 10 (`Testing with VSTest target is no longer supported…`). `TestingPlatformDotnetTestSupport=true` only helps the newer invocation style (bare `dotnet test` from the project dir, verified 7/7). So `dev test` picks the aggregator up and fails on it today.
- Suggestion: Flag in findings.md as a concrete adoption blocker: MTP-native aggregator projects are incompatible with `dev test`'s current per-project invocation without (a) keeping such projects out of `tests/` or (b) a `dev test` change (out of scope for the spike; in scope to report).
- Status: open

### Issue 3 — Severity: suggestion
- File: `source/container-apps/api/msbuild/feature-membership.targets:40-42`, `source/container-apps/web/msbuild/feature-membership.targets:40-42`
- Description: The carve-out is a blanket `Exclude="$(…TreeRoot)/**/*-tests.cs"` glob. Any `.cs` under features/platform ending in `-tests.cs` — including a genuinely orphaned or misnamed file compiling into no project — silently passes `ValidateFeatureFileMembership` with zero diagnostic. Inline comment honestly marks this as not the proposed permanent mechanism.
- Suggestion: findings.md should state this blind spot as an explicit tradeoff of "exclude glob" vs "registered-unrouted `tests` suffix" (which keeps files matched-and-validated, just routed nowhere) — confirming evidence for the strategic carve-out question in plan.md.
- Status: open

No issues found with: the `Directory.Packages.props` pin (alphabetized, matches sibling `TimeWarp.Jaribu` version, no backward-pin); `#region Purpose` presence/placement (TWA0004 clean under TreatWarningsAsErrors wherever the files compile); `ContractSerializationDefaults.Options` usage (correct seam, no inline options); namespace placement (`…Features.Admin.Roles`, `…Features.WeatherForecasts` — consistent with TWA0009 shape); TW0001 kebab naming. The `Lazy<ApiTestServerApplication>`-never-disposed pattern is acceptable for a short-lived `dotnet run` process and does not invalidate the timing/behavior evidence — port independently confirmed free before and released after each standalone run.
