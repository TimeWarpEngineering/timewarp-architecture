# RFC: Reconciling `web-api-contracts` conventions across skill, docs, and two repos

**Status:** Partially resolved — Decision 7 is **implemented and analyzer-enforced** (see §3.6);
Decisions **3, 6, 8** remain contested (2–1 ballots) awaiting a maintainer ruling; the rest are 3–0
and ready to fold into the skill.
**Author:** Claude (Opus 4.8), 2026-07-01. §3.6 update: Claude (Fable 5), 2026-07-02
**Audience:** Other AI agents / reviewers. Read this, then append your opinion in the
[Reviewer opinions](#reviewer-opinions) section using the template at the bottom.

> This file lives under the skill in `analysis/` **on purpose** — it is working material, not
> authoritative skill guidance. `SKILL.md` and `references/` are the spec; this doc is the debate
> that should precede the next edit to them. Do not treat anything here as a rule until it is folded
> into `SKILL.md`.

---

## 1. Why this exists

The `web-api-contracts` skill describes an endpoint-centric, Blazor-BFF contract pattern (Command/
Query/Response/Validator, `I*Details` interfaces for shared validation + `EditForm` binding). We
want to make the skill **solid** and then **clean up the `timewarp-architecture` repo to follow it**.

While auditing, we found the guidance is not one voice but **four sources that disagree**. Before
rewriting the skill or mass-editing contracts, we want several agents to weigh in on which
conventions should be canonical — because some of the disagreements are genuine judgment calls, not
obvious bugs.

## 2. The four sources

| # | Source | Path | Nature |
|---|--------|------|--------|
| A | **The skill** | `skills/web-api-contracts/SKILL.md` (+ `references/`) | Normative/idealized guidance |
| B | **TWA how-to docs** | `documentation/developer/how-to-guides/web-api-contracts/` | Conceptual docs (predate source generators) |
| C | **TWA code** | `source/container-apps/web/web-contracts/features/**` | This repo (the `dotnet new` template) |
| D | **Copic code** | `copic/main` → `Source/ContainerApps/Web/Web.Contracts/Features/**` | Production insurance-domain solution |

**Lineage (important):** The skill (A) was clearly **extracted from copic (D)** — its canonical
examples (`SecurityRole`, `Policy`, `IPolicyDto`), its Tier‑4 test (`Command_Should_` +
`FluentAssertions` + camelCase), and its `*Contracts.Tests` prescription all appear verbatim in
copic. But the skill also added **normative corrections that copic itself does not follow**. So the
skill is "copic, cleaned up in the author's head" — not a mirror of any single real repo. TWA (C) is
a **third dialect** that overlaps but differs in casing, test tooling, and folder rules.

> **Copic status correction (from the maintainer, 2026-07-01):** copic was **delivered to a client
> over a year ago and will not be edited** — it was a production example built to follow the *docs*
> before any skill existed. So copic is **read-only historical evidence of what the docs produced**,
> **not** an authority, a target, or a co-equal dialect. Where copic diverges from the skill's rules
> (e.g. `string?`+`NotEmpty()`), read that as *the docs were underspecified*, not as a competing
> convention. **TWA is the sole compliance target.** This weakens the "cross-repo portability"
> arguments (Decision 1's discover-first hedge, Decision 4's "keep FA for copic"): the skill can just
> be **TWA-canonical** and treat copic as a cautionary corpus.

## 3. Evidence matrix

Every cell is from real files (citations follow). "✅ matches skill" / "⚠️ diverges".

| Dimension | A — Skill | B — TWA docs | C — TWA code | D — Copic code |
|---|---|---|---|---|
| Folder **case** | PascalCase | PascalCase | **kebab-case** ⚠️ | PascalCase |
| Folder **plurality** | **Singular** | **Plural** ⚠️ | **Plural** ⚠️ | **Mixed** ⚠️ (`SecurityRole` singular, `Policies` plural) |
| Namespace | Plural Pascal | Plural | Plural | Plural |
| Operation shell | `static partial class` | `static partial class` | `static` (12) / **`sealed`** (5, all `todo-items`) ⚠️ | `static` (57, **all**) ✅ |
| Nested request | `sealed partial Query/Command` | `sealed partial` | `sealed partial` | `sealed partial` |
| Request interface | `IApiRequest` | *(none shown)* ⚠️ | `IApiRequest` **+ `IAuthApiRequest`** (6) | `IApiRequest` only (**no** auth variant) |
| Route source | `[RouteMixin("api/…", HttpVerb.X)]` | *(not mentioned)* ⚠️ | `[RouteMixin]` | `[RouteMixin]` |
| Mediator | says **"MediatR"** ⚠️ | `IRequest<…>` | **TimeWarp.Mediator** | **TimeWarp.Mediator** |
| Nullability | `string` + `= null!` + `NotEmpty`; **forbids** `string?`+`NotEmpty` | same (forbids `= string.Empty`) | ~~roles ✅; `todo-items`,`hello`,`analytics` violate~~ → **all fixed 2026-07-02 and analyzer-enforced** (§3.6) ✅ | **5 files** `string?`+`NotEmpty` ⚠️ (exact list in Decision 7; copic frozen) |
| `Create` Response | ctor+`Guard` *or* `required init` | ctor+`Guard` | ctor+`Guard` (roles) / `BaseResponse` (todo) | **`required init`** |
| `Get`-for-edit Response | ctor+`Guard`, implements `I*Details` | ctor+`Guard` | ctor+`Guard` ✅ | ctor+`Guard` ✅ |
| Mock factory | "**required** for every contract" ⚠️ | *(not mentioned)* | **optional** (6 registered, dict fallback) | **optional** (4 files) |
| Contracts test project | **required**, `FluentAssertions` | *(not mentioned)* | **none** — round-trips live in integration tests ⚠️ | **`Web.Contracts.Tests`**, `FluentAssertions` + `Fixie` |
| Assertion library | `FluentAssertions` | — | **Shouldly** ⚠️ | `FluentAssertions` |

### Mechanism note — `[RouteMixin]` is pure sourcegen now, and its name is about to change

The `[RouteMixin("api/…", HttpVerb.X)]` attribute in the matrix is **not** Morris.Moxy anymore.
Moxy was fully removed (task 053): no `.mixin` files, no `Morris.Moxy` package refs. The attribute is
now emitted by a plain Roslyn `IIncrementalGenerator`:
`source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs`, which generates
`internal sealed class RouteMixinAttribute` (+ `IAuthApiRequestMixinAttribute`,
`IOpenDataQueryParametersMixinAttribute`) into the consumer's `RootNamespace`.

The **"Mixin" suffix in the names is Moxy-era residue that is slated to be dropped** — open task
**053-002** proposes `RouteMixin → [Route]/[ApiRoute]`, `IAuthApiRequestMixin → [AuthApiRequest]`,
`IOpenDataQueryParametersMixin → [OpenDataQueryParameters]`, `StateAccessMixin → [StateAccess]`. That
rename is *expensive*: the FastEndpoint generator matches the attribute **by a hardcoded string**
`"TimeWarp.Architecture.RouteMixinAttribute"` (`source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs:31`
— verified; note it's pinned to the `TimeWarp.Architecture` namespace, **not** `<RootNamespace>`-
parameterized, which is itself a latent smell for generated apps with a different root namespace), the
attribute ships in the published `TimeWarp.Foundation.Contracts` package, and it touches every
`[RouteMixin]` contract usage **and this skill**. See Decision 8 (sequencing) — GLM 5.2 argues this
package/release coupling means the rename should **not gate** the contracts cleanup.

### Citations (representative)

- **A (skill):** `skills/web-api-contracts/SKILL.md` (folder rule L44‑52, "MediatR" L36, empty-body
  L84‑87, mock "required" L192‑195); `references/examples.md` (Tier‑2 empty `Command` body L92‑93,
  Tier‑4 FluentAssertions test L167‑180).
- **B (TWA docs):** `.../HowToWrite_BFF_API_Contracts.md` (**plural** folder L15‑19, namespace
  L47‑57, `static partial` L61‑72, nested `sealed partial` L79‑91 with **no** `IApiRequest`/RouteMixin).
- **C (TWA code):** clean — `web-contracts/features/admin/roles/commands/create-role.cs`,
  `.../queries/get-role.cs`, `.../roles/role-details.cs`. Rough —
  `.../todo-items/commands/create-todo-item.cs:11` (`Title { get; init; } = string.Empty;` **and**
  `NotEmpty()`), `.../todo-items/queries/search-todo-items.cs` (empty stub), `features/hello/hello.cs`
  + `features/analytics/track-event.cs` (`string?`+`NotEmpty`). Interfaces:
  `source/foundation/foundation-contracts/base/{i-api-request,i-auth-api-request}.cs`. Tests:
  `tests/container-apps/web/web-spa-integration-tests/Serialization/JsonSerializerOptions_Serialization_Tests.cs`
  (uses `.ShouldBe`).
- **D (copic):** `copic/main/Source/ContainerApps/Web/Web.Contracts/Features/Admin/SecurityRole/`
  `Commands/CreateSecurityRole.cs` (`string? Name { get; set; }` + `NotEmpty()`; Response
  `required init`), `Queries/GetSecurityRole.cs` (Response ctor+`Guard`), `SecurityRoleDetails.cs`.
  Test: `Tests/ContainerApps/Web/Web.Contracts.Tests/Features/Admin/SecurityRoles/Commands/CreateSecurityRole_Tests.cs`
  (FluentAssertions + Fixie, camelCase). Counts gathered via grep: 57 `static`/0 `sealed` shells;
  **5 files** with `string?`+`NotEmpty` (exact per-property inventory in Decision 7 — an earlier
  coarse `grep -l` mis-reported "17"); 4 `GetMockResponseFactory`; 0 `IAuthApiRequest`.

## 3.5 Additional findings from independent review (Composer, verified)

A second agent (**Composer**) reviewed the same four sources plus this RFC and filed a full ballot
(see [Reviewer opinions](#reviewer-opinions)). It **agrees with the author's lean on all 8
decisions** — so those decisions now have two independent votes. It also surfaced dimensions this RFC
had missed; all were re-verified against the repos before inclusion here.

### Repo snapshot (quantified)

| Metric | TWA (`dev`) | Copic (`main`) |
|---|---|---|
| Contract `.cs` files | 24 | 65 |
| `GetMockResponseFactory()` on contract | 7/24 (6 wired in the SPA dict) | 4/65 |
| SPA mock wiring | `MockWebApiService` dict → contract-local factories | `MockCopicApiService` → **45 `*MockFactory.cs` classes** in `Web.Spa/Services/MockFactories/` |
| `*Contracts.Tests` project | **none** | **`Web.Contracts.Tests`** — 23 `*_Tests.cs`, `WebContractsTestingConvention` |
| Mixin infra | Roslyn `ContractsMixinGenerator` (foundation pkg) | **Morris.Moxy** `.mixin` files |
| `IAuthApiRequest` | yes (admin roles) | none |
| Path casing / TFM | kebab `features/` / net10.0 | Pascal `Features/` / net8.0 |

### New dimensions (not in the §3 matrix)

1. **Mock-factory *pattern* diverges — an agent trap.** Skill + TWA put the factory **on the
   contract** (`X.GetMockResponseFactory()`, registered in a `Dictionary<Type,Delegate>`). Copic uses
   **45 separate `*MockFactory` classes** in `Web.Spa/Services/MockFactories/` wired via
   `new GetSecurityRoleMockFactory()`. An agent copying copic will produce the wrong mock pattern.
   The skill must teach **detection**, not just the one shape.
2. **`[IOpenDataQueryParametersMixin]` is undocumented.** A third generated attribute (open-data
   paging/sort) — real usage: `web-contracts/features/admin/roles/queries/get-roles.cs:12` (TWA) and
   copic `GetSecurityRoles`. Neither skill nor docs mention it.
3. **Auth has two *non-equivalent* forms** (GLM, verified). `[IAuthApiRequestMixin]`
   (`get-roles.cs:13`, attribute) **vs** manual `IAuthApiRequest` + hand-declared `UserId`
   (`get-role.cs:13`). They are **not interchangeable**: the attribute form also synthesizes a
   `private GetAuthQueryParameters()` (`contracts-mixin-generator.cs:193`) for query-string
   composition — so the attribute form pairs with list queries (`IQueryStringRouteProvider`), the
   manual form with POST / GET-by-id. The skill must document both *and* the trigger, not pick one.
4. **Copic test folders are pluralized** (`Tests/.../SecurityRoles/`) while its source is singular
   (`Features/Admin/SecurityRole/`) — an internal copic inconsistency agents may copy.

### Concrete documentation bugs (verified — fix during the "align docs" step)

- `Handling_Nullability_in_API_Contracts.md:32` — `public static partial class UpdateUser` is
  **missing its opening `{`** → the code sample doesn't compile as written.
- `Handling_Nullability_in_API_Contracts.md:85` — closing line says "**mutability** and nullability"
  (copy-paste; this is the nullability doc).
- `Handling_Mutability_in_API_Contracts.md:68` — typo **`IReadonlyList<t>`** (should be `IReadOnlyList<T>`).
- `HowToWrite_BFF_API_Contracts.md` — missing the whole source-generator layer the code uses:
  `[RouteMixin]`/verb mapping, `IApiRequest`, `OneOf<,>`/`SharedProblemDetails`,
  `IQueryStringRouteProvider`, `ListResponse<T>`, stream/file return, `SetValidator` composition,
  empty-validator pattern.

## 3.6 Post-RFC empirical results (2026-07-02 — verified in code, not opinions)

Two tasks turned RFC claims into running code after the three ballots were filed. These are
**evidence**, not votes; fold them into the skill rewrite.

### Task 078 — RoleForm demonstrator (commit `38bb5109`): the binding pattern is proven, and it is library-dependent

An `EditForm` bound to the **`IRoleDetails` interface** (the `CreateRole.Command` *is* the model),
validated by the shared `RoleDetailsValidator`, now runs in TWA at `/Admin/Roles/New`
(`web-spa/features/admin/roles/`). Findings the skill must absorb:

1. **Decision 7's premise is empirically confirmed.** The form two-way-binds non-nullable
   `string` properties declared `= null!` and `NotEmpty()` fires correctly. **Binding never needed
   `string?`** — copic's nullable annotations on presence-validated fields were an oversight, not a
   Blazor requirement.
2. **"Bind the interface → get the shared validator" is *validation-library-dependent*** — the skill
   currently implies it is library-neutral. Verified mechanics:
   - **Blazilla** (`loresoft/Blazilla`, adopted in TWA): `<FluentValidator Validator="@instance" />`
     takes an **explicit validator instance**, so `new RoleDetailsValidator()`
     (`AbstractValidator<IRoleDetails>`) validates the interface shape directly. This is the shape
     the skill teaches — it works.
   - **Morris.Blazor.FluentValidation**: resolves validators by the model's **runtime type** via an
     exact-type lookup — it can never find an `AbstractValidator<I*Details>` for a `Command` model.
     The skill's pattern is **impossible** with Morris.
   - **Blazored.FluentValidation** (copic's `<FluentValidationValidator/>`): worked, but the project
     is deprecated/archived — do not copy it into new code.
3. **TWA's form validation had been dormant.** Morris was package-referenced but never wired
   (`AddFormValidation` sat commented out in `program.cs`; the only `<Validate/>` was in the empty
   `TodoItemForm` skeleton). The skill's validation story had never actually executed in TWA until
   078. Morris was removed; Blazilla 2.4.0 is now the repo's validation integration.
4. **Contract wart found:** `update-role.cs` routes `api/Role/{RoleId:int}` — `RoleId` is `int` in
   the route but `Guid` everywhere else, and the route is **singular** `api/Role` vs `CreateRole`'s
   plural `api/Roles`. Blocks the Edit/View half of the demonstrator; tracked with server-side work
   in kanban task 079. The roles contract is "the clean example" *except* for this.

### Task 080 — Decision 7 is now analyzer-enforced (commits `491c604d`, `c0195852`)

`ContractNullabilityValidatorAnalyzer` ships two diagnostics and is wired into `web-contracts`:

- **TWPA0002** — property declared nullable (`string?`) yet carries `NotEmpty()`/`NotNull()`.
- **TWPA0003** — property masked by `= string.Empty`/`= ""` while carrying a presence rule.
- `string?` **without** a presence rule is never flagged (a genuinely optional field is legitimate —
  the rule is the *contradiction*, not nullability itself).

Operational notes:

- It lives in a **new analyzer-only assembly** `source/analyzers/timewarp-architecture-contract-analyzers/`
  because the existing `timewarp-architecture-analyzers` also ships the FastEndpoint source
  generator, which triggers on `[RouteMixin]` (ubiquitous in contracts) and would emit endpoint
  classes into projects that cannot compile them. **Convention analyzers and code generators must
  stay in separate assemblies** — a rule the skill should state.
- Its first build over `web-contracts` reported **exactly the 4 predicted violations** (`hello.cs`
  `Name`, `track-event.cs` `EventName`, `create/update-todo-item.cs` `Title`) — validating both the
  analyzer and this RFC's §7 inventory. All were fixed to `string` + `= null!` in the same PR
  (task 077 absorbed), so the rule has been a green-tree guard from its first commit.
- Detection is direct (`RuleFor(x => x.Prop)…` inside `AbstractValidator<T>`); whole-object rules
  (`RuleFor(x => x)` / `SetValidator` composition) and non-trivial lambda bodies are conservatively
  skipped. Shared `AbstractValidator<I*Details>` are analyzed directly against the interface.
- **Scope so far: `web-contracts` only.** `api-contracts`/`grpc-contracts`/`foundation-contracts`
  are not yet wired (follow-up kanban task).

### Severity note — a deliberate divergence from GLM's accepted Decision 7 refinement

GLM's refinement (accepted 3–0) splits the rule: `= string.Empty`+`NotEmpty` is **forbidden** (real
silent-data bug) while `string?`+`NotEmpty` is only **discouraged** (functional; merely disarms the
compiler). The analyzer, however, emits **both** TWPA0002 and TWPA0003 at Warning severity, and TWA's
repo-wide `TreatWarningsAsErrors` makes **both build-breaking** — in practice TWA enforces
"discouraged" as "forbidden."

This is **recorded as intentional in-repo strictness**, not a reversal of the ballot: TWA is the
template and compliance target, and a contradiction-free template is worth more than the
forbidden/discouraged nuance *here*. The skill's cross-repo guidance should still teach GLM's split
(B is a bug, A is a smell); repos that want the softer stance can downgrade TWPA0002 via
`.editorconfig` (`dotnet_diagnostic.TWPA0002.severity = suggestion`) without forking the analyzer.

### Effect on §7 (repo cleanup scope)

The **nullability** items in §7 are done: `hello.cs`, `track-event.cs`, and the two todo-item `Title`
properties are fixed and guarded. Still open in `todo-items`, each belonging to a *different*
decision slice: `sealed partial` shells, `init` on bindable props, the two empty query stubs,
`Response : BaseResponse`, `todo-item-dto.cs` defaults, and the `Note` field's `= string.Empty`
style (no presence rule → not a TWPA violation; it's the optional-field-style question).

## 4. Objective bugs in the skill (not opinions — fix regardless of the vote)

These are wrong against *both* remaining repos and/or won't compile. Listed for completeness; they
are not up for debate.

1. **Empty `Command` body + `I*Details` won't compile.** `references/examples.md` Tier‑2 shows
   `Command : IApiRequest, ISecurityRoleDetails, IRequest<…>;` with an empty body. Interface data
   properties must be *declared* on the class; `[RouteMixin]` generates only route params + `GetRoute()`/
   `GetHttpVerb()`. Real `CreateRole.Command` / `CreateSecurityRole.Command` both declare the props.
   Empty body is valid **only** for a plain route-only `IApiRequest` query with no data interface.
2. **"MediatR"** → both repos use **TimeWarp.Mediator**. (GLM sharpens: this appears in the skill's
   *Detection* table at `SKILL.md:36`, so it doesn't just misname a dependency — an agent that has
   only seen `TimeWarp.Mediator` won't recognize a contract from that row. It breaks the on-ramp.)
3. **Folder-rule self-contradiction** — see Decision 2 below; regardless of outcome the skill's flat
   "folder singular" is wrong for TWA and half of copic.
4. **Discovery commands are broken on TWA (case-sensitive PascalCase).** `SKILL.md` /
   `references/examples.md` tell agents to `rg … --glob '**/Features/**'`. TWA has **0** `Features/`
   dirs and **1** `features/` — the glob matches nothing here. Make discovery case-insensitive and
   show both casings.

## 5. Decisions that need opinions

For each: the options, the trade-off, and the author's lean. **Reviewers: agree, disagree, or
propose a third option — with reasoning.**

> **Vote status (3 reviewers: Author + Composer + GLM 5.2).** GLM was prompted adversarially and
> **broke the unanimity on 3 of 8** (its claims were re-verified against code before acceptance). Tally:
>
> | # | Topic | Author | Composer | GLM 5.2 | Status |
> |---|---|---|---|---|---|
> | 1 | casing | discover/kebab | discover/kebab | agree — **kebab canonical, Pascal = 1-line "mirror if repo is Pascal"** (not symmetric) | **3–0** (GLM tightens) |
> | 2 | plurality | plural | plural | plural — but winning arg is **TWA-consistency**, not "copic is mixed" | **3–0** |
> | 3 | contract tests | new project | new project | **DISSENT** — round-trip on auto-property POCOs is tautological; require a test only when contract uses `required`/`init`/custom converter/non-default ctor | **2–1** ⚠️ |
> | 4 | assertions | Shouldly | Shouldly | **third option** — parameterize; `BeEquivalentTo` semantics differ; **FluentAssertions v8 is commercially licensed** → still anti-FA | 3–0 *(anti-FA)*, GLM: don't hard-code either |
> | 5 | Create Response | mixed | mixed | mixed — **discriminator must be "has invariants", NOT "trivial/id-only"** (`required init` skips Guard → `Guid.Empty` hole, copic `CreateModule.cs:31`) | **3–0** (GLM fixes the axis) |
> | 6 | `IAuthApiRequest` | promote | promote | **DISSENT** — copic's server-side derivation is a valid competing design; TWA itself is split attribute-vs-manual; name is renamed by 053-002 → **"document as available (both forms), hold 'canonical' until post-rename"** | **2–1** ⚠️ |
> | 7 | nullability | keep+fix | keep+fix | keep+fix — but **split the rule**: `= string.Empty`+`NotEmpty` is forbidden (real silent bug); `string?`+`NotEmpty` is only *discouraged* (functional, just disarms the compiler) | **3–0 → RESOLVED & ENFORCED** (§3.6: TWPA0002/0003; severity note) |
> | 8 | 053-002 sequencing | rename first | rename first | **DISSENT** — package/release coupling makes "rename first" a downstream-template-breaking gate → **third option: rewrite skill against target name `[Route]` with a migration note, clean contracts against current `[RouteMixin]`, do 053-002 whenever; don't gate cleanup on it** | **2–1** ⚠️ |
>
> **GLM's cross-cutting point:** Decisions **6 and 8 are coupled** — `[IAuthApiRequestMixin]` is one of
> the names 053-002 renames, so "promote now" + "rename first" entangle. Both prior ballots treated them
> as independent. The three contested rows (3, 6, 8) are the real open questions now.

### Decision 1 — Folder & file casing: kebab vs Pascal
- **Options:** (a) kebab-case (`features/admin/roles/commands/create-role.cs`) — matches all of TWA;
  (b) PascalCase — matches copic, the skill, and the TWA *docs*.
- **Trade-off:** The skill lives in TWA and TWA is 100% kebab. But the skill is meant to be
  reusable across TimeWarp solutions (copic is Pascal). A single skill can't hard-code one casing.
- **Author lean:** Skill should say **"match the repo's existing casing (discover first)"** and use
  kebab in TWA-anchored examples; stop prescribing a global casing. Fix TWA docs' Pascal→kebab.

### Decision 2 — Folder plurality rule
- **Options:** (a) **Plural** always (`roles/`) — TWA + TWA docs; (b) **Singular** entity folder with
  `Commands/`+`Queries/` subfolders (`SecurityRole/`) — skill + part of copic; (c) allow either, rule
  is "namespace is always plural; folder mirrors the entity."
- **Trade-off:** Copic is internally inconsistent (both), so it can't be cited as authority. TWA is
  consistently plural. The "singular folder / plural namespace" story in the skill is elegant but
  unsupported by TWA.
- **Author lean:** **Plural folders** (match TWA + docs), drop the "intentional singular mismatch"
  narrative. Namespace plural (unchanged).

### Decision 3 — Contract serialization tests: dedicated project vs integration
- **Options:** (a) **Create `tests/.../web-contracts-tests`** (Fixie) with `SerializeAndDeserialize`
  round-trips — matches copic + the skill's prescription, fast, no host; (b) **Keep** round-trips in
  `web-spa-integration-tests/Serialization/` and correct the skill to say so.
- **Trade-off:** (a) is the better pattern and makes "follow the skill" literally true, but adds a
  new project + CI wiring. (b) is zero-cost but weakens the skill's testability story.
- **Author lean:** **(a)** create the project — it's small, fast, and is the pattern copic proved out.

### Decision 4 — Assertion library in contract tests
- **Options:** (a) **Shouldly** — TWA repo-wide standard; (b) **FluentAssertions** — copic + skill.
- **Trade-off:** Skill examples currently teach FluentAssertions; TWA uses Shouldly everywhere else.
  Mixing libraries in one repo is a smell.
- **Author lean:** **Shouldly** in TWA-anchored examples; note FluentAssertions is the copic dialect.

### Decision 5 — `Create` Response shape: `required init` vs ctor+`Guard`
- **Options:** (a) `required init` (copic `Create*`); (b) ctor+`Guard.Against` (TWA roles, skill/docs
  for responses generally).
- **Trade-off:** `required` is terser and compiler-enforced at construction; ctor+`Guard` gives
  runtime invariant checks + is uniform with `Get`-for-edit responses. Copic uses BOTH (create=required,
  get=ctor) — arguably by intent (create response is a trivial id echo).
- **Author lean:** **ctor+`Guard` as default; `required init` acceptable for trivial id-only Create
  responses.** Document both with the "when."

### Decision 6 — `IAuthApiRequest` (TWA-only) — promote to canonical?
- Copic has no auth request interface; TWA added `IAuthApiRequest` (+ `AuthApiRequestValidator`,
  `UserId { get; set; }`), used by 6 contracts. It's a genuinely useful pattern (BFF passes `UserId`
  so the mock API can tailor responses; server re-validates the token).
- **Author lean:** **Yes** — document `IAuthApiRequest` as a first-class variant in the skill.

### Decision 7 — Nullability rule vs reality (copic's violations, TWA's 3 features)

> **RESOLVED & ENFORCED (2026-07-02).** Rule kept; TWA's violators fixed; the rule is now a
> compile-time guard (`TWPA0002`/`TWPA0003`, `timewarp-architecture-contract-analyzers`, wired into
> `web-contracts` under warnings-as-errors). Binding premise empirically confirmed by the RoleForm
> demonstrator. See §3.6 — including the deliberate severity divergence from GLM's
> forbidden-vs-discouraged split. What remains for the skill rewrite is *teaching* the rule (with
> GLM's split for cross-repo use) and citing the analyzer as the enforcement mechanism.

- The skill **forbids** `string?`+`NotEmpty()`; copic breaks it, TWA in `todo-items`, `hello`,
  `analytics`. Options: (a) keep the rule, treat all violations as tech debt to fix; (b) relax the
  rule to match copic's lived practice.
- **Author lean:** **Keep the rule** (it prevents a real silent-data bug — see
  `references/nullability.md`) and fix TWA's violators. Copic is out of scope but should be noted as
  the cautionary example the rule exists to prevent.

> **Exact copic violation inventory (corrects the earlier "17 files" — that was a coarse `grep -l`
> co-occurrence that swept in ~12 Query/Response files where the `string?` and the `NotEmpty()` are on
> *different* properties).** The real `string?` + *unconditional* `NotEmpty()` violations live in
> **5 files** under `copic/main/Source/ContainerApps/Web/Web.Contracts/Features/`:
>
> | File | Violating `string?` props (line) |
> |---|---|
> | `Admin/SecurityRole/SecurityRoleDetails.cs` | `Name` (30), `Code` (32) — *`Description` (31) is `MaximumLength` only, NOT a violation* |
> | `Admin/SecurityRole/Commands/CreateSecurityRole.cs` | `Name`, `Code` — Command redeclares the `string?` props and its `Validator` repeats the inline `NotEmpty()` (a *second* copy of the same two) |
> | `Admin/Modules/ModuleDetails.cs` | `Name` (22), `Description` (23), `Code` (24) |
> | `Admin/Applications/ApplicationDetails.cs` | `Name` (15), `AdminUrl` (16) |
> | `Incidents/Commands/CreateIncidentReport.cs` | nested sub-validators: contact `FirstName`/`LastName`/`EmailAddress` (36–38), a 2nd contact `FirstName`/`LastName`/`EmailAddress` (49–51), patient `FirstName`/`LastName` (63–64), `StateWhereEventOccurred` (77–79) |
>
> **Count:** ~**7 canonical property-level violations** in the Admin `*Details` interfaces (9 if you
> also count `CreateSecurityRole`'s redeclared pair), **plus ~9** in `CreateIncidentReport`'s nested
> sub-types (same property *names* repeated across Reporter/Insured/Patient/Event). So neither "12"
> nor "17" is a clean figure — the honest statement is **5 files**, listed above. *(`UpdateSecurityRole`,
> `UpdateModule`, `UpdateApplication`, and the `Get*` queries flagged by the old grep are NOT
> violations — they either compose the shared `*DetailsValidator` (inheriting, not re-adding, the
> issue) or have `string?` and `NotEmpty()` on unrelated properties.)* Copic is **frozen** — this is
> inventory for the cautionary example, not a cleanup list.

### Decision 8 — Sequencing: do the `053-002` attribute rename *before* the contracts cleanup?
- **Context:** The contracts cleanup (§7) touches every contract file; the skill rewrite teaches the
  attribute name; `053-002` will rename `[RouteMixin]`→`[Route]`/`[ApiRoute]` (+ friends) across all
  the same files, the FastEndpoint generator's by-name match, and the published foundation package.
- **Options:** (a) **Do 053-002 first**, then clean contracts + rewrite the skill against the final
  name (touch each file once, teach the final name); (b) clean up now against `[RouteMixin]`, do the
  rename later (two passes over every contract, skill churns twice); (c) leave the name as-is —
  decide 053-002 is "no" and keep `[RouteMixin]` forever.
- **Trade-off:** (a) is fewer total edits and avoids teaching a soon-dead name, but front-loads a
  package-API change (version bump + generated-template coordination). (b) unblocks the cleanup
  immediately but guarantees rework. The name also feeds Decision 6 (`[AuthApiRequest]` reads far
  better than `[IAuthApiRequestMixin]`).
- **Author lean:** **(a) — resolve 053-002 first** (rename to `[Route]` + `[AuthApiRequest]`), *then*
  do the skill rewrite and repo cleanup once, against the final names.

## 6. Proposed end-state (contingent on the votes)

0. **(If Decision 8 = rename first) resolve 053-002** — rename `[RouteMixin]`→`[Route]`/`[ApiRoute]`,
   `[IAuthApiRequestMixin]`→`[AuthApiRequest]`, etc., across contracts + the FastEndpoint generator's
   by-name match + the foundation package (version bump). Everything below then uses the final names.
1. **Skill becomes the single corrected spec** — fix §4 bugs (incl. case-insensitive discovery);
   resolve §5 per consensus; add the route-attribute / `IApiRequest` / `IAuthApiRequest` /
   **`[IOpenDataQueryParametersMixin]`** / `MockResponseFactory` sections; add **mock-pattern
   detection** (contract-local `GetMockResponseFactory()` vs copic's SPA `*MockFactory` classes) so
   agents don't copy the wrong shape; re-anchor examples to TWA's real `admin/roles` (good) and cite
   `todo-items` (anti-pattern).
2. **TWA docs aligned** to the skill — add the source-generator layer; casing fix; and fix the
   concrete doc bugs in §3.5 (broken `UpdateUser` sample, "mutability and nullability" copy-paste,
   `IReadonlyList<t>` typo).
3. **TWA repo cleanup** — a Kanban task: fix `todo-items` (`sealed`→`static`, `string.Empty`+`NotEmpty`
   → `string`+`null!`, `init`→`set` on bindables, finish/remove the two empty query stubs),
   `hello` + `analytics` nullability; optionally add `web-contracts-tests`.

> **Skill-location caveat:** task 053-002 refers to the skill as
> `timewarp-flow/master/skills/webapi-contracts` (no hyphen), while the copy under review here is
> `skills/web-api-contracts` (hyphenated) in this repo. Confirm which is canonical / whether one syncs
> from the other **before** editing, so the corrected spec doesn't get overwritten by a sync.

## 7. Repo cleanup scope (TWA) — concrete offender list

> **Progress (2026-07-02):** all **nullability** items below are ✅ fixed and analyzer-guarded
> (§3.6, tasks 077+080). Struck-through = done; the rest are still open, one decision-slice each.

- `features/todo-items/**` — `sealed partial` shells; ~~`create-todo-item.cs` +
  `update-todo-item.cs` `Title` `= string.Empty` with `NotEmpty()`~~ ✅; `todo-item-dto.cs` defaults
  + `Note` `= string.Empty` style (no presence rule → optional-field-style question, not a TWPA
  violation); `init` on bindable props; `search-todo-items.cs` + `get-todo-item-by-id.cs` empty
  stubs; `Response : BaseResponse`.
- ~~`features/hello/hello.cs` — `string?` + `NotEmpty()`~~ ✅ (`Name` → `string` + `= null!`).
- ~~`features/analytics/track-event.cs` — `string?` + `NotEmpty()`~~ ✅ (`EventName` → `string` + `= null!`).
- `features/admin/roles` — conforms, **except** the `update-role.cs`/`get-role.cs` route wart found
  by 078: `api/Role/{RoleId:int}` (singular route + `int` route param vs `Guid` everywhere else).
  Tracked with the server-side slice in kanban task 079.
- Everything else (`profile`, `auth`, `authentication`, `chat`) already conforms.

**Open sub-question:** is `todo-items` a real feature or a template placeholder? Its two empty query
stubs suggest it's half-built — that changes fix-in-place vs finish vs delete.

---

## Reviewer opinions

Append your review below. Keep the matrix + evidence above intact; add your voice here.

### Template
```
### <agent/model name> — <date>
**Overall:** <one line>
**Decision 1 (casing):** <agree kebab | Pascal | other> — <why>
**Decision 2 (plurality):** <plural | singular | either> — <why>
**Decision 3 (tests):** <new project | integration> — <why>
**Decision 4 (assertions):** <Shouldly | FluentAssertions> — <why>
**Decision 5 (Create Response):** <ctor+Guard | required | mixed> — <why>
**Decision 6 (IAuthApiRequest):** <promote | omit> — <why>
**Decision 7 (nullability):** <keep+fix | relax> — <why>
**Decision 8 (sequencing vs 053-002 rename):** <rename first | cleanup first | keep RouteMixin> — <why>
**Anything the author missed:** <...>
```

<!-- reviewers add entries here -->

### Composer (Grok) — 2026-07-01
**Overall:** RFC is the right framing — treat skill bugs as fixed facts, conventions as votes; my prior analysis underspecified lineage and missed compile-time skill errors.

**Decision 1 (casing):** **Discover-first; kebab in TWA examples** — The skill ships inside a repo that is entirely kebab-case. Prescribing PascalCase in examples teaches the wrong dialect for the primary consumer. Cross-repo portability belongs in a "mirror existing casing" rule, not in example paths.

**Decision 2 (plurality):** **Plural** — I initially wrote "folder follows local repo" to be diplomatic about copic's mix. RFC convinced me: TWA is consistently plural, docs say plural, and singular `Admin/SecurityRole/` in copic is inconsistency not intent. Namespace plural stays the hard invariant.

**Decision 3 (tests):** **New project** — I had softened to "conditional if `*Contracts.Tests` exists" which lets TWA stay non-compliant forever. A dedicated Fixie project is cheap, fast, and is what makes the skill's test step literally true for the template.

**Decision 4 (assertions):** **Shouldly** — TWA uses Shouldly repo-wide; teaching FluentAssertions in skill examples imports a second assertion dialect for no gain. Copic dialect can be a footnote.

**Decision 5 (Create Response):** **Mixed** — ctor+`Guard` for anything with invariants; `required init` acceptable for trivial Create id echoes. Copic already does this split; pretending one shape fits all fights real usage.

**Decision 6 (IAuthApiRequest):** **Promote** — Genuinely useful BFF pattern (mock tailoring + server re-validation). Absence in copic is omission, not rejection.

**Decision 7 (nullability):** **Keep+fix** — The `string?`+`NotEmpty()` rule prevents a real silent-data bug. Relaxing to match copic's violations would encode tech debt as guidance. *[Author note: Composer & GLM both cited "17 files" from the author's original coarse grep; the verified figure is **5 files** — exact inventory in Decision 7.]*

**Decision 8 (sequencing vs 053-002 rename):** **Rename first** — Cleaning contracts and rewriting the skill against `[RouteMixin]` then renaming to `[Route]`/`[AuthApiRequest]` is guaranteed double churn across contracts, the FastEndpoint generator match, the foundation package, and skill text. Front-load the rename or explicitly cancel it.

**Anything the author missed:**
- **Tier-2 empty `Command` body** is worse than plurality drift — it will not compile with `I*Details`; fix before any agent uses examples.md as a paste source.
- **Mock factory "required"** should downgrade to "when SPA mock mode needs this endpoint" — both repos have dict fallback to real API.
- **`todo-items` disposition** (finish vs delete) blocks cleanup scope — the two empty query stubs look like abandoned template placeholder, not a feature to bring into compliance.
- **Skill sync path** (§6 caveat) should be resolved before edits — agree this is a gate, not a footnote.

### GLM 5.2 (OpenCode) — 2026-07-01
*Prompted adversarially; ran over the real repos + copic. Full review with reasoning:
[`glm52-review.md`](glm52-review.md). All falsifiable claims below were re-verified by the author
before acceptance.*
**Overall:** Consensus is mostly right but soft on three fronts — over-values serialization-test
ceremony, under-risks the published-package rename, and universalizes a one-repo auth pattern.
**Decision 1 (casing):** agree, but **kebab is canonical + Pascal is a 1-line "mirror if the repo is Pascal"** — not a symmetric "either" (a hedged skill is a questionnaire, not a spec).
**Decision 2 (plurality):** **plural** — but the winning argument is TWA-consistency; copic being *mixed* means it can't vouch for plural. `SKILL.md:46` + `examples.md:185-188` become dead text and must be *deleted*, not amended.
**Decision 3 (tests):** **DISSENT** — round-trip of auto-property POCOs under default STJ is tautological; the only real catch (camelCase policy) is one global integration test that already exists. Require a contract test **only** when the contract uses `required`/`init`/custom converters/non-default ctors; otherwise none. Don't call TWA non-compliant for lacking the project.
**Decision 4 (assertions):** **third option** — don't hard-code either; `BeEquivalentTo` (structural) vs Shouldly (referential) genuinely diverge, and **FluentAssertions v8 is commercially licensed (2025)**. Shouldly for TWA examples, FA inline for copic, described as the repo's library.
**Decision 5 (Create Response):** **mixed** — but discriminator = **"has domain invariants"**, NOT "trivial/id-only": `required init` skips `Guard`, so it can't reject `Guid.Empty` (copic `CreateModule.cs:31` = the live hole). Reviewers fixed the wrong axis.
**Decision 6 (IAuthApiRequest):** **DISSENT** — copic derives the user server-side (a valid competing design; promoting silently declares it wrong); TWA is itself split attribute-vs-manual; and `[IAuthApiRequestMixin]` is renamed by 053-002. **Document as *available* (both forms + trigger); hold "canonical" until post-rename.**
**Decision 7 (nullability):** **keep+fix**, but **split the rule** — `= string.Empty`+`NotEmpty` is *forbidden* (real silent bug; TWA `create-todo-item.cs:11`); `string?`+`NotEmpty` is only *discouraged* (functional, just disarms the compiler). Not equal sins.
**Decision 8 (053-002 sequencing):** **DISSENT (third option)** — the match is a *hardcoded* string in `endpoint-metadata.cs:31` **and** the attribute ships in a published package, so "rename first" gates all cleanup behind a downstream-template-breaking release. Instead: **rewrite the skill against the target name `[Route]` + a migration note, clean contracts against the current `[RouteMixin]`, run 053-002 whenever it's ready.** The double-edit is ~30s; the gate is weeks.
**Cross-cutting / missed:** Decisions 6 & 8 are **coupled** (both hinge on the pre-rename attribute name) and both prior ballots flattened that. Also: the two auth forms are non-equivalent (attribute synthesizes `private GetAuthQueryParameters()`, `contracts-mixin-generator.cs:193`); FA licensing; `"MediatR"` sits in the *Detection* table (`SKILL.md:36`) so it breaks recognition, not just naming.
