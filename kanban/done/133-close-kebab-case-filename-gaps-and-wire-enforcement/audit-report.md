# Kebab-case filename convention audit

> **Disposition (task 133):** PascalCase **`.razor` / `.razor.cs` / `.razor.css` are an explicit
> exception** (Blazor requires type-matching names). SourceGenerators `FileNameRuleAnalyzer`
> already excludes `*.razor.cs`. Treat F2 SPA razor findings as **policy-compliant**, not debt.
> Real work: integration-test kebab migration, docs, pure-`.cs` outliers, and wiring/fixing
> the existing analyzer (see `task.md` / `research-notes.md`).

## Executive Summary

Product **source** trees that use the axis-1 grammar (`source/container-apps/{web,api,grpc}/{features,platform}/`, foundation, analyzers `.cs`) are **fully kebab-compliant**. After the **razor exception**, remaining debt is mainly **integration test projects** still on Pascal `Features/` + `*_Tests.cs`, **documentation** (`HowTo*.md`, Pascal titles), and **static assets / ASP.NET host folders** (`Properties/`, logo names). No violations were found under the shared family `features/` / `platform/` trees outside `web-spa`.

## Scope

| In scope | Out of scope / treated as non-product noise |
|----------|-----------------------------------------------|
| `source/`, `tests/`, `documentation/`, `timewarp-templates/`, `msbuild/`, `skills/` | `bin/`, `obj/`, `.git/`, generated build outputs |
| File **and** directory basenames | Type/namespace PascalCase (language rule; intentionally separate) |
| Repo convention: kebab-case paths everywhere | Kanban task slugs that embed `--` (task-id grammar, not product paths) |

**Convention sources:**

- `AGENTS.md` — “kebab-case paths everywhere; namespaces PascalCase”
- `tw-csharp` skill — kebab files/folders/docs; exceptions for MSBuild well-known names
- Axis-1 grammar (`feature-filename-grammar.json`, TWA0015/0016) — kebab `<name>[-function]-layer.cs` under family `features/` / `platform/`
- Partial secondary files — multi-dot kebab segments (e.g. `application-state.close-modal.cs`) treated as compliant

**Documented / practical exceptions applied during the scan:**

| Exception | Reason |
|-----------|--------|
| `Directory.Build.props` / `.targets`, `Directory.Packages.props`, `BannedSymbols.txt` | MSBuild / tooling well-known casing |
| `appsettings*.json` (incl. `Development`, `Production`, `Kubernetes_Docker`) | ASP.NET environment naming |
| `Properties/launchSettings.json` | ASP.NET project system folder |
| `_Imports.razor` / `_imports.razor`, `App.razor` | Blazor host conventions |
| `AnalyzerReleases.Shipped.md` / `Unshipped.md` | Roslyn analyzer release-tracking convention |
| `AGENTS.md`, `CLAUDE.md`, `SKILL.md`, `README.md`, … | Root/tool entry docs |

Note: skill text references ADR-0013 for kebab adoption; that ADR path is **not present** under `documentation/.../architectural-decision-records/` in this worktree (only older structure docs remain).

## Methodology

1. Walked scoped trees and classified each **basename** against a kebab rule:
   - one or more lowercase alphanumeric segments separated by `-`
   - `.` allowed between kebab segments (partials) and for extensions  
     (e.g. `role-state.create-role.cs`, `simple-alert.razor.css`)
2. Applied the exception table above.
3. Separated **axis-1 product trees** from **SPA artifact**, **tests**, **docs**, and **assets**.
4. Spot-checked compliant trees (`web-contracts-tests`, foundation, analyzers `.cs`) as positive controls.

**Scan scale (after exceptions):** ~937 files in scoped trees → **207 basename violations**; ~350 dirs → **34 directory basename violations**.

## Findings

### F1 — Axis-1 product trees are clean (priority: none)

| Tree | Non-compliant basenames |
|------|-------------------------|
| `source/container-apps/{web,api,grpc}/features/` | **0** |
| `source/container-apps/{web,api,grpc}/platform/` | **0** |
| `source/foundation/` | **0** |
| `source/analyzers/**/*.cs` | **0** |
| `source/**/*.cs` excluding `web-spa` | **264 / 264** compliant |

**Conclusion:** The compiler-enforced feature grammar and the monorepo’s backend/server layer migration to kebab are in good shape.

---

### F2 — Web SPA: PascalCase components and pages (priority: high for convention debt)

**Count:** ~90 `.razor` / `.razor.cs` / `.razor.css` basenames + 1 pure `.cs` under  
`source/container-apps/web/projects/web-spa/`.

**Pattern:** Blazor types use PascalCase filenames that mirror the component type (`HomePage.razor`, `ModalContainer.razor`, `Counter.razor`), while folders and TimeWarp.State partials are already kebab (`features/counter/`, `counter-state.increment-counter.cs`).

