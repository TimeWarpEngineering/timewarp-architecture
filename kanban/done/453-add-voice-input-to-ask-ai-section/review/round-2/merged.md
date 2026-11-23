# Round 2 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

Final statuses across both rounds (M1–M5 carried from round 1, M6 new).

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 1 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs
- Disposition notes: 9249b9240. Clip kept only for transport, 429, 5xx; 400/413/422 clear it. Playwright 422 path proves the next press records without a resend.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts
- Disposition notes: 9249b9240. Browser limit is `TranscribeAgentAudio.MaxDataLength` passed at start; auto-stop plus stop-time and handler length checks; Playwright proves a too-long clip is not posted.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor
- Disposition notes: 9249b9240. Elapsed span is `role="timer"`; status span keeps `role="status"`.

### M4 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs
- Disposition notes: 9249b9240. Stop during streaming covered; AwaitingInput documented as the shared `TurnInProgress` check.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs
- Disposition notes: 9249b9240. Insert epoch drops a late transcript; insert failure keeps the transcript and retry does not re-transcribe. Playwright covers New conversation during Transcribing.

## Issues

### M6 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:40
- Description: After Close or New conversation during Transcribing, `VoiceBusy` stays set until the stale speech request returns, so a press in that window is silently ignored.
- Suggestion: cancel the request on epoch change, or release `VoiceBusy` when the result is dropped.
- Source: general
- Disposition notes: wontfix, decided by review oracle round 2. Pre-existing (round 1 already held `VoiceBusy` across the request); the delta does not make it worse; the cost is one ignored press during an HTTP round trip, and the next press works. Not worth a third round on this task.

## Duplicates / conflicts

- None. M6 is adjacent to M5 (same Close-during-Transcribing path) but is the busy flag, not the late insert M5 fixed.
