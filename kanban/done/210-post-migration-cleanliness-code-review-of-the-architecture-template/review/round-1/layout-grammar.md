# Round 1 — layout-grammar
**Date:** 2026-09-09
**Scope reviewed:** All `.cs` under `source/container-apps/{web,api,grpc}/features/**` and
`platform/**` (226 files) against the filename grammar and namespace rules; every artifact
folder under `{web,api,grpc}/projects/*` (build output excluded) against the "definition +
bootstrap only" litmus test; `web-spa` internal structure (features/ vs components/ vs pages/ vs
services/ vs mixins/ vs source/); kebab-case naming across `source/`, `tests/`, `tools/`,
`msbuild/`, `documentation/`, `skills/`; namespaces in `platform/**` and the Features-substrate
tier's Design-region discipline; the repo's single `#region Open Questions`; stale Fixie/Tailwind/
old-Authentication-feature mentions in Design regions; test-tree vs source-tree mirroring; and
family symmetry (web/api/grpc `msbuild/` shape, generated `.g.props` vs the JSON registry).

## Summary
The rehome is largely clean: every `features/` file pairs its function/layer suffix correctly,
EF migrations are explicitly and documentedly excluded from the grammar, artifact folders hold
only definition/bootstrap content, kebab-case is enforced with no unexplained exceptions, and the
three families' `msbuild/` trees are byte-for-byte symmetric (mod family prefix) and in sync with
the JSON registry. The real findings cluster around **namespace discipline at the features/platform
boundary**: one platform file borrows a product-slice namespace, one borrows the bare Features-
substrate namespace from outside its home tree, and one long-standing `todo-items` contract still
carries a pre-migration `.Commands` sub-namespace and hand-rolled routing. A few files also read as
migration stragglers sitting in the wrong tree (a web-server identity concern, three SPA pages).

## Issues

### Issue 1 — Severity: bug
- File: `source/container-apps/api/platform/identity-host/agent-token-authentication-handler-server.cs:16`
- Description: Namespace is `TimeWarp.Architecture.Features.Identity` — a product-slice namespace
  — even though the file lives under `platform/identity-host/`, a platform cluster. AGENTS.md
  states plainly that "platform clusters keep non-Features namespaces" and the
  `tw-feature-placement` skill's placement table lists platform namespaces as "Non-Features (e.g.
  Configuration, Services)". The file's own Design region says it is "intentional parity with
  web's AgentTokenAuthenticationHandler (`web/features/identity/agent-token-authentication-scheme-server.cs`)"
  — but that web file lives in the **features** tree (where `Features.Identity` is correct), while
  this api file was placed in **platform** and kept the same namespace unchanged. The sibling web
  platform file for the same concern shape, `web/platform/identity-host/mock-identity-principal-handler-server.cs`,
  correctly uses `TimeWarp.Architecture.Web.Server`.
