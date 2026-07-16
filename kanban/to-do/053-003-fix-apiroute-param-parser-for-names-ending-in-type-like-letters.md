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

- [ ] Locate regex / parser in foundation-contracts generators (`ContractsMixinGenerator` lineage)
- [ ] Fix identifier vs constraint tokenization
- [ ] Unit/generator tests for the parameter names above
- [ ] Release note + bump Foundation.Contracts package for consumers (Crunchit on beta.5+)

## Notes

- **Severity:** High — silent wrong codegen for common param names (`Date`, `LocationId`).
- **Owner:** foundation-contracts / TimeWarp.Foundation.Contracts generators.
- **Consumer:** Crunchitfs/crunchit (033-003 CCC routes; all `:string` constrained routes in clients/staff).
- **Discovered:** Crunchit task 033-003 / catalogued 033-007.
- **Workaround:** always use `{Name:string}` (or other explicit type) until fixed.
