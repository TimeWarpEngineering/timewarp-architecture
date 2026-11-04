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

## Checklist

- [ ] Feedback contracts carry [Operation] and a details interface
- [ ] FeedbackItem passes the analyzer with justified opt-outs only
- [ ] Mismatches fixed on the correct side and recorded
- [ ] Gates green

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)

## Notes

Follows 474. The third shape, Order, belongs to backlog task 118 (real-domain showcase).