- Suggestion: Change the namespace to a non-Features platform namespace (e.g.
  `TimeWarp.Architecture.Api.Server`, matching the web sibling's `Web.Server` convention), or move
  the file into an `api/features/identity/` slice if the intent is really product-slice code (no
  such slice exists in api today).
- Status: open

### Issue 2 — Severity: bug
- File: `source/container-apps/web/platform/identity-host/agent-caller-permission-scope-source-server.cs:11`
- Description: Namespace is bare `TimeWarp.Architecture.Features` (the Features-substrate tier)
  even though the file lives under `platform/identity-host/`. The `tw-feature-placement` skill
  defines the Features-substrate tier strictly as a **product-tree** tier ("file still lives under
  a folder for humans (e.g. `features/admin/roles/role-ids-contracts.cs`...)") and AGENTS.md's
  platform-namespace rule makes no substrate exception. Every other file in `platform/` uses a
  non-Features namespace (`Configuration`, `Services`, `Abstractions`, `Persistence`, `Abuse`,
  `AgentDiscovery`, `HostedServices`); this is the sole `platform/**` file using any `Features`
  namespace, bare or scoped.
- Suggestion: Either move this file into `web/features/authorization/` (where its consumer,
  `IAgentPermissionScopeSource`, already lives — see Issue 6) so the substrate namespace matches
  its documented "features substrate, not a product slice" tier, or rename its namespace to a
  platform one (e.g. `TimeWarp.Architecture.Services`, matching its `platform/identity-host/`
  siblings) if it is meant to stay physically in the platform cluster.
- Status: open

### Issue 3 — Severity: bug
- File: `source/container-apps/web/features/todo-items/delete-todo-item/delete-todo-item-contracts.cs:11`
- Description: Namespace is `TimeWarp.Architecture.Features.TodoItems.Commands` — the only file in
  the `todo-items` slice using a `.Commands` sub-namespace; every sibling contract
  (`create-todo-item`, `update-todo-item`, `get-todo-item-by-id`, `search-todo-items`,
  `todo-item-dto-contracts.cs`) uses the bare slice namespace `TimeWarp.Architecture.Features.TodoItems`.
  Grouping by message kind (commands/queries) is the exact anti-pattern the feature-placement
  skill calls out for folders ("commands/ and queries/ subfolders... do not appear inside a
  slice — grouping by message kind is a layer instinct") and the same instinct has leaked into
  this one namespace. The file also implements `IApiRequest.GetHttpVerb()/GetRoute()` by hand
  instead of `[ApiRoute]` (its own Design region calls this out as "documenting the manual
  IApiRequest alternative"), and — unlike `create-todo-item`/`update-todo-item`, which are
  `[ClientOnlyContract]` — carries no `[ClientOnlyContract]` marker despite there being no server
  endpoint or handler anywhere in the repo for `DeleteTodoItem`. `git log` traces this file back to
  the 126-001 per-use-case-folder migration, predating the endpoint-centric contract convention
  reaching this slice.
- Suggestion: Drop the `.Commands` segment to match the slice's bare namespace, and bring the
  contract in line with its siblings — either add `[ApiRoute]` + `[ClientOnlyContract(reason)]`
  (matching `create-todo-item`), or add the reason it stays hand-rolled to the Design region.
- Status: open

### Issue 4 — Severity: suggestion
- File: `source/container-apps/web/projects/web-server/hosted-identity-session-authentication-state-provider-server.cs`
- Description: This file sits at the `web-server` artifact-folder root, but per the litmus test
  ("if this deployable were deleted, would the file still mean something?") it is a real identity/
  prerender concern (task 183/205-003, documented at length in its own Design region), not entry-
  point bootstrap. Its closest sibling in shape and topic,
  `web/platform/identity-host/identity-session-cookie-forwarding-server.cs` (referenced by name in
  this file's own Design region), lives in the `platform/identity-host/` cluster instead.
- Suggestion: Move to `web/platform/identity-host/hosted-identity-session-authentication-state-provider-server.cs`
  for consistency with the rest of the identity-host cluster; no filename change needed since it
  already carries the `-server.cs` layer suffix the platform tree expects.
- Status: open

### Issue 5 — Severity: suggestion
- File: `source/container-apps/web/projects/web-spa/pages/AgentLinksPage.razor`,
  `AgentLinksPage.razor.cs`, `ProfilePage.razor`, `ProfilePage.razor.cs`, `SettingsPage.razor`,
  `SettingsPage.razor.cs` (repo-relative under `web-spa/pages/`)
- Description: These six files sit in a project-root `pages/` folder, but each one's own
  `@namespace`/namespace already names its owning feature —
  `TimeWarp.Architecture.Features.AgentLinks`, `Features.Profiles`, and `Features.Applications`
  respectively — and each corresponding feature folder already exists
  (`features/agent-links/`, `features/profiles/`, `features/application/`). The SPA convention
  (SPA exception in `tw-feature-placement`, "conventionally organized... one folder per slice") is
  already followed elsewhere: `features/identity/pages/` holds `LoginPage`, `PasskeysPage`,
  `LogoutPage`, `Authentication.razor`, `RedirectToLogin.razor`, and `features/application/pages/`
  already holds `HomePage.razor`. These three pages look like stragglers that were never moved
  into their feature folders during an earlier SPA reorganization.
- Suggestion: Move `AgentLinksPage.*` into `features/agent-links/pages/`, `ProfilePage.*` into
  `features/profiles/pages/`, and `SettingsPage.*` into `features/application/pages/` (alongside
  `HomePage.razor`), matching the pattern already used for identity's pages.
- Status: open

### Issue 6 — Severity: suggestion
- File: `source/container-apps/web/features/authorization/**` (18 files), `source/container-apps/web/features/payment/**` (2 files)
- Description: Every file in these two folders uses the bare Features-substrate namespace
  (`TimeWarp.Architecture.Features`) — neither folder has a single file using a scoped
  `Features.Authorization`/`Features.Payment` namespace. Each file's Design region correctly and
  consistently documents why (shared across Identity, Admin.Principals, Tip, MeteredCapability
  without tripping TWA0009), so this is not a rule violation. But structurally these two folders
  now look identical in shape to the skill's own worked example of a **platform cluster**:
  `payment/i-payment-http-context-application.cs` + `http-payment-http-context-server.cs` is the
  same "seam interface beside its implementation, non-Features namespace, shared by multiple
  slices" pattern the skill cites verbatim for
  `platform/identity-host/i-current-principal-accessor-application.cs` +
  `http-current-principal-accessor-server.cs`.
- Suggestion: Worth a deliberate call (not a mechanical rename) on whether `authorization/` and
  `payment/` should be reclassified as `platform/authorization/` and `platform/postgres`-style
  platform clusters rather than living under `features/` as 100%-substrate "slices" that never
  actually use a slice namespace.
- Status: open

### Issue 7 — Severity: suggestion
- File: `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs:170-220`
- Description: `TryGetCohesiveFeatureRelativePath` only recognizes `/{family}/features/` path
  markers — it never matches `/{family}/platform/` — so `TWA0015`/`TWA0016` (function/layer
  pairing) never analyze any file under `platform/`. The membership guard
  (`feature-membership.targets`) *does* cover `platform/` (any registered layer suffix is
  accepted), but the stricter function-pairing check does not. This is what lets
  `agent-token-authentication-handler-server.cs` and `mock-identity-principal-handler-server.cs`
  (real ASP.NET `AuthenticationHandler` classes, not mediator handlers) exist without a false
  TWA0015 — which is almost certainly the reason for the scoping — but it also means a genuine
  TWA0015-shaped mistake under `platform/` (e.g. a real mediator handler wrongly suffixed
  `-server.cs`) would compile silently. AGENTS.md and the skill's own opening paragraph describe
  the full `<name>[-<function>]-<layer>.cs` grammar, function segment included, as covering both
  `features/` and `platform/` clusters, which reads as broader than what the analyzer actually
  enforces.
- Suggestion: Not a build-breaking bug today (zero false positives, zero known false negatives
  found in this pass), but worth a one-line note in the skill or analyzer Design region making the
  `features/`-only scope of TWA0015/16 explicit, so a future "let's also analyze platform/" change
  doesn't get merged without accounting for `AuthenticationHandler`-named files.
- Status: open

### Issue 8 — Severity: nit
- File: `source/container-apps/web/projects/web-spa/constants.cs`
- Description: Purpose region self-describes as "SPA-local constants that have no home elsewhere" —
  a mild grab-bag admission, though at one constant (`OperationCancelled`) it is harmless today.
- Suggestion: No action needed at this size; revisit if more unrelated constants accumulate here.
- Status: open

### Issue 9 — Severity: nit
- File: n/a (documentation gap)
- Description: `SKILL.md` (repo-wide, e.g. `skills/tw-feature-placement/SKILL.md`) and
  `AnalyzerReleases.{Shipped,Unshipped}.md` are uppercase basenames required by their respective
  tooling (Claude Code skill loader; Roslyn analyzer release-tracking convention), but neither is
  listed in AGENTS.md's kebab-case exception table (`.razor`, MSBuild well-known files,
  `Properties/`, `launchSettings.json`, `appsettings.*.json`, `_Imports.razor`, `App.razor`).
- Suggestion: Add both to the exception table so a future contributor doesn't "fix" the casing and
  break skill discovery or analyzer release tracking.
- Status: open

## Checked clean
- Every `.cs` under `web/api/grpc features/**` matches `<name>[-<function>]-<layer>.cs` with a
  registered layer, and every `-handler-` file correctly pairs with `-application` (37 files
  checked; zero mismatches in `features/`).
- EF Core migrations under `web/platform/postgres/migrations/**` are explicitly excluded from the
  filename-grammar membership guard, with an inline comment explaining why (EF tooling owns those
  basenames) and a separate explicit `Compile Include` wiring them into `web-infrastructure`.
- Artifact folders (`web-contracts/`, `web-application/`, `web-domain/`, `web-infrastructure/`,
  `api-*`, `grpc-*`) hold only their csproj + `global-usings.cs` (or, for `*-server` projects,
  program.cs/appsettings/launchSettings/components bootstrap) — no stray product logic found.
  `api-server/generic-pipeline-behavior.cs` is a deliberate, self-documented bootstrap exemplar
  ("Lives in the api-server artifact folder as host bootstrap exemplar; move under features/ or
  platform/ if it grows real product logic") — exactly the pattern the skill's `sample-options.cs`
  exemplar describes.
- Use-case-folder rule (every operation gets its own `<slice>/<use-case>/` folder; shared files
  stay at slice root) is followed consistently across every web/api/grpc feature slice inspected.
- Kebab-case is enforced across `source/`, `tests/`, `tools/`, `msbuild/`, `documentation/`,
  `skills/` with no violations beyond the documented `.razor`/`.razor.cs` Blazor exception and the
  two tooling-mandated uppercase names noted in Issue 9.
- Features-substrate files (`admin/principals/*`, `authorization/*`, `payment/*`) that use the
  bare `Features` namespace all carry a `#region Design` explicitly justifying the substrate
  choice and naming which slices share the type, per the skill's documentation requirement.
- The repo's single `#region Open Questions`
  (`web/features/identity/revoke-credential/revoke-credential-handler-application.cs`) is a
  genuinely conditional, forward-looking question ("revisit if/when" a new caller is proposed),
  not a stale question with an available answer.
- No stale Design-region references treating Fixie, Tailwind, or the old "Authentication" feature
  as current were found in `features/`, `platform/`, `tests/common/timewarp-testing/`, or
  `web-spa/features/identity/`; the handful of Fixie mentions found are correctly past-tense
  ("Replaces the Fixie...", "the old Fixie-era suite").
- `grpc` has no `tests/container-apps/grpc/` tree and no `-tests.cs` runfiles under
  `source/container-apps/grpc/`, consistent with the documented "grpc when it gains runfiles" note
  — nothing to aggregate yet.
- Family symmetry: `web/api/grpc` each import `msbuild/feature-membership.targets` once from their
  own `Directory.Build.targets`, and all three families' `feature-filename-grammar.g.props` are
  identical modulo family-prefix substitution and match the current
  `feature-filename-grammar.json` registry's layers (`contracts, application, domain,
  infrastructure, server, tests`) and functions (`handler→application, endpoint→server`) exactly —
  no stale/drifted generated files found.
