# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** 882428205 vs 2da1279fb (the framework's scope), plus call sites: `feedback-contract-map-application.cs`, `contract-serialization-defaults.cs`, `computed-attribute.cs`, the 474 analyzer.

## Summary

The change puts `[Operation(..., Group = "feedback")]` on all 13 feedback requests. It adds `IFeedbackItemDetails`
to Submit, with an included validator. It extends TWA0041 in three ways: mirrored wire enums
compared by member name, a walk into child classes, and a walk into a wrapped projection. Each
`[NotAggregate]` gives a reason that names the mutation.

Comparing enums by name matches the runtime. `FeedbackContractMap` maps enums by name, and the
wire uses `JsonStringEnumConverter` with `allowIntegerValues: false`.

What I verified:

- Analyzer tests: 300/300 passed.
- `web-application` builds with 0 warnings and 0 errors.
- `ganda repo audit` reports only the warnings that were already there.
- No feedback Jaribu or Playwright file changed.

Two issues remain:

- One `[Computed]` marks a member that does project from the root, which goes against what the
  attribute's own Design says it is for.
- The wrapper rule is broader than its Design says.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/feedback/feedback-activity-entry-contracts.cs:29
- Description: `[Computed]` on `FeedbackActivityEntry.AuthorPrincipalId` exempts a member that does
  project from the root. `ToEntry` copies `FeedbackActivity.AuthorPrincipalId`, and nulls it on the
  filer's copy. The `ComputedAttribute` Design (`computed-attribute.cs:7-9`) limits the attribute
  to "a value the handler derives ... [that] has no root member". This member has a root member of
  the same name, so the attribute's only effect is to make TWA0041 pass. TWA0041 then stops
  checking it, and a rename or retype of `FeedbackActivity.AuthorPrincipalId` would go unreported.
  The restatement's own acceptance says: "Where the analyzer cannot express a shape the feedback
  root really has, extend the analyzer rather than opt out". A read model that widens `T` to `T?`
  so it can redact is such a shape. The audit silenced is TWA0041 (ContractAgreementAnalyzer
  projection check).
- Suggestion: In the TWA0041 projection walk only, accept a contract `Nullable<T>` (or `T?`
  reference) against a root `T` when `T` is compatible. A projection can always carry a
  non-nullable value in a nullable slot, and the handler may null it. Keep TWA0039 (details,
  write side) strict, so `Guid?` against `Guid` is still drift there. Then:
  - Remove `[Computed]` from `AuthorPrincipalId`.
  - Change `Given_Child_Entry_Member_Disagrees_Flags_TWA0041_On_The_Child` to a real child drift,
    and add a test that the widened form is clean on a projection and still flags on a details
    interface.
  - Update the `contract-agreement-types.cs` Design rule 5 and the `tw-aggregate-pattern` line
    "`Guid?` against `Guid` is still drift" so they scope that rule to the write side.

  If the maintainer would rather keep "nullable widening is drift" everywhere, that is an
  operator decision. Record it as wontfix with the decider. Even then, change the `ComputedAttribute`
  Design so it covers "a root value the handler redacts".
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-agreement-projection.cs:70
- Description: The wrapper rule claims more than the code checks. The Design and the skill say
  that on a non-list response, "a member" with no root member of its name, whose type is a contract
  class, is the wrapped projection. They also say "this is not a name mapping: a member that drifted
  still fails". In the code, every unmatched contract-class member of every non-list response is
  treated as a wrapper, not one member per response. Two effects follow:
  - (a) Misleading messages. Drop `[Computed]` from the top-level `GetFeedback.Response.Attachments`
    (`get-feedback-contracts.cs:52`). Nothing reports the actual problem, "FeedbackItem has no
    member Attachments". Instead, `GetFeedback.Attachment`'s own members (`AttachmentId`,
    `FileName`, …) are each reported as missing on `FeedbackItem`.
  - (b) A drifted or new class-typed member can pass silently when that class's members happen to
    be a subset of the root's members. For example, a `Summary Header` holding `Title` and `Status`
    passes even though `FeedbackItem` has no `Header`.

  No test covers a top-level unmatched class member on a response that already has a different
  wrapper, or on one that is not a wrapper response at all.
- Suggestion: Limit the wrapper to one member per response. It should be the only unmatched,
  non-computed contract-class member. If there are two or more candidates, report each one as a
  missing root member. Add a test for the `GetFeedback.Response.Attachments` case. Or else word
  the Design and the skill to say what the code does, and accept the weaker guarantee.
- Status: open

### Issue 3 — Severity: nit
- File: source/analyzers/timewarp-architecture-attributes/computed-attribute.cs:10
- Description: The Design lists where `[Computed]` applies as "a property, the response class, or
  a list response's item class". This change makes `[Computed]` also skip a child class
  (`TryGetChildren`) and a wrapped class (`TryGetWrapped`), and the projection Design says so
  (`contract-agreement-projection.cs:20`). The two Designs now disagree.
- Suggestion: Add "a child class or a wrapped projection class" to the `ComputedAttribute` Design
  and summary.
- Status: open
