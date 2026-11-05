# Bring FeedbackItem contracts under the contract-agreement analyzer

## Description

Second shape for the analyzer from task 474. `FeedbackItem` (`features/feedback/feedback-item-domain.cs`)
is an existing aggregate with entity children (activity entries with sequence rules), a status
transition table in a contracts file, and attachments. Its endpoints predate the `[Operation]`
convention: none carry an operation name or a shared details interface, so the analyzer cannot
link them today.

## Requirements

- Every feedback endpoint contract gets `[Operation("feedback.<verb>", Group = "feedback")]` per
  `tw-web-api-contracts`, and the create/update pair gets a shared details interface where the
  members are the same.
- The root is linked to the group the way 474 defines, and the analyzer passes with no
  `[NotAggregate]` opt-outs that are not justified in a comment.
- Where the analyzer finds a real mismatch (a response member with no root member), fix the side
  that is wrong and record which in the task; do not add opt-outs to get green.
- Gates: `dev build` 0 warnings, feedback Jaribu and Playwright tests unchanged, `dev template-smoke`,
  `ganda repo audit`.

## Goal (implementer restatement)

Make the 474 contract-agreement analyzer prove that `FeedbackItem` agrees with the feedback
contracts. Linking is already by convention: the root's slice namespace is
`Features.Feedback`, where the feedback contracts live. What is missing is `[Operation]` on the
contracts, so nothing links today.

Acceptance:

- Every hosted feedback request (the 13 `Command`/`Query` classes) carries
  `[Operation("feedback.<verb>", Description = …, Group = "feedback")]`.
- Submit (the only writer of the filed fields, since nothing edits a filing) implements a shared
  `IFeedbackItemDetails` (Kind, Title, Body) with its validator. There is no update command, so the
  "pair" is Submit plus the read models that show the same members.
- web-application builds clean with the analyzer linked to `FeedbackItem`. Each `[NotAggregate]` or
  `[Computed]` says why, either in a reason or in an XML doc.
- Where the analyzer cannot express a shape the feedback root really has, extend the analyzer
  rather than opt out. That covers wire enums mirrored from domain enums, child-entity
  collections, and a response that wraps the projection in a member. Record each extension, add
  analyzer tests, and update AnalyzerReleases and the skills.
- Real drift found is fixed on the wrong side and recorded.
- Gates: `dev build` 0 warnings, the analyzer tests, feedback Jaribu tests unchanged, and
  `dev template-smoke`. `ganda repo audit` is clean.

## Checklist

- [x] Feedback contracts carry [Operation] and a details interface
- [x] FeedbackItem passes the analyzer with justified opt-outs only
- [x] Mismatches fixed on the correct side and recorded (none were real drift; see Results)
- [x] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- Implemented: ganda task work implement oracle (Claude Opus 5.5), 2026-11-05.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 3aa86ac9-d70e-4fbd-b279-cae8c52cb00e, max-turns 500, Done — 2026-11-05T04:56:59Z
- Review oracle, round 1: general reviewer (Claude Opus 5.5, model claude-opus-5-5), effort 3, turn
  cap 200, under ganda task work, 2026-11-05. The result is Blocked: a fix pass is needed for open
  findings M1, M2, and M3.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session d360989f-d0af-4deb-9d1b-a58e768a274e, max-turns 200, artifacts general.md, merged.md — 2026-11-05T05:01:45Z
- Fix pass 1 (implement-fix-2 oracle, Claude Opus 5.5), 2026-11-05: M1–M3 fixed.
- Review oracle, round 2: general reviewer (Claude Opus 5.5, model claude-opus-5-5), effort 2, turn
  cap 120, under ganda task work, 2026-11-05. It reviewed the fix delta 0b8af0e1e..76f95a527 and
  found no new issues. Disposition: clean.

## Results

- **Linked:** all 13 feedback requests now carry
  `[Operation("feedback.<verb>", Description = …, Group = "feedback")]`: submit, get, list-mine,
  reply-as-filer, upload-attachment, remove-attachment, download-attachment, list, get-for-admin,
  change-status, set-severity, reply, and add-note. The contracts are in `Features.Feedback`, the
  slice namespace of `FeedbackItem` (`Features.Feedback.Domain`), so the 474 convention links the
  group with nothing declared on the entity.
- **First run, before any fix:** 23 diagnostics. None was real drift. Three were analyzer gaps,
  because the feedback root has shapes the Roles and Profile shapes do not. The rest were
  handler-derived values and commands that no check covers.
