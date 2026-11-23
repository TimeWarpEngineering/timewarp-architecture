# Round 1 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:64
- Description: Every transcription problem keeps the stored clip, and Error + clip always resends it. For clip-bound failures (422 no speech, 400 rejected, 413 too long, validator 400 over `MaxDataLength`) the retry resends the same bytes, fails the same way, spends the key again, and the person cannot record a new clip without closing the panel or starting a new conversation.
- Suggestion: keep the clip only for resend-fixable failures (transport, 5xx, 429, 503); `ClearVoice()` before `Error` for 400/413/422 so the next press records. Add a test that routes the transcription to 422 and proves the next press re-opens the microphone.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts:222
- Description: No browser-side recording cap; the server rejects base64 over 6,000,000 chars only after upload, so a long dictation is lost (and, with M1, retried forever).
- Suggestion: cap duration/bytes below the server limit or pre-check length and show the too-long sentence; name the limit once.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:970
- Description: The elapsed span is `aria-live="polite"` and re-renders every second, so screen readers announce each tick over the dictation.
- Suggestion: remove `aria-live` from the ticking span (or `role="timer"`); keep `role="status"` on the transcribing/error span.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs:150
- Description: Playwright does not cover the acceptance exception "except to stop a recording already in progress" (stop enabled and working while a turn streams); AwaitingInput is unproven too.
- Suggestion: add the record → send → stop-during-stream case; note in Results if AwaitingInput relies on the shared `TurnInProgress` path.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:962
- Description: Close / New conversation during Transcribing does not drop the in-flight result; it is inserted into the new composer or leaves a stale `InsertFailed` error. `InsertFailed` keeps the clip, so retry re-transcribes instead of re-inserting.
- Suggestion: ignore the result when the phase is no longer Transcribing; on InsertFailed keep the text or clear voice state.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- M2 and M1 interact (an oversized clip hits the M1 retry loop) but are separate fixes: M1 is the retry policy, M2 is the client-side limit.
