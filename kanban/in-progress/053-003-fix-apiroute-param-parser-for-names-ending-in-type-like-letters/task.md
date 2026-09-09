# Fix ApiRoute param parser for names ending in type-like letters

## Description

Foundation **ContractsMixinGenerator** (bundled with `TimeWarp.Foundation.Contracts`) mis-parses
route parameters whose names end with characters that look like type constraints.

**Repro (Crunchit portal BFF contracts, epic 033):**

```csharp
// Broken — generator treats trailing d/e as type constraints
[ApiRoute("api/ccc/locations/{LocationId}/exports/{Date}/validate", HttpVerb.Post)]
// Emits mangled property names / types: LocationI + type d, Dat + type e

// Workaround used in production contracts
[ApiRoute("api/ccc/locations/{LocationId:string}/exports/{Date:string}/validate", HttpVerb.Post)]
```

Same pattern required for bare `{ClientId}`, `{StaffId}` when similar collisions occur; Crunchit
uses `:string` proactively on all string route params for safety.

## Requirements

- Parse `{ParamName}` without consuming trailing letters of the identifier as a type.
- Support explicit `{ParamName:type}` constraints without breaking bare names.
- Regression tests for: `LocationId`, `Date`, `ClientId`, `StaffId`, `UserId`, and mixed templates.
- Document constraint grammar (which type tokens are recognized).

## Checklist

- [x] Locate regex / parser in foundation-contracts generators (`ContractsMixinGenerator` lineage)
- [x] Fix identifier vs constraint tokenization
- [x] Unit/generator tests for the parameter names above
- [x] Release note + bump Foundation.Contracts package for consumers (Crunchit on beta.5+)
- [x] Implementation review (effort 1, general) — disposition `clean`

## Notes

- **Severity:** High — silent wrong codegen for common param names (`Date`, `LocationId`).
- **Owner:** foundation-contracts / TimeWarp.Foundation.Contracts generators.
- **Consumer:** Crunchitfs/crunchit (033-003 CCC routes; all `:string` constrained routes in clients/staff).
- **Discovered:** Crunchit task 033-003 / catalogued 033-007.
- **Workaround:** `{Name:string}` remains valid; it is no longer required for bare string params
  once consumers are on Foundation.Contracts **2.0.0-beta.17** (or later).
- **Sibling (out of scope):** `PageSourceGenerator` in Architecture.Generators uses the same
  optional-colon regex (`\{(\w+)\s*:?(\w+)\}?`). Not shipped in Foundation.Contracts; left for a
  follow-on.

## Session

- Implementer: Grok session 01a0856a-8503-7040-8a70-6de6915df8df (2026-09-09)
- Review oracle: Grok session 01a08574-94c1-7733-97ba-b26d532346f3 (2026-09-09)
- Reviewer (general): Grok subagent 01a08576-12da-79e1-82aa-dcb574a6c222 (2026-09-09)

## Results

`ContractsMixinGenerator` required a colon before a type/constraint token. The previous regex
`\{(\w+)\s*:?(\w+(\(\d+\))?)\}?` made the colon optional while still requiring a second `\w+`, so
`{Date}` became `Dat` + type `e` and `{LocationId}` became `LocationI` + type `d`.

The tokenizer is now `{Name}` or `{Name:constraint}` (`\{(\w+)(?:\s*:\s*(\w+(?:\(\d+\))?))?\}`).
Bare names default to `string`. Explicit `{Name:guid}` / `{Name:datetime}` / `{Name:min(1)}` /
`{Name:string}` keep the previous C# type mapping.

### Files changed

- `source/foundation/foundation-contracts-generators/contracts-mixin-generator.cs` — regex +
  `MapConstraintToClrType` + Design region grammar
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-mixin-generator-tests.cs`
  — bare `LocationId`/`Date`/`ClientId`/`StaffId`/`UserId`, explicit constraints, mixed template
  (Crunchit 033-003 repro)
- `skills/tw-web-api-contracts/SKILL.md` — constraint grammar table + pitfall
- `documentation/developer/how-to-guides/web-api-contracts/how-to-write-bff-api-contracts.md` —
  same grammar
- `documentation/release-notes.md` — 2.0.0-beta.17 Foundation.Contracts fix

### Key decisions

- **No extra version bump.** `<Version>` and platform CPM pins are already `2.0.0-beta.17`;
  latest published tag is `v2.0.0-beta.16`. This fix ships in beta.17. Bumping to beta.18 would
  skip the already-queued next release. Crunchit should consume **≥ 2.0.0-beta.17** once that
  release is published.
- `{Name:string}` stays valid (stripped from `RouteTemplate`, same as before).
- Page-route tokenizer not fixed here (different package).

### Test outcomes

- `ContractsMixinGenerator_Tests`: 6 passed (3 existing + 3 new)
- Full `timewarp-architecture-sourcegenerator-tests`: 63 passed, 0 failed
- `dotnet run tools/dev-cli/dev.cs -- build`: 0 Warning(s), 0 Error(s)

### How to validate

**Smoke**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests
dotnet test -c Release -- --filter-class ContractsMixinGenerator
```

**Expect**

- Test run summary: Passed, total 6, failed 0
- `Should_Keep_Bare_Param_Names_That_End_With_Type_Like_Letters` emits
  `public string Date { get; set; }` and `public string LocationId { get; set; }` (not `Dat`/`e`
  or `LocationI`/`d`)
- `Should_Parse_Mixed_Bare_And_Constrained_Params` keeps the Crunchit template
  `api/ccc/locations/{LocationId}/exports/{Date}/validate/{ClientId:guid}/{StaffId}/{UserId}`
  with `string`/`string`/`Guid`/`string`/`string` properties

**Automated gate**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: 63 passed, 0 failed

dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)
```

**Depends on:** none (host-free generator driver).

**Not in scope:** nuget.org publish of 2.0.0-beta.17; Crunchit dropping `{Name:string}` until that
package is on the feed; `PageSourceGenerator` optional-colon regex.

### Review disposition

- **Rounds:** 1 · **Effort:** 1 · **Roster:** general
- **Counts (final):** bug 0 / suggestion 0 / nit 0 (all open=0, fixed=0, wontfix=0)
- **Disposition:** `clean` — no findings raised
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/disposition.md`
