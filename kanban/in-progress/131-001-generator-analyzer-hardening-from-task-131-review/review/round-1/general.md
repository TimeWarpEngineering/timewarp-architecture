# Round 1 — general
**Date:** 2026-07-29
**Scope reviewed:** commit `bcce35a8` (feat analyzers harden) and current tree under `source/analyzers/**` (shared/hosted-route-discovery.cs, fast-endpoint-source-generator.cs, endpoint-metadata.cs / EndpointEmitModel, diagnostic-descriptors.cs, endpoint-auth-posture-analyzer.cs, endpoint-coverage-analyzer.cs, ingress-route-prefix-generator.cs, attributes/api-endpoint-attribute.cs, AnalyzerReleases), AGENTS.md TWA0020 / TWE·SG tables, tests under `tests/analyzers/`, package-local + developer reference docs for ApiEndpoint generation.

## Summary

Task goals F-003, F-004, F-005, F-008, and F-014 hold in the implementation as checked against code: static `RouteRegistry` is gone; FastEndpoint uses equatable `EndpointEmitModel` + `.Collect()` with TWE003 on **all** conflict parties and **no** emission for the group; shared `HostedRouteDiscovery` is linked into both generator and convention-analyzer packages; ClientOnly is outer-or-nested consistently for generation skip, ingress, TWA0006, and TWA0020; `EndpointType` is deleted; verbs fail closed via TWE007 with Head/Options allowed; TWE001/TWE004 are gone, TWE002 is wired, and TWE/SG live in a single `DiagnosticDescriptors` SSOT with AGENTS.md tables matching. Auth posture and coverage pairing were not regressed (TWA0013/14 still fire; ClientOnly early-return on TWA0020 is intentional). Remaining notes are small fail-closed edge cases, a package-doc residual, and test/docs nits — nothing that overturns the feature goals.

## Goals verification (claims re-checked)

| Goal | Verdict | Evidence |
|------|---------|----------|
| **F-003** no static RouteRegistry; Collect batch TWE003 all parties; generate none of group | **Met** | No `RouteRegistry` / static concurrent registry under `source/analyzers/`. `FastEndpointSourceGenerator` Collect + group-by `(Route, HttpVerb)`; TWE003 per party; skip emit. Test `Should_Detect_Route_Conflicts_On_All_Parties_And_Generate_None` + dual-run stability. |
| **F-004** linked shared discovery; ClientOnly skip; TWA0020 | **Met** | `shared/hosted-route-discovery.cs` Compile-linked in both csprojs. Outer/nested ClientOnly in discovery, FastEndpoint skip, ingress `TryGetHostedOperation`, coverage `TryGetRoutedRequest`, TWA0020 analyzer + tests. |
| **F-005** EndpointType deleted | **Met** (code + developer doc) | Attribute is marker-only; emit always `BaseFastEndpoint`; `ApiEndpointSourceGenerator.md` documents no override. Residual stale line in package-local md (Issue 2). |
| **F-008** no GET default; TWE007; Head/Options | **Met** | `ResolveHttpVerbName` / `ConvertHttpVerbToMethodName` allow-list; unknown → TWE007; product `HttpVerb` includes Head/Options; tests for Trace fail-closed and Head/Options emit. |
| **F-014** TWE/SG consolidated; TWE001/004 gone; TWE002 wired | **Met** | Single `DiagnosticDescriptors`; no TWE001/004 descriptors; TWE002 reported on missing Query/Command; page/typed-id use shared TWE005/006/SG010/011; one SG001; AGENTS.md + Unshipped releases updated. |

## Issues

