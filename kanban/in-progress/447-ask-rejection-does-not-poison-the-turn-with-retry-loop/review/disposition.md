# Disposition — task 447

**Date:** 2026-10-30
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (effort 2, general + orchestrator) found no bugs, one suggestion (M1: hand-written
`ReachesModel` duplicated the mapping and the validator's empty-turn rule) and two nits (M2: Unit test
borrows the Integration class's capture helpers; M3: post-filter `lastUser` meaning undocumented and
unpinned). The fix pass (cadb4b0df) resolved M1 and M3: `CompleteAgentChat.IsEmpty` is the single public
rule shared by the validator and `RelayChatClient.ToTurns`, and two relay cases pin the edge content
kinds and the post-filter `lastUser` order. Round 2 (effort 1, general + orchestrator) verified both and
raised nothing new. Gates after the fix pass: build 0/0, audit exit 0, Unit 430/430, AskRejection 4/4.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | nit | `CaptureSpa` and `CaptureJsRuntime` are the catalog screenshot fixture and belong with the proof that owns them; the Unit run passes without the Integration `SetupOnce`; Design notes on both files record the sharing. Relocate to `infrastructure/` when a third consumer appears. | review oracle (round 1) |

## Escalations

- None.
