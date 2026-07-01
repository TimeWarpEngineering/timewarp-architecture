# Web API Contracts — Cross-Source Analysis

**Date:** 2026-07-01  
**Scope:** `skills/web-api-contracts/`, `documentation/developer/how-to-guides/web-api-contracts/`, this template repo (`timewarp-architecture/dev`), and production reference repo (`copic/main`).

---

## Executive summary

The **skill is the most complete and actionable source** for scaffolding contracts. Official docs cover structure, mutability, and nullability at a conceptual level but omit RouteMixin, OneOf, mock factories, contract tests, query-string patterns, and mixin attributes.

**Folder naming is inconsistent across all four sources.** The skill says singular folders + plural namespaces; official docs say plural folders + plural namespaces; both repos use mixed conventions in practice. **Only namespace pluralization is consistently applied.**

**Copic is the canonical example corpus** for tier-2/3 patterns (SecurityRole CRUD, filterable queries, `Web.Contracts.Tests`), but it carries significant legacy debt (`string?` + `NotEmpty()`, separate SPA mock-factory classes, Morris.Moxy mixins). **The template repo is the target architecture** (Roslyn `ContractsMixinGenerator`, `GetMockResponseFactory()` on contracts, `IAuthApiRequest`) but is thinner (24 contract files, no `*Contracts.Tests`, some anti-pattern samples in todo-items).

The skill should be treated as the **agent workflow authority**, with docs updated to match and explicit **repo-era detection** (Moxy vs generator, mock-factory placement, test project presence).

---

## Sources reviewed

| Source | Location | Role |
|--------|----------|------|
| Agent skill | `skills/web-api-contracts/SKILL.md` + `references/` | Agent scaffolding workflow |
| Official docs | `documentation/developer/how-to-guides/web-api-contracts/` (4 files) | Human-oriented guides |
| Template repo | `timewarp-architecture/dev` — `source/container-apps/web/web-contracts/` | `dotnet new timewarp-architecture` output |
| Production repo | `copic/main` — `Source/ContainerApps/Web/Web.Contracts/` | Largest real-world contract set |

---

## Repository snapshot comparison

| Metric | Template (`dev`) | Copic (`main`) |
|--------|------------------|----------------|
| Contract `.cs` files | 24 | 65 |
| `GetMockResponseFactory()` on contract | 7 / 24 (29%) | 4 / 65 (6%) |
| SPA mock wiring | `MockWebApiService` dictionary → contract factories | `MockCopicApiService` → 47 `*MockFactory.cs` classes in `Web.Spa/Services/MockFactories/` |
| `Web.Contracts.Tests` project | **None** | **Yes** — 23 `*_Tests.cs` with `SerializeAndDeserialize` |
| Mixin infrastructure | Roslyn `ContractsMixinGenerator` in `TimeWarp.Foundation.Contracts` | Morris.Moxy — `Common.Contracts/Mixins/*.mixin` |
| `IAuthApiRequest` usage | Yes (admin roles) | **None** |
| Path casing | Lowercase `features/` | PascalCase `Features/` |
| .NET version | net10.0 | net8.0 |

---

## Pattern alignment matrix

| Pattern | Skill | Official docs | Template | Copic |
|---------|-------|---------------|----------|-------|
| `public static partial class` outer shell | Required | Required | Mostly yes; todo-items use `sealed` | Universal |
| Nested `Query` / `Command` / `Response` / `Validator` | Yes | Yes | Yes | Yes |
| `[RouteMixin(route, HttpVerb)]` | Yes | **Missing** | Yes | Yes |
| `IRequest<OneOf<Response, SharedProblemDetails>>` | Yes | Partial (no OneOf named) | Yes | Yes |
| `I*Details` + `AbstractValidator<I*Details>` | Yes | Mentioned | Yes (roles) | Yes (SecurityRole, TodoItem, Admin) |
| Nullability: `string` + `null!` for required | Yes | Yes | Partial (todo-items use `string.Empty`) | **Legacy violations widespread** |
| Mutability: `set` on bindable, `init`/`get` on display | Yes | Yes | Yes (roles) | Yes |
| `IQueryStringRouteProvider` | Yes | **Missing** | Yes (GetRoles, Hello) | Yes (many queries) |
| `[IOpenDataQueryParametersMixin]` | **Missing** | **Missing** | Yes (GetRoles) | Yes (GetSecurityRoles, etc.) |
| `[IAuthApiRequestMixin]` / `IAuthApiRequest` | **Missing** | **Missing** | Yes | **N/A** |
| `GetMockResponseFactory()` on every contract | Required | **Missing** | Partial coverage | Rare; legacy SPA classes |
| `*Contracts.Tests` serialization tests | Required | **Missing** | **None** | Full project |
| Read-only display interfaces (`IPolicyDto`) | Yes | **Missing** | Limited | Yes (`IPolicyDto`) |

