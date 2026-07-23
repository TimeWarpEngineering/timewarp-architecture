# Disposition — task 113

**Date:** 2026-07-23 (round 1) / 2026-07-24 (round 2)
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Phase 4b effort-1 general review of remaining golden path (113-003/004/005). No bugs. Two suggestions and one nit: CI fail-closed soft-skip and deleted/add-child Version tests fixed; two-party IsConcurrencyToken analyzer/auto-apply deferred as intentional design with Profile+docs as the teaching path (wontfix on this task).

Round 2 (independent post-hoc review, see round-2/independent-review.md): implementation
substance verified empirically, but `dev template-smoke` had never been run and failed on
both matrices — sourceName rewrite of the TypedId EF namespace using (fixed via composed
`<Using>`, task-115 pattern) and web-infrastructure-tests shipping into no-postgres apps
(fixed via slnx `#if (postgres)` + template.json exclude). M2's promised follow-on task was
also missing — filed as task 121. All fixes verified: build 0/0, smoke SUCCEEDED both
matrices, web-infrastructure-tests 5/5.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | Two-party Version contract is deliberate; auto-IsConcurrencyToken would change behavior for all hosts; analyzer is a valid follow-on outside 113 scope. Profile + ADR + how-to document the host half. | orchestrator |

## Escalations

- None
