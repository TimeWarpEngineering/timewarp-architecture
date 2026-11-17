# Round 1 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-activity-entry-contracts.cs:29
- Description: `[Computed]` on `FeedbackActivityEntry.AuthorPrincipalId` exempts a member that does
  project from `FeedbackActivity.AuthorPrincipalId`. The handler nulls it only on the filer's copy.
  The `ComputedAttribute` Design limits the attribute to handler-derived values that have no root
  member, so its only effect here is to make TWA0041 pass. TWA0041 then stops checking that member.
- Suggestion: In the TWA0041 projection walk only, accept a contract `T?` against a root `T` (read
  widening, for redaction), and keep TWA0039 strict. Then:
  - Remove the `[Computed]`.
  - Retarget the affected analyzer test, and add widened-is-clean and details-still-strict tests.
  - Scope "`Guid?` against `Guid` is still drift" to the write side in the
    `contract-agreement-types.cs` Design and in `tw-aggregate-pattern`.

  The alternative is an operator wontfix that keeps the strict rule. It must also change the
  `ComputedAttribute` Design to cover a redacted root value.
- Source: general
- Disposition notes: Fixed (fix pass 1). TWA0041 now uses `ContractAgreementTypes.ReadCompatible`: a
  response may widen a root `T` to `T?` (Nullable<T> and reference nullability); TWA0039/TWA0040
  stay strict. `[Computed]` removed from `FeedbackActivityEntry.AuthorPrincipalId` in the product
  and the fixture. Tests: child-entry disagreement retargeted to `int`/`long`; new widened-is-clean
  (`string?` vs `string`), narrowing-still-drifts (`Guid` vs root `Guid?`), and details-still-strict
  (`string? Title` on `IFeedbackItemDetails`). The 474 Profile nullability test now expects TWA0039
  only. Designs updated in `contract-agreement-types.cs`, `contract-agreement-analyzer.cs`,
  `contract-agreement-projection.cs`, `tw-aggregate-pattern`, and AnalyzerReleases.

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-agreement-projection.cs:70
- Description: The Design describes the wrapped projection as one member. The code treats every
  unmatched contract-class member of a non-list response as a wrapper. That gives misleading
  messages: if `GetFeedback.Response.Attachments` loses `[Computed]`, each `Attachment` member is
  reported, not `Attachments` itself. It can also hide drift when the class's members are a subset
  of the root's. No test covers either case.
- Suggestion: Allow one wrapper per response, the only unmatched non-computed contract-class
  member, and report the rest as missing. Add a test. Or else reword the Design and the skill to
  match the code.
- Source: general
- Disposition notes: Fixed (fix pass 1). A response wraps only when its single non-`[Computed]` member is
  unmatched and a contract class (or a list of one); otherwise it projects directly and an unmatched
  class member is reported itself. Tests: a response that also projects `FeedbackItemId` reports
  `Item` as missing; two unmatched members (`Items` + uncomputed `EmailCopyAvailable`) are both
  reported. Mutation on the product: dropping `[Computed]` from `GetFeedback.Response.Attachments`
  gives one TWA0041 on `Attachments`, not one per `Attachment` member.

### M3 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-attributes/computed-attribute.cs:10
- Description: The `ComputedAttribute` Design does not list child classes or wrapped projection
  classes, but the walk now honors `[Computed]` on both.
- Suggestion: Add both to the attribute's Design and summary.
- Source: general
- Disposition notes: Fixed (fix pass 1). `ComputedAttribute` Design and summary name child classes and
  wrapped projection classes, and say a redacted projected value is `T?`, not `[Computed]`.

## Duplicates / conflicts

- None. M3 is separate from M1's alternative: M3 applies whichever way M1 is decided.