---

## Folder and namespace conventions

### What each source says

| Source | Folder rule | Namespace rule |
|--------|-------------|----------------|
| **Skill** | Singular (`Features/Admin/SecurityRole/`) | Plural (`*.Features.Admin.SecurityRoles`) |
| **HowToWrite_BFF_API_Contracts.md** | Plural (`Features/Users/`) | Plural (`*.Features.Users`) |
| **Skill examples.md** | Singular in table | Plural |

### What repos actually do

**Template** — lowercase paths, mixed folder plurality:

- `features/admin/roles/` → namespace `Features.Admin.Roles` (plural folder **and** namespace)
- `features/todo-items/` → namespace `Features.TodoItems`

**Copic** — PascalCase `Features/`, **mixed** folder plurality:

| Folder | Namespace | Notes |
|--------|-----------|-------|
| `Features/Admin/SecurityRole/` | `Copic.Features.Admin.SecurityRoles` | Singular folder, plural namespace (matches skill) |
| `Features/Announcements/` | `Copic.Features.Announcements` | Plural both (matches docs) |
| `Features/TodoItem/` | `Copic.Features.TodoItems` | Singular folder, plural namespace |
| `Features/Policies/` | `Copic.Features.Policies` | Plural both |

**Test paths in Copic** use plural feature names (`Tests/.../SecurityRoles/`) while source uses `Admin/SecurityRole/` — another inconsistency agents may copy incorrectly.

### Recommendation

Document the **invariant** clearly:

> **Namespace is always plural.** Folder name may be singular (entity slice) or plural (feature group) — **discover and mirror the parent folder convention in the current repo**, never assume from another repo.

The skill's singular-folder rule conflicts with official docs and many Copic folders. Align skill + docs on namespace plural + folder follows local convention.

---

## Nullability analysis

### Skill + `references/nullability.md`

Strong, opinionated, internally consistent:

- Type declares intent; validator enforces it
- Forbidden: `string?` + unconditional `NotEmpty()`, `= string.Empty` on required refs, `= default!` on non-generic refs
- Responses: ctor + `Guard`, not FluentValidation

### Official `Handling_Nullability_in_API_Contracts.md`

Aligns with skill on core rules (`null!`, avoid `string.Empty`, ctor for responses). Gaps:

- No explicit **contradiction** callout (`string?` + `NotEmpty()`)
- Broken code sample (lines 32–44 — mismatched braces)
- Closing paragraph incorrectly says "mutability and nullability" (copy-paste error)

### Template repo

**Roles** follow skill rules (`= null!` on UpdateRole). **Todo-items** violate skill:

```csharp
public string Title { get; init; } = string.Empty;  // forbidden pattern
```

### Copic — widespread legacy contradictions

Admin bindable interfaces use `string?` with unconditional `NotEmpty()`:

| File | Properties | Validator |
|------|------------|-----------|
| `SecurityRoleDetails.cs` | `string? Name`, `Code` | `NotEmpty()` on both |
| `ApplicationDetails.cs` | `string? Name`, `AdminUrl` | `NotEmpty()` on both |
| `ModuleDetails.cs` | `string? Name`, `Description`, `Code` | `NotEmpty()` on all |

**Copic TodoItem** (newer) follows skill correctly: `string Title = null!`, optional `string? Description`.

**Implication for agents:** Copic Admin contracts are **anti-patterns for nullability** despite being structurally correct for `I*Details`. Skill `examples.md` Tier 2 SecurityRole example is the **target state**, not Copic's current Admin code.

---

## Mutability analysis

### Skill + docs + both repos — aligned on principles

- Display DTOs: `{ get; }` or `{ get; init; }`, `IReadOnlyList<T>` for read-only collections
- Bindable `I*Details`: `{ get; set; }`, `List<T>` when editable
- Identity fields on editable entities: immutable (`SecurityRoleId`, `RoleId`, `Guid`)

### Copic nuance: mixed mutability on `ISecurityRoleDetails`

```csharp
public interface ISecurityRoleDetails
{
  public Guid Guid { get; }           // read-only on interface
  public string? Name { get; set; }    // bindable but nullable (legacy)
  public IEnumerable<IModuleDetails> Modules { get; }  // read-only collection
}
```