### Issue 1 — Severity: suggestion
- File: `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:95-120` and `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs:192-197`
- Description: Fail-closed verb/route handling is slightly inconsistent between shared discovery and the FastEndpoint emit path. `HostedRouteDiscovery.TryGetHostedOperation` rejects null/whitespace route templates (`IsNullOrWhiteSpace` → not hosted). `EndpointEmitModel.FromSymbol` accepts an empty template as `Route = ""` with a **resolved** verb (`verbUnresolved = false`), and `ProcessBatch` then **silently** `continue`s when `string.IsNullOrEmpty(model.Route)` — no TWE002/TWE007, no emission. Separately, a missing/incomplete `[ApiRoute]` is folded into TWE007 with display `"<missing ApiRoute>"`, so the diagnostic message claims an “unresolvable or unsupported HttpVerb” for a missing route attribute.
- Suggestion: Treat empty/whitespace route the same as shared discovery (either fail closed with an explicit diagnostic, or set `VerbUnresolved` / a dedicated shape flag and report). Prefer not to overload TWE007’s verb wording for “missing ApiRoute” unless the messageFormat is broadened (e.g. “unresolvable route or HttpVerb”).
- Status: open

### Issue 2 — Severity: nit
- File: `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.md:104-109`
- Description: F-005 asked to fix docs that teach dead `EndpointType` API. Developer reference `documentation/developer/reference/ApiEndpointSourceGenerator.md` is updated, but the package-local generator doc still lists **“Invalid endpoint type configurations”** under Error Handling and remains otherwise stale (e.g. implies scanning the including project; example still shows pre-task-110-ish shapes). Low impact if the developer reference is the SSOT, but the residual line still implies EndpointType validation exists.
- Suggestion: Delete the “endpoint type configurations” bullet and either refresh this file to match the developer reference / current diagnostics (TWE002/003/007, SG002, TWA0020) or point readers at `ApiEndpointSourceGenerator.md` only.
- Status: open

### Issue 3 — Severity: suggestion
- File: `tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-more-tests.cs:175-226` (and absence of cases)
- Description: Stated behaviors are well covered for the happy paths of this task (all-party TWE003 + zero sources, dual-run no phantom conflict, TWE002 missing Query/Command, TWE007 via `HttpVerb.Trace`, Head/Options emit without conflict, ClientOnly outer+nested skip, TWA0020 outer+nested, coverage outer ClientOnly). Gaps: (1) no generator test for **missing `[ApiRoute]`** on an otherwise complete `[ApiEndpoint]` (the TWE007 `"<missing ApiRoute>"` branch); (2) no test for empty/whitespace route template (Issue 1); (3) dual-run test reimplements the harness compilation and only works because FE/`BaseFastEndpoint` stubs are compiled **into** the contract assembly — the harness XML comment still wrongly names `TimeWarp.Architecture.Features.BaseFastEndpoint` while the stub is `TimeWarp.Foundation.Features.BaseFastEndpoint`.
- Suggestion: Add one missing-`ApiRoute` TWE007 test; optionally empty-route once policy is fixed; fix harness summary comment and prefer `GeneratorTestHarness.Run` + a second `RunGenerators` for dual-run to avoid drift.
- Status: open

## Non-issues (checked, not raised)

- **Incremental model:** `EndpointEmitModel` is a sealed record with `ImmutableArray<string> Tags` and value-typed flags — suitable for Collect batching; no Roslyn symbols retained across the pipeline.
- **ClientOnly placement:** outer **or** nested Query/Command is consistent across generator skip, ingress, TWA0006, TWA0020, and tests.
- **Auth posture:** TWA0013/0014 logic intact; TWA0020 early-return avoids noisy stacking and is documented.
- **Coverage pairing:** still gates on `BaseFastEndpoint` + paired `*contracts` name segment; ClientOnly opt-out via shared `TryGetRoutedRequest`.
- **SG002 once per batch:** reported then return before per-model work — matches plan.
- **Head/Options:** present on product `HttpVerb` and generator allow-list; same route different verbs do not TWE003 (covered by Head/Options test).
- **AGENTS.md:** TWA0020 + TWE/SG tables and retired TWE001/TWE004 note match descriptors and Unshipped releases.
