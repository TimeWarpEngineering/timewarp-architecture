# Round 1 — merged findings
**Date:** 2026-11-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/analyzers/details-copy-contracts-tests.cs:1
- Description: The tests that cover From, Clone, and the Version hook on the real contracts are in
  a standalone runfile that CI never runs. `dev test` runs only `*.csproj` under `tests/`, and
  `web-jaribu-tests` compiles only `features/**/*-tests.cs` and `platform/**/*-tests.cs`. The
  acceptance tests (Avatar survives, GetRole get-only lists copied, Version 7/8 through
  `CopyBeyondDetails`) were run once by hand, and nothing guards them against regression.
- Suggestion: Move them into co-located runfiles: the existing `get-profile-tests.cs` and
  `update-site-settings-tests.cs`, and a new `get-role-tests.cs`. Another option is
  `web-contracts-tests`. Delete the stray runfile and update `### How to validate`.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/details-copy-analyzer.cs:191, :306;
  source/foundation/foundation-contracts-generators/details-copy-symbols.cs:271
- Description: An explicit `object ICloneable.Clone() => this` is not found by
  `GetMembers("Clone")`, so TWA0047 does not fire. The generator then emits a public `Clone` that
  `ICloneable` dispatch never calls. TimeWarp.State's snapshot shares the mutable response, and
  the build says nothing.
- Suggestion: Resolve Clone through `FindImplementationForInterfaceMember(ICloneable.Clone)`, or
  through methods whose `ExplicitInterfaceImplementations` include it. Do this in the analyzer
  (identity check, location, `HasClone`) and in the generator's `HasClone`. Add an analyzer test
  for the explicit form.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: details-copy-analyzer.cs:296-409; details-copy-symbols.cs:180-330
- Description: The analyzer and the generator each have their own copy of the I*Details, mutable,
  partial-chain, From, Clone, and constructor predicates. `ContractAgreementAnalyzer` has a third
  copy of `IsDetailsInterface`. Comments are the only thing that keeps them in agreement, which
  goes against the AGENTS.md rule "generate one from the other or add a build check". M2 is the
  same blind spot written twice.
- Suggestion: Move them to one linked file under `source/analyzers/shared/`, the existing
  linked-source pattern, and use it from the convention analyzers, `ContractAgreementAnalyzer`, and
  foundation-contracts-generators.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer. M3 is listed separately from M2: M2 is the behavior defect, and M3 is
  the structural cause that let it be written twice.
