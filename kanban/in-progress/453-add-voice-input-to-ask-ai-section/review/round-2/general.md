# Round 2 — general
**Date:** 2026-11-24
**Scope reviewed:** fix delta 9249b9240 (`fix: drop a refused voice clip and stop a long dictation`) against round-1 M1–M5, plus the updated `## Goal`, `## Results`, and `### How to validate` in `task.md`.

## Summary

The fix pass addresses all five round-1 findings. The Dictate handler now keeps the clip only for a transport failure, 429, or 5xx and drops it for 400 / 413 / 422 (M1). The browser takes `TranscribeAgentAudio.MaxDataLength` as its one limit, records in 1 s slices, stops itself when the next chunk would pass that limit, and reports `too-long` instead of uploading; the handler re-checks the length before posting (M2). The elapsed span is `role="timer"` (implicit `aria-live="off"`); the status span keeps `role="status"` (M3). Playwright adds `Refused_Clip_Records_Again_And_Stop_Works_While_Streaming`: a 422 records again with no resend, stop works while AskStop is visible, a too-long clip is not posted, and New conversation during Transcribing drops the late transcript. AwaitingInput is documented as the same `TurnInProgress` check (M4, as the round-1 suggestion allowed). An epoch bumped by `ClearVoice` and published to `ask-ai.ts` makes a late result a no-op, and an insert failure keeps the transcript, not the clip, so retry inserts without calling speech-to-text (M5).

Recorded gates cover the delta: `./bin/dev build` 0/0, the Playwright class filtered run 2 passed, `ganda repo audit` exit 0 with the pre-existing `vscode-window-icon` advisory. The transcription runfile was not edited and was not re-run, which is correct for this delta. Results states the one residual window honestly (an insert call already running when Close publishes the epoch can still land); on single-threaded WASM interop that window is an already-dispatched synchronous JS call and is acceptable.

The goal restatement and the fix-pass acceptance match the task and round-1 findings. No misread.

## Resolved prior

| ID | Round-1 severity | Status | Evidence |
|----|------------------|--------|----------|
| M1 | bug | fixed | `agent-surface-state.dictate.cs` `KeepsClip` (429, 5xx) and `ClearVoice` otherwise; Playwright 422 path asserts `hits == 1` after the next press opens Stop |
| M2 | suggestion | fixed | `ask-ai.ts` `rememberVoiceChunk` / `VoiceCaptureExceeded`, `StopVoiceCapture` length check, handler `data.Length > MaxDataLength`; AgentAsk clock dispatches Dictate when exceeded; Playwright too-long path asserts `hits == 0` |
| M3 | suggestion | fixed | `AgentAsk.razor` elapsed span `role="timer"`, no `aria-live` |
| M4 | suggestion | fixed | Playwright record → send → stop while AskStop visible; AwaitingInput noted in Results and test Design region |
| M5 | nit | fixed | `VoiceEpoch` + `SetVoiceInsertEpoch` / `InsertComposerText(text, epoch)`; `VoiceTranscript` retry path; Playwright New conversation during Transcribing |

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:40
- Description: Close or New conversation during Transcribing resets the phase to Idle, but the handler that owns the in-flight speech request still holds `VoiceBusy` until that request returns. A microphone press in that window shows "Dictate a prompt" and is silently ignored. The next press after the stale request finishes works. This behavior predates the fix pass (round 1 already had `VoiceBusy` around the request); the delta does not make it worse.
- Suggestion: cancel the speech request when the epoch changes, or release `VoiceBusy` in `ReleaseVoiceCaptureAsync` when it drops a transcribing result.
- Status: open

## Not raised

- The browser auto-stop path (clock tick → `VoiceCaptureExceeded` → Dictate) is not driven by Playwright; the fake recorder in `TooLongScript` emits its oversized chunk on stop, so the test proves the stop-time check. The acceptance for this pass ("not posted, too-long sentence, next press records") is proven. Not a finding.
- `StopAndTranscribeAsync` checks `StillTranscribing(AgentSurfaceState.VoiceEpoch)` with the epoch read after the await rather than captured before it. The phase check still catches Close (phase becomes Idle), so there is no behavior difference.