Skill examples use mutable `ApplicationId { get; set; }` with non-nullable `Name { get; set; } = null!` — cleaner than Copic's nullable required fields.

### Docs gap

`Handling_Mutability_in_API_Contracts.md` does not explain **why** mutability matters for Blazor (`EditForm` binding without view models). Skill covers this well; docs should add one paragraph linking to ADR 0003.

---

## RouteMixin and mixin infrastructure

### Template (current target)

`ContractsMixinGenerator` (ships in `TimeWarp.Foundation.Contracts`) replaces three Moxy mixins:

| Attribute | Emits |
|-----------|-------|
| `[RouteMixin]` | `RouteTemplate`, `GetHttpVerb()`, `GetRoute()`, route param properties |
| `[IAuthApiRequestMixin]` | `UserId`, `GetAuthQueryParameters()` |
| `[IOpenDataQueryParametersMixin]` | Open-data paging/sort query params + `GetOpenDataQueryParameters()` |

**Not documented in skill or official docs.** Copic still documents Moxy in `Common.Contracts/Mixins/RouteMixin.md`, including stale `GetUri()` (generator emits `GetRoute()` only).

### Skill gaps

- No mention of generator package or emitted members
- "Do not hand-declare route params" is correct for `[RouteMixin]` params but omits **auth** (`UserId` from mixin) vs **manual** `IAuthApiRequest` (template GetRole declares `UserId` manually while GetRoles uses mixin attribute)
- Discovery command `**/Features/**` fails on template's lowercase `features/`

---

## Mock response factory patterns

### Skill + `mock-response-factory` skill

Every contract exposes `GetMockResponseFactory()`; SPA registers `typeof(Query/Command) → factory()` in mock API service dictionary.

### Template

`MockWebApiService` uses contract-local factories — **matches target pattern** for covered endpoints (7/24). Uncovered endpoints fall back to real API.

### Copic (legacy)

| Approach | Count | Location |
|----------|-------|----------|
| `GetMockResponseFactory()` on contract | 4 | `Web.Contracts` |
| `IMockResponseFactory` implementor classes | 47 | `Web.Spa/Services/MockFactories/` |

`MockCopicApiService` wires `new GetSecurityRoleMockFactory()` — not `GetSecurityRole.GetMockResponseFactory()`.

**Agents copying Copic will produce the wrong mock pattern** relative to skill and template. Skill should add explicit detection:

```bash
rg -l 'GetMockResponseFactory' --glob '**/Web.Contracts/**/*.cs'   # contract-local (preferred)
rg -l 'MockFactory' --glob '**/Web.Spa/**/*.cs'                    # legacy SPA classes
```

---

## Contract tests

### Skill requirement

`*Contracts.Tests` project with Fixie `Command_Should_.SerializeAndDeserialize()` using camelCase `JsonSerializerOptions`.

### Copic — reference implementation

- Project: `Tests/ContainerApps/Web/Web.Contracts.Tests/`
- Convention: `WebContractsTestingConvention` extends `TimeWarp.Fixie.TestingConvention`
- 23 test files; SecurityRole has Command + Response round-trip tests
- Test folder naming uses plural `SecurityRoles/` (differs from source `SecurityRole/`)

### Template — gap

No `*Contracts.Tests` project. Serialization tests exist elsewhere (`web-spa-integration-tests`) for state, not contracts.

**Skill should soften:** "Add serialization tests to `*Contracts.Tests` when the project exists; if absent, check whether the repo tests contracts elsewhere before creating a new project."

---

## Official documentation review

### `Overview.md`

Good scope statement (Web.Contracts + Api.Contracts, not gRPC). Links to three guides + ADR. Does not mention agent skill.

### `HowToWrite_BFF_API_Contracts.md`

**Covers well:** feature folder layout, Commands/Queries subfolders, bindable interfaces, namespace plural, static partial class, nested types.

**Missing (skill has these):**

- `[RouteMixin]` and HTTP verb mapping
- `IApiRequest`, `OneOf<,>`, `SharedProblemDetails`
- `IQueryStringRouteProvider`
- Mixin attributes
- Mock response factories
- Contract test requirements
- `ListResponse<T>`, stream/file return types
- Validation composition (`SetValidator`)
- Empty validator pattern (`AbstractValidator<Query>;`)

**Inaccuracy:** States feature folders are plural; Copic and skill examples use singular entity folders under Admin.

### `Handling_Nullability_in_API_Contracts.md`

Solid principles; needs contradiction section and syntax fix in UpdateUser example.

