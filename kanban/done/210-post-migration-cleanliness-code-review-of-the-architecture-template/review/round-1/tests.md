# Round 1 — tests
**Date:** 2026-09-09
**Scope reviewed:** Single-framework check (Fixie/xUnit/NUnit/MSTest/FluentAssertions) across
Directory.Packages.props, every tests/*.csproj, and every co-located runfile `#:package`
directive; `dev test` execution model (tools/dev-cli/endpoints/test-command.cs) vs the actual
tests/ tree (csproj enumeration, orphaned directories); family JARIBU_MULTI aggregators
(web-jaribu-tests, api-jaribu-tests) glob coverage against all 19 `source/**/*-tests.cs`
runfiles; co-located runfile preamble drift against the two AGENTS.md exemplars
(create-role-tests.cs, get-weather-forecasts-tests.cs); fixture lifetime (C-create/C-share,
SessionHostFixture subclasses, static-Lazy/static-host patterns) and stale Fixie-era Design
prose; tests/common/timewarp-testing and timewarp-testing-tests dead-helper/slnx check;
template.json test-path exclusions vs actual paths; Definition-of-Done coverage for all 35
`[ApiEndpoint]` contracts in web-contracts/api-contracts; global.json SDK/test.runner
consistency across all 18 test-project global.json files. Did not run the full `dev test` suite
(fixed ports); did not build any project.

## Summary
The Fixie→Jaribu migration (epic 145) is clean at the package level: zero Fixie/xUnit/NUnit/
MSTest/FluentAssertions references anywhere, one pinned Jaribu version, and all 18 test-project
`global.json` files are byte-identical on SDK/test.runner. The family aggregators
(web-jaribu-tests, api-jaribu-tests) correctly glob and compile all 19 co-located
`source/**/*-tests.cs` runfiles, and preamble drift across those 19 files is minor and
justified. The two real problems found: `tests/foundation/foundation-domain-jaribu-tests/`
is a genuinely dead, never-executed duplicate of `foundation-domain-tests/enumeration-tests.cs`
with a stale comment claiming it duplicates a Fixie suite that no longer exists; and the
`ListAgentHumanLinks` endpoint plus the `UpdateRole`/`DeleteRole` validators have zero test
coverage despite AGENTS.md's Definition of Done making endpoint validation-rejection coverage
mandatory.

## Issues

### Issue 1 — Severity: bug
- File: tests/foundation/foundation-domain-jaribu-tests/enumeration.cs:1-8
- Description: This standalone runfile is not compiled by anything — it has no `.csproj`, so
  `dev test`'s `tests/*.csproj` glob (tools/dev-cli/endpoints/test-command.cs:73-78) skips it
  entirely; it is not under `source/`, so neither the web nor api JARIBU_MULTI aggregator globs
  it either; its filename (`enumeration.cs`, not `*-tests.cs`) wouldn't match those globs even
  if it were relocated. The file's own header comment says "Jaribu duplicate of the Fixie suite
  in tests/foundation/foundation-domain-tests (task 046)" and "Kept in parallel as a small worked
  example of BOTH test frameworks against the same class" — but Fixie was retired in task
  145-007, and `tests/foundation/foundation-domain-tests/enumeration-tests.cs` is now itself a
  Jaribu MTP suite (confirmed: its csproj comment reads "mechanical Fixie→Jaribu conversion").
  Diffing the two files shows equivalent test cases (GetAll, FromValue, FromName,
  FromAlternateCode, FromString, CompareTo, Equals/GetHashCode, ToString, Constructor) against
  the same `Enumeration`/`Color` fixture, just with different naming conventions
  (`Enumeration_GetAll_Given_.ConcreteEnumeration_Should_ReturnAllStaticFields` vs
  `GetAll.Returns_all_static_fields`). So this is no longer "two frameworks, one worked example"
  — it is one framework's suite duplicated by a dead, unexecuted runfile whose own Purpose/Design
  comment misdescribes current reality (AGENTS.md's context-regions rule: "A Design region
  describing the old approach is a bug you just introduced").
- Suggestion: Delete `tests/foundation/foundation-domain-jaribu-tests/` (and its
  `Directory.Build.props`) now that the "house runfile precedent" it was created to demonstrate
  (task 046) is fully superseded by the standardized co-located runfile preamble (task 135) used
  everywhere else. If it's kept as a deliberate "how a standalone dotnet-run runfile looks"
  teaching example, rewrite the header to say so plainly and stop claiming it duplicates Fixie.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-contracts.cs:1-61
- Description: `ListAgentHumanLinks` is a hosted `[ApiEndpoint]` (`GET api/agent-links`) with its
  own handler (`list-agent-human-links-handler-application.cs`) but has zero test coverage
  anywhere in the repo — not in `agent-human-link-tests.cs` (which covers
  RequestAgentHumanLink/ApproveAgentHumanLink/DenyAgentHumanLink/GetHumanUx but never
  ListAgentHumanLinks), not in any suite-shaped project, not in contract serialization tests.
  Confirmed via `grep -rli "listagenthumanlinks|list-agent-human-links"` across tests/ and
  source/ — the only hits are the contract itself, its handler, and SPA client state files
  (agent-links-state.fetch.cs) that call it from the client side without a backend test. AGENTS.md
  Definition of Done: "API endpoint: contract ... + Handler + co-located Jaribu integration tests
  (happy path AND validation rejection)" is unconditional for `[ApiEndpoint]` contracts, so this
  is a hard gap, not a nice-to-have.
- Suggestion: Add a co-located Jaribu test (or extend `agent-human-link-tests.cs`) exercising
  `ListAgentHumanLinks` for both the human-session and agent-token dual-scheme paths described in
  its own Design region.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/features/admin/roles/update-role/update-role-contracts.cs:34-41; source/container-apps/web/features/admin/roles/delete-role/delete-role-contracts.cs:37-42
- Description: Both `UpdateRole.Validator` (RoleId NotEmpty + RoleDetailsValidator +
  AuthApiRequestValidator) and `DeleteRole.Validator` (RoleId NotEmpty + AuthApiRequestValidator)
  have real FluentValidation rules, but neither has a validation-rejection test anywhere. The
  only coverage is
  `tests/container-apps/web/web-server-integration-tests/features/admin/roles/roles-endpoint-tests.cs:116-171`
  (`UpdateThenDelete_Roundtrip.Create_Update_Get_Delete`), which is happy-path only (plus one
  not-found business-rule check, which is not the same as a FluentValidation rejection). Neither
  operation appears in `roles-authorization-tests.cs` either (grep for `DeleteRole|UpdateRole`
  in that file returns nothing), so the AuthApiRequestValidator branch is also unexercised for
  these two. Compare to `CreateRole`, which has a dedicated
  `create-role/create-role-validator-tests.cs`. This is the same DoD requirement as Issue 2
  ("happy path AND validation rejection").
- Suggestion: Add `Validator` rejection tests for `UpdateRole` (empty RoleId, invalid
  RoleDetails) and `DeleteRole` (empty RoleId), following the `create-role-validator-tests.cs`
  pattern.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-infrastructure-tests/ef-principal-role-store-tests.cs:16-17,29,117,121,162; tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs:21-22,34,90,94,135; tests/container-apps/web/web-infrastructure-tests/profile-postgres-persistence-tests.cs:13-14,105,118,131,170
- Description: All three files independently define a near-identical `PostgresAvailability`
  record, `Lazy<Task<PostgresAvailability>> Availability` field, `ResolveAvailabilityAsync`
  (env connection string vs. ephemeral Testcontainers), and `IsCiEnvironment` helper — same
  skip-when-unavailable logic, copy-pasted three times with minor shape drift (one record carries
  a `Container` field, one doesn't). This is exactly the kind of "two things must agree, generate
  one from the other" duplication the repo's own conventions (AGENTS.md "Prefer
  analyzers/source generators over convention-by-memory") warn about, applied to test
  infrastructure instead of product code.
- Suggestion: Extract the shared availability-resolution logic to `tests/common/timewarp-testing`
  (or a shared file within `web-infrastructure-tests`) so the three Postgres-backed suites share
  one implementation.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:22,84
- Description: The runfile's namespace is `TimeWarp.Architecture.Task205` — named after the
  kanban task that pinned this decision rather than after what the code does. This ships in the
  generated template (it's a co-located product-folder file) and conflicts with the repo's own
  "namespaces do not track folders [or tasks]" convention; a namespace literal like `Task205` is
  meaningless outside this repo's kanban history and won't mean anything to a generated app's
  maintainers. The file deliberately imports across four product slices
  (AgentLinks.Application, Identity.Application, MeteredCapability.Application,
  Profiles.Application) to prove a cross-cutting invariant, so a single feature-slice namespace
  doesn't fit cleanly — but a task number is not the right escape hatch either.
- Suggestion: Rename to something behavior-based, e.g.
  `TimeWarp.Architecture.Features.Identity.ProgressiveProfileGateTests` or a dedicated
  cross-slice-invariants namespace, consistent with how other multi-slice test files are named.
- Status: open

### Issue 6 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/identity/agent-registration-tests.cs:9-13; tests/container-apps/web/web-server-integration-tests/features/identity/agent-protected-endpoint-tests.cs:12; tests/container-apps/web/web-server-integration-tests/features/identity/passkey-registration-tests.cs:15; tests/container-apps/web/web-server-integration-tests/features/identity/infrastructure/integration-software-agent-key.cs:16
- Description: Several Design regions describe the still-current per-class shared-state behavior
  (a `static HostGraph? Graph` populated once in `SetupOnce` and shared across every test method
  in the class) using the phrase "Fixie per-class fixture sharing" as if Fixie were still the
  active framework causing it. The behavior itself is accurately described (and is correct
  Jaribu C-create usage — the field really is shared across methods within one class), but with
  Fixie now fully retired (zero Fixie references in any package/csproj), labeling an ongoing
  Jaribu behavior with the old framework's name reads as stale and could mislead someone into
  thinking Fixie is still involved.
- Suggestion: Reword to describe the pattern generically (e.g. "the static Graph field is shared
  across every test method in this class, same lesson as under the old Fixie suite this replaced")
  so the historical reference doesn't read as a claim about the current framework.
- Status: open

### Issue 7 — Severity: nit
- File: tests/common/timewarp-testing-tests/timewarp-testing-tests.csproj; tests/tools/agent-identity-cli-tests/agent-identity-cli-tests.csproj
- Description: Neither project is listed in `timewarp-architecture.slnx`. This is harmless for
  `dev test` (which globs `tests/*.csproj` directly, not the .slnx) and for warnings-as-errors
  (inherited per-project from Directory.Build.props regardless of solution membership), but
  unlike `web-jaribu-tests.csproj`/`api-jaribu-tests.csproj` — which carry an explicit "Not in
  the .slnx — discovered only by `dev test` globs" comment — these two have no comment explaining
  the omission, and `dev build` (which builds the .slnx) never touches them, so an IDE opening
  the solution won't show them either.
- Suggestion: Either add them to the .slnx for IDE visibility, or add the same one-line
  "intentionally excluded from .slnx" comment the jaribu aggregators carry so the omission reads
  as a decision rather than an oversight.
- Status: open

## Coverage table — `[ApiEndpoint]` contracts (web-contracts + api-contracts)

| Contract | Happy path | Validation rejection | Test file(s) |
|---|---|---|---|
| GetAgentBearerIdentity | yes | yes | source/.../get-agent-bearer-identity-tests.cs |
| GetWeatherForecasts | yes | yes | source/.../get-weather-forecasts-tests.cs |
| ListPrincipals | yes | yes | source/.../list-principals-tests.cs |
| SetPrincipalRoles | yes | yes | source/.../set-principal-roles-tests.cs |
| CreateRole | yes | yes | source/.../create-role-tests.cs, .../create-role-validator-tests.cs |
| DeleteRole | yes | **MISSING** | roles-endpoint-tests.cs (happy path only) — Issue 3 |
| GetRole | yes | n/a (auth-only validator) | roles-endpoint-tests.cs |
| GetRoles | yes | n/a (auth-only validator) | roles-endpoint-tests.cs, roles-authorization-tests.cs |
| SetRolePermissions | yes | yes (lockout suite) | set-role-permissions-lockout-tests.cs, source/.../set-role-permissions-tests.cs |
| UpdateRole | yes | **MISSING** | roles-endpoint-tests.cs (happy path only) — Issue 3 |
| ApproveAgentHumanLink | yes | yes | source/.../agent-human-link-tests.cs |
| DenyAgentHumanLink | yes | yes | source/.../agent-human-link-tests.cs |
| GetHumanUx | yes | n/a | source/.../agent-human-link-tests.cs |
| ListAgentHumanLinks | **MISSING** | **MISSING** | none — Issue 2 |
| RequestAgentHumanLink | yes | yes | source/.../agent-human-link-tests.cs |
| TrackEvent | yes | yes | track-event-validator-tests.cs, track-event-handler-tests.cs |
| Hello | yes | yes | hello-handler-tests.cs, hello-validator-tests.cs |
| AddAgentKey | yes | (covered via credential suites) | credential-add-tests.cs |
| AddPasskey | yes | (covered via credential suites) | credential-add-tests.cs, credential-list-tests.cs |
| CompleteAgentKeyRegistration | yes | (covered) | agent-registration-tests.cs, invoke-metered-capability-tests.cs |
| CompleteAgentTokenIssuance | yes | (covered) | agent-token-tests.cs |
| CompletePasskeyAuthentication | yes | (covered) | passkey-authentication-tests.cs |
| CompletePasskeyRegistration | yes | (covered) | passkey-registration-tests.cs |
| EndBrowserSession | yes | n/a (empty Validator) | end-browser-session-tests.cs |
| GetAgentIdentity | yes | (covered) | agent-protected-endpoint-tests.cs |
| GetCredentials | yes | (covered) | credential-list-tests.cs |
| GetCurrentSession | yes | (covered) | multiple identity/session suites |
| RevokeCredential | yes | (covered) | credential-revoke-tests.cs, revoke-credential-concurrency-retry-tests.cs |
| StartAgentKeyRegistration | yes | (covered) | agent-registration-tests.cs |
| StartAgentTokenIssuance | yes | (covered) | agent-token-tests.cs |
| StartPasskeyAuthentication | yes | (covered) | passkey-authentication-tests.cs |
| StartPasskeyRegistration | yes | (covered) | passkey-registration-tests.cs |
| InvokeMeteredCapability | yes | yes | source/.../invoke-metered-capability-tests.cs |
| GetProfile | yes | (covered) | get-profile-session-tests.cs, source/.../get-profile-tests.cs |
| UpdateProfile | yes | (covered) | update-profile-session-tests.cs, source/.../update-profile-tests.cs |
| SubmitTip | yes | (covered) | source/.../submit-tip-tests.cs |

"(covered)" = validator has only `AuthApiRequestValidator`/composed shared validators and the
rejection path is exercised indirectly through the ceremony/ authorization suites listed; not
independently spot-checked method-by-method beyond confirming the operation is exercised.

## Checked clean

- **Single framework**: zero Fixie/xUnit/NUnit/MSTest/FluentAssertions package references in
  Directory.Packages.props, any `tests/*.csproj`, or any runfile `#:package` directive. All
  matches on those keywords are historical comments ("mechanical Fixie→Jaribu conversion",
  "Not a test project — no tests of its own (task 145-007: Fixie era ended)"). Shouldly 4.3.0 /
  TimeWarp.Jaribu 1.0.0-beta.15 / TimeWarp.Jaribu.TestingPlatform 1.0.0-beta.15 are each pinned
  exactly once.
- **global.json consistency**: all 18 test-project `global.json` files are byte-identical
  (`sdk.version: 10.0.400`, `rollForward: latestFeature`, `test.runner: Microsoft.Testing.Platform`),
  matching the root `global.json` SDK pin.
- **Family aggregator glob coverage**: all 19 `source/**/*-tests.cs` runfiles are covered by
  exactly one of `web-jaribu-tests.csproj`'s or `api-jaribu-tests.csproj`'s
  `features/**/*-tests.cs` + `platform/**/*-tests.cs` globs; none are orphaned. No grpc
  `*-tests.cs` files exist yet, so the absence of a grpc-jaribu-tests aggregator is not currently
  a gap.
- **Preamble consistency**: all 19 runfiles match the exemplar shape (shebang, `#:project`,
  `#:package TimeWarp.Jaribu`/`Shouldly`[/`FluentValidation`], `#:property PublishAot=false`,
  `#:property NoWarn=...`, `//-:cnd:noEmit` / `#if !JARIBU_MULTI` guard, block-scoped namespace).
  Extra NoWarn codes and `DefineConstants=$(DefineConstants);api` in several files are documented,
  deliberate (task 145-004 R2-1 / task 164), not drift.
