# Disposition — task 105

**Date:** 2026-10-04
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general review round (effort 3, by-diff budget) found no bugs and no suggestions. Two nits on
edge semantics were accepted as wontfix with rationale. The gates were re-verified in this worktree:
`dev build` 0/0; foundation-domain-tests 62/62, foundation-contracts-tests 25/25, and
analyzers 192/192 pass; `ganda repo audit` passes.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | A permanently-null member field is not a legitimate pattern. It costs performance only, never correctness. Not caching a partial snapshot is the deliberate fail-safe. | orchestrator (review oracle) |
| M2 | nit | Value-only comparison is documented in the Design region and predates this change. No mixed-subclass sorting exists. Throwing would be an unrequested breaking change. | orchestrator (review oracle) |

## Escalations

- None. The extract-vs-keep decision remains Steve's open question on task.md. It is not a review finding.