### `Handling_Mutability_in_API_Contracts.md`

Solid; typo `IReadonlyList<t>`; missing Blazor binding motivation.

### Docs vs skill relationship

| Content | Best home |
|---------|-----------|
| Why endpoint-centric / BFF philosophy | Docs (human onboarding) |
| Step-by-step scaffold workflow | Skill |
| Nullability/mutability decision trees | Both (skill = terse; docs = explanatory) |
| RouteMixin generator reference | Docs + skill detection section |
| Repo-specific discovery commands | Skill only |

---

## Skill review (consolidated)

### Strengths

1. Complete scaffold workflow (10 steps + checklist)
2. Nullability/mutability references are production-quality
3. Repo-agnostic with "read 2–3 existing contracts" guardrail
4. Tiered examples in `references/examples.md` map to Copic patterns
5. Explicit anti-patterns table and legacy warning
6. Correct delegation to `mock-response-factory` skill

### Gaps and errors

| Issue | Severity | Detail |
|-------|----------|--------|
| Folder naming vs docs/repos | High | Singular-folder rule conflicts with docs and many Copic paths |
| Case-sensitive discovery globs | High | `**/Features/**` misses template's `features/` |
| Missing mixin/generator section | High | `[IAuthApiRequestMixin]`, `[IOpenDataQueryParametersMixin]`, emitted APIs |
| Mock factory assumes contract-local | Medium | Copic legacy uses SPA `*MockFactory` classes |
| Hard requirement for `*Contracts.Tests` | Medium | Template has no such project |
| `static` vs `sealed` outer class | Low | Template todo-items use `sealed` |
| `mock-response-factory.md` stub | Low | Dead one-line redirect |
| No link to official docs | Low | Drift risk |
| Evals routing-only | Low | Fixtures exist but unwired |

### Skill examples provenance

| Tier | Example | Closest real repo |
|------|---------|-------------------|
| 1 | `GetAnnouncementsForCurrentUser` | Copic — nearly identical (minus mock factory) |
| 2 | SecurityRole CRUD | Copic structure; skill nullability is **aspirational** vs Copic Admin |
| 3 | `GetAccountOwnedPolicies` | Copic — identical route/filters |
| 4 | Serialization test | Copic `CreateSecurityRole_Tests.cs` |

---

## Recommendations

### For the skill (priority order)

1. **Fix discovery commands** — case-insensitive paths; document both `Features/` and `features/`.
2. **Revise folder rule** — plural namespace (invariant); folder singular/plural follows local repo.
3. **Add mixin/generator section** — three attributes, emitted members, Moxy vs Roslyn detection.
4. **Add repo-era table** — template vs Copic: mock factory location, test project, auth mixin, nullability debt.
5. **Conditionalize contract tests** — required when `*Contracts.Tests` exists; guidance when absent.
6. **Warn against copying Copic Admin nullability** — point to skill Tier 2 as target, Copic as structural reference only.
7. **Link official docs** in SKILL.md under "Further reading".
8. **Expand evals** — wire `evals/contracts/fixtures/` into `eval.yaml`.

### For official documentation

1. **Extend HowToWrite** with RouteMixin, OneOf, IApiRequest, query-string provider, mock factories, contract tests.
2. **Fix** nullability doc syntax error and closing paragraph.
3. **Reconcile folder naming** — document namespace invariant + folder follows local convention.
4. **Add RouteMixin reference** replacing stale Moxy `GetUri()` docs; point to `ContractsMixinGenerator`.
5. **Cross-link** to `skills/web-api-contracts/` for agent-assisted development.

### For template repo (`dev`)

1. Add `Web.Contracts.Tests` (or document intentional omission).
2. Fix todo-items `string.Empty` → `null!`; align outer class to `static`.
3. Increase `GetMockResponseFactory()` coverage beyond 7/24.

### For Copic (optional hygiene)

1. Migrate Admin `string?` + `NotEmpty()` to `string` + `null!` incrementally.
2. Consolidate SPA `*MockFactory` classes toward contract-local `GetMockResponseFactory()` when touching endpoints.
3. Plan Moxy → `ContractsMixinGenerator` migration (net10 / foundation package alignment).

---

## Conclusion

The **web-api-contracts skill is the right abstraction** for agents: it encodes workflow, guardrails, and anti-patterns that official docs omit. **Copic provides volume and test patterns**; **the template provides infrastructure direction** (Roslyn mixins, auth requests, contract-local mocks).

The highest-risk agent failures today are:

