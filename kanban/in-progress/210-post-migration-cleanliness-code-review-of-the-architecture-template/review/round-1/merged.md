# Round 1 — merged findings
**Date:** 2026-09-09
**Sources:** leftovers, layout-grammar, tests, build-msbuild-template, docs-skills, code-quality, orchestrator

## Baseline gates (task worktree, origin/master `54c07cbc`)

| Gate | Result |
|------|--------|
| `dev build` | 0 warnings / 0 errors |
| `dev test` | pass |
| `dev template-smoke` | pass |
| `dev check-version` | 2.0.0-beta.17 in source vs v2.0.0-beta.16 released — safe |
| `ganda repo audit` | **blocking FAIL** (`kebab-path-names`, see M1); warnings on memsearch `.githooks` scaffold and `peacock.color` |

The migrations themselves verified clean by independent reviewers: zero Fixie/xUnit/NUnit/MSTest/
FluentAssertions references, zero Tailwind/npm/postcss config, zero `Features.Authentication` /
`Features.Account` remnants after the 132-001 fold, all 19 co-located runfiles aggregated, all 18
test `global.json` files identical to the root pin, CPM has no unused or duplicate pins, all
`template.json` paths resolve, all preprocessor regions balanced, all TWA/TWE/SG ids in AGENTS.md
match code, and no stale Design region was found in product code. What remains is a long tail of
cleanliness debt concentrated in **documentation/**, **namespace discipline at the
features/platform boundary**, and **suppression hygiene**.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 13 | 5 | 0 |
| suggestion | 12 | 3 | 0 |
| nit | 2 | 6 | 0 |

## Issues

### A. Gates and repo hygiene

### M1 — Severity: bug — Status: fixed
- File: `kanban/done/205-001-reject-invalid-profile-language-bcp-47--culture-name/`
- Description: Double hyphen in the folder name fails `ganda repo audit` `kebab-path-names`, which is a blocking pre-PR gate for every task in this repo.
- Suggestion: `ganda repo audit --fix --checks kebab-path-names` (rename to `…bcp-47-culture-name`), commit.
- Source: orchestrator
- Disposition notes: fixed on this branch via `ganda repo audit --fix --checks kebab-path-names` (folder renamed; audit passes with 2 advisory warnings).

### M2 — Severity: bug — Status: open
- File: `AGENTS.md:178-179` (Documentation section) vs `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj:26-42`
- Description: AGENTS.md states "generated apps receive the tree in their template output" for `documentation/`, but the packaging csproj's `Content Include` list is `source/**`, `tests/**`, `msbuild/**`, `.template.config/**`, and named root files only. `documentation/`, `skills/`, `AGENTS.md`/`CLAUDE.md`, and `tools/dev-cli` are not packed, so a generated app receives none of them. One of the two is wrong: either the docs are meant to ship (pack them, and then every stale page in section D is a shipped defect) or the claim is stale (fix AGENTS.md and decide whether the dev CLI / docs story for generated apps is "bring your own"). This is a template-coherence decision, not a mechanical fix.
- Suggestion: Decide ship-scope explicitly (recommend: pack `documentation/` after section D is cleaned, and state in AGENTS.md what does and does not ship); record the decision in an ADR or the Documentation section.
- Source: orchestrator
- Disposition notes:

### B. Layout, grammar, namespaces

### M3 — Severity: bug — Status: open
- File: `source/container-apps/api/platform/identity-host/agent-token-authentication-handler-server.cs:16`
- Description: Platform-cluster file declares the product-slice namespace `TimeWarp.Architecture.Features.Identity`. AGENTS.md: "platform clusters keep non-Features namespaces". Its web sibling in `web/platform/identity-host/` correctly uses `…Web.Server`.
- Suggestion: Rename namespace to a platform namespace (e.g. `TimeWarp.Architecture.Api.Server`) or move to an `api/features/identity/` slice if it is really product code.
- Source: layout-grammar
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: `source/container-apps/web/platform/identity-host/agent-caller-permission-scope-source-server.cs:11`
- Description: Platform-cluster file declares the bare Features-substrate namespace `TimeWarp.Architecture.Features`; the substrate tier is documented as features-tree-only and this is the sole `platform/**` file using any Features namespace.
- Suggestion: Move into `web/features/authorization/` beside its consumer `IAgentPermissionScopeSource`, or give it a platform namespace. Decide together with M9.
- Source: layout-grammar
- Disposition notes:

### M5 — Severity: bug — Status: open
- File: `source/container-apps/web/features/todo-items/delete-todo-item/delete-todo-item-contracts.cs:11`; `source/container-apps/web/features/todo-items/todo-item-dto-contracts.cs:15`
- Description: Only file in the slice with a `.Commands` sub-namespace (message-kind grouping, the documented anti-pattern); hand-rolled `GetHttpVerb/GetRoute` instead of `[ApiRoute]`; no `[ClientOnlyContract]` although no server endpoint or handler exists (its siblings `create-todo-item`/`update-todo-item` carry the marker). Sibling DTO file carries a redundant, typo'd `TODO: Revist the Mixins` already answered by its own Design region.
- Suggestion: Drop `.Commands`; either adopt `[ApiRoute]` + `[ClientOnlyContract(reason)]` like its siblings or state in Design why it stays hand-rolled; delete the DTO TODO line.
- Source: layout-grammar, code-quality
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs`
- Description: Real identity/prerender concern sitting at the artifact-folder root; fails the "would it still mean something if the deployable were deleted" litmus. Its named sibling `identity-session-cookie-forwarding-server.cs` lives in `web/platform/identity-host/`.
- Suggestion: Move to `web/platform/identity-host/` (filename already grammar-conformant).
- Source: layout-grammar
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: `source/container-apps/web/projects/web-spa/pages/{AgentLinksPage,ProfilePage,SettingsPage}.razor(.cs)`
- Description: Six files in a project-root `pages/` folder whose own namespaces are `Features.AgentLinks`, `Features.Profiles`, `Features.Applications`; those feature folders already exist and identity/application already keep pages under `features/<slice>/pages/`.
- Suggestion: Move each into its feature's `pages/` folder and delete the root `pages/` folder.
- Source: layout-grammar
- Disposition notes:

### M8 — Severity: suggestion — Status: open
- File: `source/container-apps/web/features/authorization/**` (18 files), `source/container-apps/web/features/payment/**` (2 files)
- Description: Every file in both folders uses the bare Features-substrate namespace; neither has a single slice-scoped file. Structurally they match the skill's own worked example of a platform cluster (seam interface beside implementation, shared by several slices).
- Suggestion: Deliberate call, not a mechanical rename: reclassify as `platform/authorization/` and `platform/payment/` or document why they remain under `features/`. Resolve together with M4.
- Source: layout-grammar
- Disposition notes:

### M9 — Severity: suggestion — Status: open
- File: `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs:170-220`
- Description: TWA0015/0016 only match `/{family}/features/` paths; `platform/` is never function-pair-checked (the membership guard still enforces layer suffixes there). AGENTS.md and the skill describe the full grammar as covering both trees. Scoping is probably deliberate (avoids false positives on ASP.NET `AuthenticationHandler`-named files) but is undocumented.
- Suggestion: Record the features-only scope in the analyzer Design region and the skill, or extend to `platform/` with an allowance for `*-handler-server.cs` when the type derives from `AuthenticationHandler`.
- Source: layout-grammar
- Disposition notes:

### M10 — Severity: nit — Status: open
- File: `AGENTS.md` kebab exception table
- Description: `SKILL.md` and `AnalyzerReleases.{Shipped,Unshipped}.md` are tooling-mandated uppercase basenames not listed in the exception table.
- Suggestion: Add both to the table.
- Source: layout-grammar
- Disposition notes:

### C. Tests

### M11 — Severity: bug — Status: fixed
- File: `tests/foundation/foundation-domain-jaribu-tests/` (`enumeration.cs`, `Directory.Build.props`)
- Description: Dead, never-executed runfile: no csproj, not under `source/`, filename does not match `*-tests.cs`, so neither `dev test` nor any aggregator touches it. Its header claims to be "a Jaribu duplicate of the Fixie suite" for a two-framework worked example, but the sibling `foundation-domain-tests` is itself Jaribu since 145-007, and the test cases are equivalent. Stale Purpose/Design = bug per the context-regions rule.
- Suggestion: Delete the folder.
- Source: tests, leftovers
- Disposition notes: Deleted the folder on 210-001. Sibling `foundation-domain-tests` remains the live Jaribu suite.

### M12 — Severity: bug — Status: open
- File: `source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-contracts.cs`
- Description: Hosted `[ApiEndpoint]` (`GET api/agent-links`) with a handler and zero test coverage anywhere (only SPA client state references it). Definition of Done requires happy-path AND validation-rejection tests for every API endpoint.
- Suggestion: Extend `agent-human-link-tests.cs` (or add a co-located runfile) covering both the human-session and agent-token paths its Design region describes.
- Source: tests
- Disposition notes:

### M13 — Severity: bug — Status: open
- File: `source/container-apps/web/features/admin/roles/update-role/update-role-contracts.cs:34-41`; `…/delete-role/delete-role-contracts.cs:37-42`
- Description: Both validators carry real rules (RoleId NotEmpty, RoleDetailsValidator) but have no rejection tests; only happy-path coverage exists in `roles-endpoint-tests.cs`. `CreateRole` has a dedicated validator test — these two do not.
- Suggestion: Add `update-role-validator-tests.cs` and `delete-role-validator-tests.cs` following the `create-role-validator-tests.cs` pattern.
- Source: tests
- Disposition notes:

### M14 — Severity: suggestion — Status: open
- File: `tests/container-apps/web/web-infrastructure-tests/{ef-principal-role-store-tests,ef-principal-store-contract-tests,profile-postgres-persistence-tests}.cs`
- Description: Three copy-pasted `PostgresAvailability` / `ResolveAvailabilityAsync` / `IsCiEnvironment` helpers with minor shape drift.
- Suggestion: Extract one shared helper into `tests/common/timewarp-testing` (or a shared file in the suite).
- Source: tests
- Disposition notes:

### M15 — Severity: suggestion — Status: open
- File: `source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs:22,84`
- Description: Namespace `TimeWarp.Architecture.Task205` is named after a kanban task; it ships in the product tree and means nothing outside this repo's history.
- Suggestion: Rename to a behavior-based namespace (e.g. `…Features.Identity.ProgressiveProfileGate`).
- Source: tests
- Disposition notes:

### M16 — Severity: suggestion — Status: fixed
- File: `timewarp-architecture.slnx`; `tests/tools/agent-identity-cli-tests/agent-identity-cli-tests.csproj`; `source/container-apps/web/projects/web-spa/web-spa.csproj`
- Description: `web-spa.csproj` (the SPA product project, built only transitively via web-server's ProjectReference) and `agent-identity-cli-tests` are absent from the solution with no documented rationale, unlike the JARIBU_MULTI aggregators and `timewarp-testing-tests`, which carry "intentionally not in .slnx" comments. `dev build` (slnx) therefore never compiles `agent-identity-cli-tests` under the 0/0 gate; IDEs never show web-spa.
- Suggestion: Add web-spa under the `(web)` block and agent-identity-cli-tests under a new `/tests/tools/` folder; or add the same one-line exclusion comment the aggregators carry.
- Source: build-msbuild-template, tests
- Disposition notes: Added `web-spa` under the `(web)` block. Added `/tests/tools/` with `agent-identity-cli-tests`, gated by `#if (false)` so generated apps do not list a project `template.json` excludes.

### M17 — Severity: nit — Status: open
- File: `tests/container-apps/web/web-server-integration-tests/features/identity/{agent-registration-tests.cs:9-13, agent-protected-endpoint-tests.cs:12, passkey-registration-tests.cs:15, infrastructure/integration-software-agent-key.cs:16}`
- Description: Design regions label current per-class shared-state behavior as "Fixie per-class fixture sharing" as if Fixie were still the active framework. Behavior described is correct Jaribu C-create.
- Suggestion: Reword to describe the pattern generically; keep the historical note past-tense.
- Source: tests
- Disposition notes:

### D. Documentation, skills, AGENTS.md

### M18 — Severity: bug — Status: fixed
- File: `AGENTS.md:25,72,88,110,143,162`
- Description: Six skill references use bare names that do not match the registered skills: `dev-cli`→`tw-dev-cli`, `blazor-css-strategy`→`tw-blazor-css-strategy`, `feature-placement`→`tw-feature-placement`, `web-api-contracts`→`tw-web-api-contracts`, `slice-isolation`→`tw-slice-isolation`, `agent-context-regions`→`tw-agent-context-regions`. Agents invoking by the given name fail. Same file uses the `tw-` names correctly elsewhere.
- Suggestion: Prefix all six with `tw-`. Also fix the stale diagram annotation at `AGENTS.md:150` ("api platform/ tree absent") — `api/platform/identity-host/` now has 5 files — and mention the fourth dual-mode switch `UseX402Packages` alongside the three listed.
- Source: docs-skills, build-msbuild-template
- Disposition notes: Fixed on 210-004. All six skill names prefixed; api `platform/identity-host/` annotation; `UseX402Packages` listed with the other dual-mode switches.

### M19 — Severity: bug — Status: fixed
- File: `kanban/overview.md` (whole file); `kanban/task-template.md:9`; `scripts/get-next-task-number.ps1`
- Description: Describes hand-numbered ids, `B001_…`/`001_…` underscore filenames, and PascalCase folders (`Backlog`, `ToDo`, `InProgress`) — directly contradicting AGENTS.md's "never hand-number, always `ganda kanban create`" and the actual kebab folders. The companion script exists solely to compute hand-assigned numbers.
- Suggestion: Rewrite `kanban/overview.md` to the `ganda kanban` workflow (or reduce it to a pointer at AGENTS.md §Task management + `tw-kanban`); fix the template example; delete `get-next-task-number.ps1`.
- Source: docs-skills
- Disposition notes: Fixed on 210-004. `kanban/overview.md` now points at AGENTS.md Task management + `tw-kanban`; task-template parent example uses a numeric id; `scripts/get-next-task-number.ps1` deleted (overview + `profile.ps1` source removed).

### M20 — Severity: bug — Status: open
- File: `documentation/developer/how-to-guides/testing/how-to-add-lifecycles-to-tests.md:7`
- Description: Links to a non-existent PascalCase-era path (`Tests/TimeWarp.Architecture.Testing/ConventionTests/LifecycleExamples.cs`) and teaches `Setup`/`Cleanup` method names; current Jaribu convention is `SetupOnce`/`CleanUpOnce`.
- Suggestion: Rewrite against `SetupOnce`/`CleanUpOnce` with a real exemplar, or delete in favor of the `tw-jaribu` / `tw-feature-placement` runfile references.
- Source: leftovers, docs-skills
- Disposition notes:

### M21 — Severity: bug — Status: open
- File: `documentation/developer/conceptual/architectural-decision-records/project-structure-and-conventions/` (whole orphaned subfolder); `…/architectural-decision-records/proposed/xxx.md`
- Description: Documents the opposite of the approved architecture (one PascalCase file per class, plain `ProblemDetails`, `<Container>.<Feature>.<Entity>` namespaces) with no inbound links. `proposed/xxx.md` duplicates the same stale convention under a placeholder filename.
- Suggestion: Delete both (superseded by ADR-0008 + `tw-web-api-contracts`), or move under a clearly marked `rejected/` with a one-line superseded-by note.
- Source: docs-skills
- Disposition notes:

### M22 — Severity: bug — Status: open
- File: `documentation/developer/conceptual/features/application/is-processing.md`
- Description: Orphaned page citing Windows-backslash `\Source\Client\…` paths and an `ApplicationState.cs` that does not exist anywhere.
- Suggestion: Delete, or rewrite against the current web-spa layout and TimeWarp.State if `IsProcessing` is still a live concept.
- Source: docs-skills
- Disposition notes:

### M23 — Severity: bug — Status: open
- File: `documentation/developer/conceptual/component-naming-and-organization.md:~150-255`
- Description: Example tree uses PascalCase folders (`Components/`, `Editors/`, `Pages/`, `Features/`), a non-existent `editors/` folder, and a two-layout example (`AltLayout.razor`) contradicting the single-shell pattern in `tw-blazor-layout`.
- Suggestion: Rewrite the tree in kebab folders (Razor filenames stay PascalCase) and replace the two-layout example with the shell pattern, cross-referencing `tw-blazor-layout`.
- Source: docs-skills
- Disposition notes:

### M24 — Severity: bug — Status: open
- File: `documentation/developer/conceptual/architectural-decision-records/overview.md`
- Description: Top-level ADR index is unedited MADR boilerplate linking only the tool's `examples/`; none of approved 0001–0010 are linked.
- Suggestion: Replace with a real index linking `approved/overview.md` and `proposed/`.
- Source: docs-skills
- Disposition notes:

### M25 — Severity: bug — Status: open
- File: `documentation/developer/conceptual/architectural-decision-records/approved/0003-endpoint-centric-api-with-interface-based-validation.md:64`; `documentation/developer/reference/dotnet-conventions.md:4`
- Description: Broken relative link (`../api-design.md` should be `../../api-design.md`); "Target net9.0" while the repo targets net10.0.
- Suggestion: Fix the link; update to net10.0.
- Source: docs-skills
- Disposition notes:

### M26 — Severity: bug — Status: fixed
- File: `skills/tw-mock-response-factory/SKILL.md:4`
- Description: Frontmatter trigger list includes `MockCopicApiService` — a client name that also does not exist in this repo. Skills publish publicly; no client names allowed.
- Suggestion: Remove the token (`MockWebApiService` is already in the list).
- Source: docs-skills
- Disposition notes: Fixed on 210-004. `MockCopicApiService` removed from `when-to-use`; `MockWebApiService` remains.

### M27 — Severity: suggestion — Status: open
- File: `documentation/overview.md`; `documentation/roadmap.md`; `documentation/developer/overview.md`; `documentation/developer/tutorials/overview.md`; `documentation/developer/conceptual/testing/overview.md`; `documentation/developer/conceptual/features/overview.md`; `…/architectural-decision-records/proposed/overview.md`; `…/conceptual/testing/end-to-end-testing.md`; `…/how-to-guides/testing/how-to-write-endpoint-test.md`; `…/proposed/xxxx-powershell-coding-standards.md`
- Description: `documentation/overview.md` is unedited boilerplate ("TODO: Give a short introduction", `https://todo/your-docs`, dotnet-core 3.0 link, non-existent `CONTRIBUTING.md`). Nine further pages are empty or one-line stubs; `xxxx-powershell-coding-standards.md` is a placeholder-named non-ADR fragment.
- Suggestion: One pass: write real content where the slot earns it, delete the rest; `proposed/` must not be a permanent parking lot.
- Source: docs-skills
- Disposition notes:

### M28 — Severity: suggestion — Status: open
- File: `runfiles/overview.md`; `readme.md:1,4`
- Description: `runfiles/overview.md` cites a `build.cs` that does not exist (directory holds only the overview). `readme.md` badges: `dotnet-6.0`, and a workflow badge pointing at `blazor-state/…/release-build.yml`, which does not exist for this repo.
- Suggestion: Reword the runfiles page (or delete until populated); point badges at this repo's `workflow.yml` and current .NET, or drop them.
- Source: docs-skills
- Disposition notes:

### M29 — Severity: nit — Status: fixed
- File: `skills/tw-web-api-contracts/analysis/{composer-skill-analysis,glm52-review}.md`
- Description: Contain a client name and past-tense review narrative. `analysis/` is excluded from public sync by convention, but nothing in-repo records that exclusion.
- Suggestion: Add a one-line marker in `analysis/` (or the skill) stating it is excluded from publication.
- Source: docs-skills
- Disposition notes: Fixed on 210-004. `skills/tw-web-api-contracts/analysis/readme.md` records exclusion from publication (`skills-are-public-no-history`).

### E. Code quality, suppressions, dead code

### M30 — Severity: bug — Status: open
- File: `tools/dev-cli/endpoints/verify-samples-command.cs:20-26`
- Description: Prints "Samples verified successfully!" around a bare TODO; nothing is verified, exit code 0.
- Suggestion: Implement, or make it an honest stub (error line + non-zero exit) — or delete the endpoint if `samples/` stays empty.
- Source: code-quality
- Disposition notes:

### M31 — Severity: suggestion — Status: fixed
- File: `source/container-apps/web/projects/web-spa/pipeline/my-behavior.cs`; `source/container-apps/web/projects/web-spa/global-suppressions.cs:10`
- Description: `MyBehavior<,>` is a placeholder-named, unregistered pipeline behavior (program.cs wires only `ActiveActionBehavior` and `EventStreamBehavior`) — dead code shipped to every generated app, plus a suppression that exists only for it.
- Suggestion: Delete both (the two live behaviors already teach the pattern).
- Source: code-quality
- Disposition notes: Deleted `my-behavior.cs` and its CA1720 suppression. Live `ActiveActionBehavior` / `EventStreamBehavior` remain.

### M32 — Severity: suggestion — Status: open
- File: `source/container-apps/Directory.Build.props:8`; `source/foundation/Directory.Build.props:4-7`; `source/container-apps/grpc/projects/grpc-server/grpc-server.csproj:5`; `source/container-apps/web/projects/web-server/web-server.csproj:23`
- Description: A 30-id uncommented `<NoWarn>` on container-apps, a 20-id foundation `<NoWarn>` justified only as "existed in original code" (ships in published `TimeWarp.Foundation.*`), and two uncommented per-project NoWarns. `tests/Directory.Build.props` is the exemplar (every id justified).
- Suggestion: Audit each id against current code; keep only what is needed, one-line reason per id or group, in the tests/ style.
- Source: code-quality
- Disposition notes:

### M33 — Severity: suggestion — Status: open
- File: `source/container-apps/web/projects/web-spa/global-suppressions.cs:11-13`
- Description: Three `SuppressMessage` entries with `Justification = "<Pending>"` (CA1052 on Program, CA2000 on Program.Main, CA1720 on EventStreamBehavior.Guid).
- Suggestion: Write real justifications or fix the underlying warnings and delete.
- Source: code-quality
- Disposition notes:

### M34 — Severity: suggestion — Status: open
- File: `source/container-apps/grpc/projects/grpc-server/program.cs:46-56`
- Description: Hand-rolled CORS duplicating `CorsPolicy.AnyPolicy` solely to add gRPC exposed headers.
- Suggestion: Extend the foundation policy (overload accepting exposed headers) and consume it like web/api do.
- Source: code-quality
- Disposition notes:

### M35 — Severity: suggestion — Status: fixed
- File: `source/container-apps/web/projects/web-spa/features/developer/components/user-claims-base.cs:5`
- Description: `TODO [2026-06]: Reassess UserClaimsBase …` is past its own checkpoint date with the decision (delete / integrate / leave) still "pending"; the file is an `#if false` sketch.
- Suggestion: Decide now — most likely delete — or open a kanban task and reference it from the file.
- Source: code-quality
- Disposition notes: Deleted. Nothing inherited it; `UserClaims.razor` already owns the live claims display.

### M36 — Severity: suggestion — Status: open
- File: `source/container-apps/web/projects/web-spa/features/profile-menu/profile-menu-state/profile-menu-state.toggle.cs:29`
- Description: "Transitions and NotifyLossOfInterest not working" — a known missing UX behavior tracked only by an inline TODO.
- Suggestion: Open a kanban task and point the Design region at it, or fix it.
- Source: code-quality
- Disposition notes:

### M37 — Severity: nit — Status: fixed
- File: `source/container-apps/web/projects/web-spa/components/pages/SideNavigationLink.razor:5,9`
- Description: Two stale TODOs: one naming a person and a source generator that has since shipped, one "Add Bootstrap classes" (Bootstrap and Tailwind both retired).
- Suggestion: Delete both lines.
- Source: code-quality
- Disposition notes: Deleted both TODO lines and the unused `[TwParentComponent]` / `[TwAttributeComponent]` comment stubs.

### M38 — Severity: nit — Status: fixed
- File: `source/foundation/foundation-application/abstractions/i-current-user-service.cs:9`
- Description: `TODO: Should this be a strongly typed UserId?` is a design question, not a work item.
- Suggestion: Move into `#region Open Questions` (or answer it: the repo already has TypedId infrastructure).
- Source: code-quality
- Disposition notes: Moved to `#region Open Questions` as Q1. Left `Guid?` in place — changing it is a TypedId fold across CurrentUserService, IAuthApiRequest, and the contracts generator, not this cleanup.

### M39 — Severity: nit — Status: fixed
- File: `tests/common/timewarp-testing/scoped-sender.cs:1`; `tests/common/timewarp-testing/global-suppressions.cs:1,6-7`
- Description: No `#region Purpose` (TWA0004 is off for tests/, but the rest of this project carries them); the IDE0052 justification "Construction the item will start it" is typo'd and terse.
- Suggestion: Add Purpose lines; tighten the justification.
- Source: code-quality
- Disposition notes: Added Purpose regions. IDE0052 justification is now "Constructor assignment starts the nested test server; the field is otherwise unread."

### M40 — Severity: nit — Status: fixed
- File: `.github/workflows/workflow.yml:25,51,110`; `timewarp-templates/version.json`
- Description: Path filters name a `Directory.Version.props` that does not exist; `version.json` is an unused Nerdbank.GitVersioning file stuck at `1.0-beta` while the real version is 2.0.0-beta.17.
- Suggestion: Remove the dead filter entries; delete `version.json`.
- Source: build-msbuild-template
- Disposition notes: Removed the three `Directory.Version.props` path-filter entries; deleted unused `timewarp-templates/version.json`.

### M41 — Severity: nit — Status: fixed
- File: `source/analyzers/timewarp-architecture-analyzers/helpers/string-extensions.cs:24`; `source/foundation/foundation-domain/entities/base/i-aggregate-root.cs:48-50`
- Description: Two `#pragma warning disable` lines are redundant: CA1308 is already `none` repo-wide in `.editorconfig:298`, and CA1040 is already in `source/foundation/Directory.Build.props` `<NoWarn>`.
- Suggestion: Delete both pragmas (or, if the project-wide suppression is trimmed under M32, keep the local one and drop the global).
- Source: code-quality
- Disposition notes: Deleted both local pragmas. 210-006/M32 still owns the project-wide `<NoWarn>` audit; if CA1040 is later trimmed there, restore a local pragma on `IAggregateRoot`.

## Duplicates / conflicts

- `foundation-domain-jaribu-tests`: leftovers (suggestion) + tests (bug) → M11 at **bug** (stale Purpose region + never executed).
- Lifecycle how-to: leftovers + docs-skills → M20.
- `.slnx` omissions: build (2 suggestions) + tests (nit) → M16 at suggestion.
- `delete-todo-item` namespace/routing (layout) + DTO TODO (code-quality) → M5.
- `MyBehavior` dead type + its CA1720 suppression → M31.
- `Directory.Version.props` + `version.json` (both build nits) → M40.
- `scripts/*.ps1` still present (leftovers I3): already owned by open task 061 — **not carried** as a finding; `get-next-task-number.ps1` is folded into M19 because it encodes the forbidden workflow.
- `constants.cs` grab-bag (layout nit) — **not carried**: one constant, no action.
- Reviewer severity was recalibrated where the "ships to every generated app" premise was wrong: `documentation/` does not currently ship (M2), so readme badges and `runfiles/overview.md` are carried as suggestion (M28) rather than bug. Docs that give agents wrong instructions or cite non-existent paths stay at bug because agents act on them in this repo.

## Suggested fix dispatch (child tasks under 210)

| Bundle | Findings | Nature |
|--------|----------|--------|
| 210-001 dead files + stale TODOs | M11, M31, M35, M37, M38, M39, M40, M41 | mechanical deletes/renames, one commit |
| 210-002 namespace and placement at the features/platform boundary | M3, M4, M5, M6, M7, M8, M9, M10 | needs one placement decision (M8) before the moves |
| 210-003 missing endpoint tests | M12, M13, M14, M15 | new Jaribu tests + one helper extraction |
| 210-004 AGENTS.md + kanban docs + skills | M18, M19, M26, M29 | agent-facing docs; highest leverage |
| 210-005 documentation/ purge and ship-scope decision | M2, M20–M25, M27, M28 | decide ship-scope first, then delete/rewrite |
| 210-006 suppression hygiene + dev-cli honesty | M30, M32, M33, M34, M36 | per-id NoWarn audit, CORS overload |
| — | M16 | small; fold into 210-001 or 210-004 |