- **Fixture lifetime**: only one `SessionHostFixture<TInner>` subclass exists
  (`SpaSessionFixture`), and it matches the base class's own documented exemplar exactly —
  `CreateAsync` delegates to the pre-existing `SpaIntegrationHost.StartAsync` per-class factory,
  registered once via a dedicated `ModuleInitializer`. No process-static `Lazy`/bare-static
  sharing of a host **across classes** was found; every `private static HostGraph? Graph` field
  is scoped to (and disposed by) its own class's `SetupOnce`/`CleanUpOnce`, which is correct
  C-create.
- **template.json test-path exclusions**: every `tests/...` path referenced in
  `.template.config/template.json` (agent-identity-cli-tests, foundation/**, timewarp-identity-tests,
  timewarp-402-tests, analyzers/**, the three `applications/*-test-server-application.cs` files,
  timewarp-testing-tests/**, container-apps/api/**, container-apps/web/**,
  web-spa-integration-tests' serialization/pipeline/features test files, web-infrastructure-tests/**)
  resolves to an existing file or directory. None dangle.
- **timewarp-testing-tests**: proves `HostGraphFactory`'s Web+Api C-create shape
  (`host-graph-factory-tests.cs`), is not in the .slnx (Issue 7, nit) but IS picked up and run by
  `dev test`'s direct `tests/*.csproj` glob.