- **Analyzer extensions** (`contract-agreement-types.cs`, new `contract-agreement-projection.cs`,
  with Design regions):
  1. *Mirrored enums:* two enums agree when their member names are the same set. Values are not
     compared. Contracts must not reference domain, so a wire `FeedbackStatus` mirrors the domain's
     enum, and the names are what travel. `T?` against `T?` compares `T`. On the write side
     (TWA0039, TWA0040), `Guid?` against `Guid` is still drift. On the read side (TWA0041), a
     response may widen a root `T` to `T?` (fix pass 1, M1). When the names differ, the message lists which side has which names, and it uses
     full type names because the short names print alike.
  2. *Child projection:* a response member and its root member that are both classes, or both
     collections of classes, are walked member by member. `FeedbackActivityEntry` projects from the
     owned `FeedbackActivity`, and the message names the child.
  3. *Wrapped projection:* a non-list response whose only non-`[Computed]` member has no root
     member of its name and is a class in the contracts assembly wraps the projection. A response
     that projects any other member has no wrapper (fix pass 1, M2). It is checked against the root one
     level down: `GetFeedbackForAdmin.Response.Item` (`FeedbackAdminView`) and
     `ListMyFeedback.Response.Items`. This is not a name mapping. A drifted member still fails,
     because the wrapped class's members are then checked against the root.
- **Details interface:** `IFeedbackItemDetails` (Kind, Title, Body) and
  `FeedbackItemDetailsValidator` are in `feedback-item-details-contracts.cs`. `SubmitFeedback.Command`
  implements the interface, and its validator `Include`s the details validator, so property names
  are unchanged for the Blazilla form. `SubmitFeedback.MaxTitleLength` and `MaxBodyLength` forward
  to the interface constants. A filing is never edited, so there is no update command. The
  get-only read models do not implement the `{ get; set; }` interface; TWA0041 checks them instead.
- **`[Computed]`, each with an XML doc:**
  - `Permalink` on GetFeedback and the ListMyFeedback item: built from the id.
  - `Attachments` on GetFeedback and FeedbackAdminView: the attachment store.
  - `FilerHasEmail`, `OwnerDisplayName` on the admin view and the list item, and
    `EmailCopyAvailable`: read from the profile slice.
  - `FeedbackActivityEntry.AuthorPrincipalId` is no longer `[Computed]` (fix pass 1, M1). It is
    `Guid?` against the domain's `Guid` because the handler redacts it to null on the filer's copy.
    That is read widening, which TWA0041 now accepts, so the member is checked.
- **`[NotAggregate]`, each reason in the attribute:**
  - Upload, remove, and download attachment: `FeedbackAttachment` is its own entity, and the
    attachment store is not `FeedbackItem` state.
  - Change status and set severity: checked transitions through `ChangeStatus` and `SetSeverity`.
  - Reply, reply as filer, and add note: thread appends through `AddAdminReply`, `AddFilerReply`,
    and `AddInternalNote`, whose `Body` is the entry's body, not `FeedbackItem.Body`. A naive
    member-name check would match these wrongly, which is why none was added.
  - The `NotAggregateAttribute` Design and `tw-aggregate-pattern` now name this third case: a
    named mutation that no check covers. A real check for named mutations belongs with task 476.
- **Mismatches fixed:** none needed. The wire enums matched the domain by name, the reply bodies are
  entry bodies, and the nullable author is deliberate redaction (read widening).
- **Fix pass 1 (review round 1, M1–M3, all fixed):**
  - M1: `ContractAgreementTypes.ReadCompatible` accepts a response `T?` (Nullable<T> or a nullable
    reference) against a root `T`. Only TWA0041 uses it; details and Set stay strict. One 474 test
    changed: the Profile `Email` nullability drift now expects TWA0039 only, since
    `GetProfile.Response.Email` (`string?` against `string`) is widening.
  - M2: one wrapper per response, and only when it is the response's single non-`[Computed]`
    member. Mutation on the product: dropping `[Computed]` from `GetFeedback.Response.Attachments`
    gives `TWA0041 … GetFeedback.Response.Attachments … 'FeedbackItem' has no public member
    'Attachments'`, one error, not one per `Attachment` member. Restored.
  - M3: the `ComputedAttribute` Design and summary list child and wrapped classes, and say a
    redacted value is `T?`, not `[Computed]`.
  - New analyzer tests (5): narrowing still drifts, reference widening is clean, details widening
    flags TWA0039, a directly projecting response reports `Item` itself, and two unmatched members
    are both reported. The child-entry disagreement test is retargeted to `int` against `long`.
  - Gates re-run after fix pass 1: `dev build` 0 warnings, 0 errors; `dev test --group fast` 14/14
    projects (analyzers 305/305); web-jaribu-tests 464/464; `dev template-smoke` passed;
    `dev lint-skills` passed; `ganda repo audit` exit 0 (only the warnings that were already
    there: kanban-landed claims and the line cap on migration Designer files).
- **Mutation check:** I dropped `[Computed]` from `AuthorPrincipalId` and from `FilerHasEmail`,
  and added an `Extra` member to the wire `FeedbackSeverity`. web-application then failed with 6
  TWA0041 errors: the child entry's author, both nullable severities on the child entry, the
  wrapped view's `FilerHasEmail`, and `Severity` on the admin view and the list item. I restored
  all three files.
