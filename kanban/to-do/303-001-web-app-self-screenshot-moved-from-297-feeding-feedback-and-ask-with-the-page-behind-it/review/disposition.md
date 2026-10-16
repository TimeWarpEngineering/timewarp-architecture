# Disposition — task 303-001

**Date:** 2026-10-17
**Outcome:** accepted-exceptions
**Rounds:** 3 (round 1: full review; round 2: re-review of fixes; round 3: verification of the round-2 fixes)
**Final open count:** 0

## Summary

The review found 4 bugs: a null image list or null turn crashing the relay with a 500, the client building a request over the server's image cap, and a new BL0016 pragma in a test. All 4 are fixed and covered by tests. Ask and Feedback each capture with one action of their own, so an agent capture taken at the same moment can never be attached in place of the person's. Refused captures now tell the agent why. Images are checked against PNG or JPEG magic bytes. Two items are accepted as exceptions.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M7 (budget part) | suggestion | Each request is bounded: authenticated, rate-limited per principal, at most 2 images of at most 7,000,000 base64 characters each. A smaller image budget is a cost decision for Steve. | orchestrator; the round-2 reviewer agreed |
| M9 | nit | The silent crop at 4096 px is one of the limits the capture spike accepted, and passing a selector captures part of a long page. A "cropped" indicator can be a follow-up if wanted. | orchestrator; the round-2 reviewer agreed |

## Escalations

- None that block. One item for Steve to confirm: Feedback is a route, not a dialog, so "the page behind" means the latest capture taken on another page. See the Learnings section in task.md.