1. Copying **Copic nullability** from Admin contracts  
2. Using **wrong discovery globs** on the template repo  
3. Assuming **singular folders** or **contract-local mock factories** without reading the target repo first  

Unifying folder documentation, documenting mixin generation, and adding repo-era detection to the skill would close most of the gap between documentation, skill, and production reality.

---

## Update — after `contract-conventions-rfc.md` (2026-07-01)

Read [`contract-conventions-rfc.md`](contract-conventions-rfc.md). It sharpens this analysis in several places and turns open questions into explicit decision ballots. Below: what changes in *this* document, and where I land on each RFC decision.

### Corrections to the original analysis

| Original claim | RFC refinement |
|----------------|----------------|
| Skill is "most complete source" | More precise: skill is **"copic, cleaned up in the author's head"** — normative ideal, not a mirror of copic or TWA |
| "Copic uses Morris.Moxy" | True for copic; **TWA has no Moxy** — `[RouteMixin]` is Roslyn `ContractsMixinGenerator` only. Do not tell agents to "detect Moxy vs generator" in TWA |
| Folder rule: "discover local convention" | RFC argues TWA is **consistently plural + kebab**; skill's singular-folder story is unsupported here. For TWA specifically, prescribe plural — reserve discover-first only for cross-repo portability |
| Contract tests: "conditional if project exists" | RFC Decision 3 leans **create `web-contracts-tests`** — I now agree that's the right TWA end-state, not permanent conditional wording |
| Mock factory "required for every contract" | RFC correctly flags this as **overstated** — both repos treat mocks as optional with SPA dict fallback. Skill should say "add when endpoint is used in mock mode" |
| Missing from original analysis | **§4 objective bugs**: empty `Command` body implementing `I*Details` won't compile (Tier-2 example is wrong); skill says "MediatR" but both repos use **TimeWarp.Mediator**; **053-002** rename sequencing |

### Position on RFC decisions

| # | Topic | Vote | Notes |
|---|-------|------|-------|
| 1 | Casing | **Discover-first; kebab in TWA examples** | Agree with RFC author. Skill must not prescribe PascalCase globally when the template it ships in is 100% kebab |
| 2 | Folder plurality | **Plural** (TWA + docs) | Shift from my earlier "either is fine." Singular `SecurityRole/` in copic is legacy inconsistency, not a pattern to teach |
| 3 | Contract tests | **New `web-contracts-tests` project** | Small, fast, matches copic proof — better than leaving round-trips only in integration tests |
| 4 | Assertions | **Shouldly** in TWA examples | Repo-wide standard; skill Tier-4 should drop FluentAssertions for TWA-anchored material |
| 5 | Create Response | **Mixed** — ctor+`Guard` default; `required init` for trivial id-only | Matches copic's de-facto split and keeps `Get`-for-edit uniform |
| 6 | `IAuthApiRequest` | **Promote** | TWA-only today but genuinely useful; document alongside `IApiRequest` |
| 7 | Nullability | **Keep rule + fix TWA violators** | Unchanged. Copic's 17 files are the cautionary tale, not the spec |
| 8 | 053-002 sequencing | **Rename first, then cleanup** | Avoid teaching `[RouteMixin]` if `[Route]`/`[AuthApiRequest]` is weeks away; one pass over contracts + skill |

### Revised recommendation priority (supersedes §Recommendations for TWA work)

1. Resolve **053-002** attribute rename (or explicitly defer it) before mass contract edits
2. Fix **§4 objective skill bugs** (empty `I*Details` body, MediatR→TimeWarp.Mediator, mock "required" wording)
3. Rewrite skill against **final attribute names**, **plural kebab folders**, **Shouldly** tests, **`IAuthApiRequest`**
4. Add **`web-contracts-tests`**; migrate examples off copic-only FluentAssertions
5. Kanban cleanup: **`todo-items`**, **`hello`**, **`analytics`** (RFC §7 offender list)
6. Confirm **skill canonical path** (`skills/web-api-contracts` here vs `timewarp-flow/.../webapi-contracts`) before editing — sync overwrite risk

### Unchanged from original analysis

- Nullability/mutability reference content is production-quality — keep
- Copic Admin nullability is debt; use skill Tier-2 as target, copic for structure only
- Case-sensitive `**/Features/**` discovery glob is broken on TWA — still must fix
- Official docs need mechanical sections (RouteMixin/generator, OneOf, mock factories, tests)

Full ballot with reasoning: see reviewer entry in [`contract-conventions-rfc.md`](contract-conventions-rfc.md#reviewer-opinions).