- **Docs:** the TWA0041 row in `AnalyzerReleases.Unshipped.md`, and the `tw-aggregate-pattern`
  "Contract agreement" section (type rules, the walk past the top level, the TWA0042 cases).
  `tw-web-api-contracts` is at 498 of its 500 lines, and its paragraph is still accurate, so it is
  unchanged.
- **Side effect:** endpoints with no XML summary on the request now get their OpenAPI summary from
  the `[Operation]` description (task 472 order). ListFeedback keeps its XML summary.
- **Version:** `dev check-version` shows 2.0.0-beta.32 in source, still unreleased (NuGet has
  beta.31), so the analyzer change ships in beta.32 with 474. No bump.
- **Gates:**
  - `dev build`: 0 warnings, 0 errors.
  - `dev test --group fast`: 14/14 projects. The analyzer tests are 305 with 0 failed (after fix
    pass 1); 16 are in `contract-agreement-feedback-analyzer-tests.cs`, with the shape in
    `contract-agreement-feedback-test-fixture.cs`.
  - web-jaribu-tests (co-located feedback suites): 464/464.
  - web-server-integration-tests: 436 passed, 1 skipped (the manual `RunForever`).
  - `dev template-smoke`: passed.
  - `dev lint-skills`: passed.
  - `ganda repo audit`: exit 0. Two warning-severity checks were there before this task and are
    not part of it: kanban-landed claims held by other tasks, and the line cap on generated
    migration Designer files plus one done-task research file.
- **Not run locally:** Playwright and the Aspire AppHost suites, because worktree AppHosts share
  user secrets. They are left to CI. No feedback test file changed.

### How to validate

- **Smoke:** `./bin/dev build`, then
  `(cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet run)`. To see the rules
  fire, delete `[Computed]` above `Attachments` in
  `source/container-apps/web/features/feedback/get-feedback/get-feedback-contracts.cs` and run
  `(cd source/container-apps/web/projects/web-application && dotnet build)`.
- **Expect:**
  - The build reports 0 warnings and 0 errors.
  - The analyzer tests report `failed: 0`, and the `Should_Check_Contract_Agreement_For_Feedback_Shape`
    cases are included.
  - With `[Computed]` removed, web-application fails with exactly one error:
    `TWA0041: 'TimeWarp.Architecture.Features.Feedback.GetFeedback.Response.Attachments' does not
    project from 'FeedbackItem' (operation group 'feedback'): 'FeedbackItem' has no public member
    'Attachments'`. Restore the attribute afterwards.
  - `FeedbackActivityEntry.AuthorPrincipalId` (`Guid?` against the root's `Guid`) has no
    `[Computed]` and builds clean.

### Review

- **Round 1** (general, effort 3), with `review/round-1/merged.md` as the live ledger:
  - Counts: 1 bug, 1 suggestion, and 1 nit, all open. Nothing is fixed or wontfix yet.
  - M1 (bug): `[Computed]` on `FeedbackActivityEntry.AuthorPrincipalId` exempts a member that
    does project from the root, which only silences TWA0041.
  - M2 (suggestion): the wrapper rule accepts every unmatched contract-class member, but the
    Design says one.
  - M3 (nit): the `ComputedAttribute` Design does not list child and wrapped classes.
- **Reviewer re-ran:**
  - Analyzer tests: 300/300.
  - `web-application` build: 0 warnings, 0 errors.
  - `ganda repo audit`: only the warnings that were already there.
  - Enum-by-name agreement against `FeedbackContractMap` and the string enum converter: confirmed.
  - No feedback Jaribu or Playwright file changed.
- **Fix pass 1:** M1, M2, and M3 fixed (commit 76f95a527).
- **Round 2** (general, effort 2, turn cap 120), with the delta 0b8af0e1e..76f95a527 as the scope:
  - It re-verified M1–M3 as fixed and found no new issues.
  - The reviewer re-ran the analyzer tests (305/305), the `web-application` build (0 warnings,
    0 errors), and `dev lint-skills` (passed).
  - Not a finding: read widening does not reach collection elements, so `List<string?>` against
    `List<string>` would still be drift. The product has no such shape.
- **Final counts:** bug 0 open / 1 fixed / 0 wontfix, suggestion 0 / 1 / 0, nit 0 / 1 / 0.
- **Disposition:** clean. All three raised findings are fixed, and there are no wontfix items or
  escalations.
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`, `review/round-1/merged.md`
  - `review/round-2/general.md`, `review/round-2/merged.md` (last ledger)
  - `review/disposition.md`
  - `review/round-1/merged.md`

## Learnings

- Misread (minor): the task's gate says "feedback Jaribu **and Playwright** tests unchanged". The
  Goal restatement says only "feedback Jaribu tests unchanged". It had no effect, because no
  Playwright file changed. A restatement should keep every gate the task names.

## Review

- Round 1: Blocked — open M1, M2, M3
- Fix pass 1: M1, M2, M3 fixed; awaiting round 2
- Round 2: Done
## Notes

Follows 474. The third shape, Order, belongs to backlog task 118 (real-domain showcase).
