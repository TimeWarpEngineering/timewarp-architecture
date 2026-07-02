# Broaden contract nullability analyzer to api / grpc / foundation contracts

## Description

Task 080 shipped `ContractNullabilityValidatorAnalyzer` (TWPA0002: `string?` + `NotEmpty()`/
`NotNull()`; TWPA0003: `= string.Empty`/`= ""` + presence rule) in the analyzer-only assembly
`source/analyzers/timewarp-architecture-contract-analyzers/`, but wired it into **`web-contracts`
only** (task 077's scope). Extend enforcement to the remaining contract/validator surfaces so the
whole template is guarded, using the proven 080 recipe: **wire + fix everything it reports in the
same PR** (warnings-as-errors makes violations build-breaking, so the tree must go green in one
change).

Candidate projects (confirm each actually contains `AbstractValidator<T>` usage before wiring —
wiring a project with no validators is harmless but pointless):

- `source/container-apps/api/api-contracts/`
- `source/container-apps/grpc/grpc-contracts/`
- `source/foundation/foundation-contracts/`
- Consider also non-contract projects that declare validators against bindable shapes
  (e.g. `web-spa`? server-side validators?) — discover with
  `grep -rl "AbstractValidator<" source/ --include=*.cs`.

## Checklist

- [ ] Inventory: which projects have `AbstractValidator<T>` + request/DTO shapes.
- [ ] Wire the `<ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`
      into each (copy the `web-contracts.csproj` block, incl. the comment).
- [ ] Build each; fix every TWPA0002/0003 reported (same-axis fixes only: type annotation +
      initializer; do not touch mutability/shape).
- [ ] Full `dev build` green (0/0) in the same PR.
- [ ] Note in the template docs (or the skill, via [[081-rewrite-web-api-contracts-skillmd-per-rfc-resolutions]])
      that new contract projects must include the analyzer reference.

## Stretch (own judgment whether here or split out)

- [ ] `SetValidator(new XDetailsValidator())` composition-following (080 deferred it — currently
      shared `AbstractValidator<I*Details>` are analyzed directly against the interface, which
      covers TWA's real usage; composition-following matters only if a concrete command's props
      could *mask* the interface's, e.g. property hiding).
- [ ] Code-fix provider: TWPA0002 → strip `?` + add `= null!`; TWPA0003 → `= string.Empty` → `= null!`.

## Notes

- Recipe, rationale (analyzer-only assembly vs the generator-carrying one), and severity note live in
  [[080-roslyn-analyzer-flag-validator-presence-rule-vs-declared-nullability-contradictions-in-contracts]]
  and the RFC §3.6 ([[contract-conventions-rfc]]).
- Template consideration: feature-flag preprocessing (`#if (api)`, `#if (grpc)`) means generated apps
  may drop these projects — the analyzer reference must live inside the same conditional regions as
  the projects themselves (it does if it's in each csproj; just don't add solution-level wiring).
