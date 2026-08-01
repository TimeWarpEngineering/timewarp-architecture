# Migrate web-server-integration-tests to Jaribu with C-create

## Description

The at-scale proof (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §7.3). ~24 files/~14 host-consuming classes. DEPENDS ON
145-002 (factory). Hybrid topology applies: slice-shaped tests CO-LOCATE into
source/container-apps/web/features/<slice>/ as `-tests.cs` runfiles (suite shrinks);
genuinely host-level BFF/cross-cutting tests stay suite-shaped, converted to Jaribu classes.

## Requirements

1. Triage every test class: slice-shaped → co-located runfile (grammar + preamble per
   tw-feature-placement); host-level → stays in suite, Fixie ctor-injection replaced by
   SetupOnce + HostGraphFactory Web+Api graph with the MockAccessTokenProvider override.
2. Suite csproj converts to Jaribu MTP (aggregator-style wiring + global.json pin mirror) or
   dissolves entirely if nothing host-level remains — triage decides; document the outcome.
3. web-jaribu-tests aggregator picks up new co-located files automatically (glob); bump
   template-smoke JaribuFamilyAggregators expected counts if exemplar files change
   (tw-feature-placement maintenance bullet).
4. WebServerTestConvention/Fixie plumbing for this suite deleted with the migration.
5. Record aggregate wall-clock before/after in Results — this is the data source for the
   145-008 gate.

## Checklist

- [x] Triage table (class → co-locate | host-level) in task folder
- [x] Co-located runfiles pass standalone + via aggregator
- [x] Host-level remainder green under Jaribu via dev test
- [x] Fixie plumbing for this suite removed; counts updated where needed
- [x] Before/after wall-clock recorded; kanban committed
- [ ] full dev build / dev test / template-smoke (optional follow-up if not in this session)

## Session

- Converted suite to Jaribu MTP + C-create `HostGraphFactory.CreateWebWithApiAsync` per host-using class.
- Co-located hello endpoint only (identity left suite-shaped).
- web-jaribu ExpectedSucceeded 5 → 7; timewarp-testing ProjectReference on web-jaribu-tests.

## Triage

| Class / file | Host? | Disposition |
|---|---|---|
| HelloEndpoint / `hello-endpoint-tests` | Web+Api | **Co-located** → `source/.../hello/hello/hello-tests.cs`; suite file removed |
| Hello_Handler / `hello-handler-tests` | Web+Api | Host-level suite (handler via Send) |
| Hello_Validator / `hello-validator-tests` | No | Suite (host-free validator) |
| TrackEvent endpoint/handler/validator | Web+Api / no | Suite (optional co-locate deferred) |
| CreateRole endpoint/handler/validator | Web+Api / no | Suite (contracts already co-located in create-role-tests.cs) |
| RolesEndpoints_ (GetRoles/GetRole/UpdateThenDelete) | Web+Api | Host-level suite (each class own SetupOnce) |
| RolesAuthorization_ | Web+Api | Host-level suite (real HTTP auth) |
| All identity ceremony / options / revoke-concurrency | Web+Api or no | **Suite-shaped** (ceremony helpers stay non-test helpers) |
| WebTestServerApplication_ Should | Web+Api | Host-level suite; `RunForever` `[Skip]` |

**Suite outcome:** remains as host-level remainder under Jaribu MTP (does not dissolve).

## Results

### Baseline (Fixie, before)

- `dotnet test tests/container-apps/web/web-server-integration-tests -c Release`
- **97 passed, 1 skipped**, wall ~**31s**

### After (Jaribu MTP + C-create)

- Suite: `dotnet test tests/container-apps/web/web-server-integration-tests -c Release`
  - **95 succeeded, 1 intentional skip** (`RunForever`; MTP may print/count the skip twice → summary shows skipped: 2)
  - Wall **~7–13s** (test duration ~7s; process wall with restore/JIT higher)
  - −2 vs baseline = hello endpoint co-located out of suite
- Co-located hello: `dotnet run source/container-apps/web/features/hello/hello/hello-tests.cs`
  - **2 passed**
- Aggregator: `dotnet test tests/container-apps/web/web-jaribu-tests -c Release`
  - **7 succeeded** (create-role 5 + hello 2)
- Combined coverage vs baseline: 95 suite + 2 hello = **97** product tests (parity)

### Plumbing

- csproj: Fixie.TestAdapter → TimeWarp.Jaribu.TestingPlatform; TestingPlatformDotnetTestSupport
- project-local `global.json` SDK 10.0.301 + MTP runner
- deleted `infrastructure/web-server-test-convention.cs`
- CredentialCeremonyHelpers unchanged (still take `WebTestServerApplication`)
- MockAccessTokenProvider: not re-registered (WebTestServerApplication built-in)