**Representative paths:**

| Area | Examples |
|------|----------|
| Shared components | `components/NavMenu.razor`, `components/elements/HyperLink.razor`, `components/layouts/MainLayout.razor` |
| Feature pages | `features/*/pages/*Page.razor` (e.g. `CounterPage.razor`, `WeatherForecastsPage.razor`, `LoginPage.razor`) |
| Feature components | `features/to-do/components/TodoItemForm.razor`, `features/event-stream/components/EventStream.razor` |
| Pure `.cs` | `components/elements/BadgeStatus.cs` |
| Host pages | `pages/Authentication.razor`, `pages/SettingsPage.razor` |

**Note:** State action partials under SPA features (`application-state.close-modal.cs`, etc.) **are** kebab-compliant (multi-dot partial form). Only UI file basenames lag.

**Risk:** Template ships SPA UI naming that contradicts “kebab everywhere,” so generated apps inherit mixed conventions. Folder paths under SPA features are already kebab.

---

### F3 — Integration tests still Pascal + snake (priority: high)

**Newer tests are already kebab** (positive control):

- `tests/container-apps/web/web-contracts-tests/` — e.g. `role-contracts-serialization-tests.cs`, `global-usings.cs`, `features/admin/roles/`

**Legacy-style projects (file + directory violations):**

| Project | Dir issues | File pattern |
|---------|------------|--------------|
| `tests/.../web-server-integration-tests/` | `Features/`, `Infrastructure/`, nested Pascal slices | `CreateRole_Endpoint_Tests.cs`, `GlobalUsings.cs` |
| `tests/.../web-spa-integration-tests/` | same | `CounterState_Clone_Tests.cs`, `BaseTest.cs` |
| `tests/.../api-server-integration-tests/` | same | `GetWeatherForecastsEndpoint_Tests.cs` |
| `tests/.../aspire-tests/` | — | `IntegrationTest1.cs`, `GlobalUsings.cs` |

**Counts (source-controlled, non-`obj`):** ~**38** non-kebab test **files**, ~**19+** Pascal test **directories** under those integration projects (plus nested Feature paths counted in the walk).

**Target kebab shape (aligned with contracts tests):**

| Current | Target |
|---------|--------|
| `Features/Admin/Roles/CreateRole/` | `features/admin/roles/create-role/` |
| `CreateRole_Endpoint_Tests.cs` | `create-role-endpoint-tests.cs` |
| `GlobalUsings.cs` | `global-usings.cs` |
| `Infrastructure/` | `infrastructure/` |

---

### F4 — Documentation basenames (priority: medium)

**Count:** **46** non-kebab files under `documentation/`.

**Clusters:**

1. **How-to guides (Pascal `HowTo*`)** — e.g. `HowToRemoveDemoFeatures.md`, `HowToUpgradeToAnalyzerPackages.md`, testing how-tos.
2. **Title Case conceptual docs** — e.g. `ApiDesign.md`, `EndToEndTesting.md`, `DotnetConventions.md`.
3. **`Overview.md` / `Roadmap.md` style** — single-word Pascal titles across many folders.
4. **Snake + Pascal hybrids** — `Handling_Mutability_in_API_Contracts.md`, `HowToWrite_BFF_API_Contracts.md`.
5. **ADR example** — `0001-use-CC0-as-license.md` (uppercase `CC0` segment).

Directories under `documentation/` are largely kebab already; only basenames lag.

---

### F5 — Host `Properties/` folders (priority: low — framework)

| Path | Notes |
|------|--------|
| `source/container-apps/*/projects/*/Properties/` (api, web-server, grpc, aspire) | ASP.NET default; `launchSettings.json` inside |
| `source/container-apps/yarp/Properties/` | same |

Treat as **documented ecosystem exception** unless the team standardizes on a kebab host folder and rewires tooling.

---

### F6 — Static assets and template logos (priority: low)

| Location | Examples |
|----------|----------|
| `web-spa/wwwroot/images/TheFreezeTeam/`, `TimeWarp/` | Pascal dirs; `SOLID_*.png` |
| `timewarp-templates/assets/` | `Logo.png`, `LogoNoMargin.svg`, `LogoNoWordsOrShadow_512x512.png` |
| `timewarp-templates/testEnvironments.json` | camelCase basename |

Cosmetic / packaging only; no build grammar impact.

---

### F7 — Not counted as product violations

- **Kanban task filenames** with double-hyphen phrase separators (`059-002-...--...md`) — kanban CLI naming, not product source.
- **Roslyn** `AnalyzerReleases.*.md` — exception table.
- **Partial multi-dot** SPA/state files — compliant under the refined rule.

## Recommendations

Ordered by impact on the template and convention enforcement. **No time estimates.**

### Priority 1 — Decide SPA Blazor basename policy

Pick one and document it in `AGENTS.md` / `tw-csharp` / a short ADR:

| Option | Meaning |
|--------|---------|
| **A. Kebab everywhere (strict)** | Rename `HomePage.razor` → `home-page.razor` (and code-behinds); type names stay PascalCase (`HomePage`). Matches repo-wide rule. |
| **B. Explicit SPA exception** | Document: “`.razor` / `.razor.cs` basenames may match the public type (PascalCase); all other files and folders remain kebab.” Stops false debt. |

**Checklist if A:** inventory under `web-spa/components`, `web-spa/features/**/pages`, `web-spa/features/**/components`, `web-spa/pages`; rename + update any path references; keep state partials as-is (already kebab).

### Priority 2 — Migrate remaining integration test trees to kebab

Mirror `web-contracts-tests` layout:

1. `Features/` → `features/`; nested Pascal → kebab segments.
2. `*_Tests.cs` / Pascal test helpers → kebab (`create-role-endpoint-tests.cs`, `global-usings.cs`, `base-test.cs`).
3. `Infrastructure/` → `infrastructure/`; `Pipeline/` / `Serialization/` → lowercase kebab.
4. Leave projects that are already compliant alone (contracts tests, etc.).

**Suggested order:** web-server integration → api-server integration → web-spa integration → aspire tests.

### Priority 3 — Documentation rename pass

- Standardize how-tos to `how-to-<topic>.md` (e.g. `how-to-remove-demo-features.md`).
- Conceptual docs: `api-design.md`, `end-to-end-testing.md`, `overview.md`.
- Fix underscore hybrids under `web-api-contracts/`.
- Update internal links in the same change set.

### Priority 4 — Optional cleanup

- Template logo assets → kebab (`logo-no-margin.svg`, etc.) if packaging scripts allow.
- `testEnvironments.json` → `test-environments.json` if nothing hard-codes the name.
- Formally list ASP.NET `Properties/` + `appsettings.<Env>.json` in `tw-csharp` exception bullets.

### Priority 5 — Enforcement gap

Today TWA0015/0016 cover **axis-1 feature grammar**, not generic kebab for SPA UI or tests. Options:

1. Script/CI check: basenames under `source/` and `tests/` must match the kebab regex (with exception allowlist).
2. Or document SPA + host exceptions and only enforce outside those globs.

Without one of these, SPA and test drift will reappear.

## Summary table

| Area | Status | Approx. violations | Priority |
|------|--------|--------------------|----------|
| Axis-1 `features/` + `platform/` (non-SPA) | Compliant | 0 | — |
| Foundation + analyzers `.cs` | Compliant | 0 | — |
| Source `.cs` excl. web-spa | Compliant | 0 | — |
| web-spa Blazor UI files | **Violations** | ~90 razor family + 1 `.cs` | High (policy + rename or document exception) |
| Integration test projects | **Violations** | ~38 files + Pascal dirs | High |
| Documentation | **Violations** | 46 files | Medium |
| `Properties/` host folders | Framework | 5 dirs | Low / allowlist |
| Assets / template logos | **Violations** | ~15 | Low |

## References

- `AGENTS.md` — Layout (kebab-case paths)
- `.agents` / flow skill `tw-csharp` — File and directory naming
- `source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json` — Axis-1 grammar SSOT
- `skills/tw-feature-placement/SKILL.md` — Feature filename grammar
- Positive control: `tests/container-apps/web/web-contracts-tests/`
- Related older docs (pre-kebab examples still show Pascal `Features/`):  
  `documentation/developer/conceptual/architectural-decision-records/project-structure-and-conventions/ProjectStructureAndConventions.md`

## Appendix — Suggested kebab renames (samples)

### SPA (if strict policy)

| Current | Suggested |
|---------|-----------|
| `HomePage.razor` | `home-page.razor` |
| `ModalContainer.razor.cs` | `modal-container.razor.cs` |
| `TodoItemForm.razor` | `todo-item-form.razor` |
| `BadgeStatus.cs` | `badge-status.cs` |
| `AuthenticationStateListener.razor` | `authentication-state-listener.razor` |

### Tests

| Current | Suggested |
|---------|-----------|
| `Features/Admin/Roles/CreateRole/CreateRole_Endpoint_Tests.cs` | `features/admin/roles/create-role/create-role-endpoint-tests.cs` |
| `GlobalUsings.cs` | `global-usings.cs` |
| `Infrastructure/WebServerTestConvention.cs` | `infrastructure/web-server-test-convention.cs` |

### Docs

| Current | Suggested |
|---------|-----------|
| `HowToRemoveDemoFeatures.md` | `how-to-remove-demo-features.md` |
| `HowToUpgradeToAnalyzerPackages.md` | `how-to-upgrade-to-analyzer-packages.md` |
| `Handling_Nullability_in_API_Contracts.md` | `handling-nullability-in-api-contracts.md` |
| `DotnetConventions.md` | `dotnet-conventions.md` |
