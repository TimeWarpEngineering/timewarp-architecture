# Round 2 — general
**Date:** 2026-11-05
**Scope reviewed:** fix pass 1, commit 76f95a527 against 0b8af0e1e (round-1 base). Product paths:
`contract-agreement-types.cs`, `contract-agreement-projection.cs`, `contract-agreement-analyzer.cs`
(Design only), `computed-attribute.cs`, `AnalyzerReleases.Unshipped.md`,
`feedback-activity-entry-contracts.cs`, `tw-aggregate-pattern/SKILL.md`, and the three analyzer
test files.

## Summary

The fix pass resolves all three round-1 findings. M1: `ContractAgreementTypes.ReadCompatible`
accepts a response `T?` against a root `T` (both `Nullable<T>` and annotated reference types).
Only the TWA0041 projection walk calls it. TWA0039 (`Disagreement`) and TWA0040 (the Set element
check) still call the strict `Compatible`. `[Computed]` is gone from
`FeedbackActivityEntry.AuthorPrincipalId` in the product and in the fixture, so that member is
checked now. M2: `TryGetWrapper` picks a wrapper only when the response has exactly one
non-`[Computed]` member, that member has no root member, and its type is a contract-assembly
class (or a list of one). Otherwise the response projects directly and an unmatched member is
reported itself. This rule is stricter than before, so it cannot hide drift that the old rule
caught. M3: the `ComputedAttribute` Design and summary name child classes and wrapped classes.
I found no new defects in the delta.

## Verification (re-run by reviewer)

- Analyzer tests: 305/305 passed, 0 failed. The five new tests are included, along with the
  retargeted child-entry test and the 474 Profile test.
- `web-application` `dotnet build`: 0 warnings, 0 errors. The product links `FeedbackItem` with
  the widened `AuthorPrincipalId` and no `[Computed]` on it.
- `dev lint-skills`: 9 skills passed. Evals are valid.
- Read-side widening is one-way. `ReadCompatible` calls `Compatible` first, unwraps `Nullable<T>`
  only when the root is not nullable, and drops the reference annotation only when the root is not
  annotated. Narrowing (a contract `Guid` against a root `Guid?`) still fails. The test
  `Given_Child_Entry_Narrows_Root_Nullable_Flags_TWA0041` covers it.
- Write side stays strict. `Given_Details_Widens_Root_To_Nullable_Flags_TWA0039` gets only
  TWA0039. The response's widened `Title` passes TWA0041, as intended.
- The retargeted 474 test is correct. `GetProfile.Response.Email` (`string?` against the root's
  `string`) is read widening now, so the test expects TWA0039 alone.
- Line caps: the changed files are under 500 lines. `contract-agreement-projection.cs` has 148
  and the feedback analyzer tests have 233.
- The Designs agree with each other and with the code: the types Design (write side vs read
  side), the projection Design (one wrapper), the analyzer Design (TWA0041 widening), the
  `tw-aggregate-pattern` "Contract agreement" section, and the TWA0041 row in AnalyzerReleases.

## Issues

None.

Not raised as a finding: read widening applies to the leaf type and the top-level `Nullable<T>`
only. Collection elements still go through the strict `Compatible`, so `List<string?>` against a
root `List<string>` would be drift. The product has no such shape. The Designs describe widening
as `T` to `T?` and do not mention elements, so this is a limit of the rule, not a defect.
