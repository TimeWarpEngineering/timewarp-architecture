# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch vs master: server contract, handler, `XaiSpeech` / `XaiSpeechToText`, registration, `AgentSurfaceState` Dictate / Close / NewConversation, `AgentAsk.razor` + css, `AskAiJsModule`, `ask-ai.ts`, Jaribu unit runfile, Playwright test, task.md record.

## Summary

The change adds a Dictate action, a microphone in the Ask lead row, browser capture in `ask-ai.ts`, and a server endpoint that forwards base64 audio to xAI `POST {endpoint}/stt` with the server-side key. The key stays on the server, error bodies are never copied into problems, admission reuses the chat relay cap (2 concurrent, so a dictation during a stream is admitted), and CSS uses `--twe-*` tokens with no inline styles. The restatement matches the task (the FluentUI `Mic` icon is what the original Notes specified), so there is no misread. The main defect is the retry path: every transcription failure keeps the clip, so failures that cannot succeed on a resend (no speech, rejected, too long, validation) leave the button resending the same clip with no way to record again short of closing the panel. Recorded gates (`dev build` 0/0, unit runfile 10 passed, filtered Playwright pass, audit exit 0) cover the change.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:64
- Description: `Handle` routes `Error` + stored `VoiceData` to `TranscribeStoredAsync`, and `TranscribeStoredAsync` never clears `VoiceData` on a problem response (lines 976-979). Failures that are a property of the clip, not the network, are therefore retried forever with the same bytes: 422 "The recording had no speech." (`XaiSpeech.NoSpeech`), 400 "The recording could not be transcribed." (`BadAudio`), 413 "The recording is too long." (`TooLong`), and a validator 400 (clip over `MaxDataLength`). In each case the "Try voice input again" button resends the identical clip, gets the identical failure, and spends the key again. The only way to record a new clip is to close the panel or start a new conversation (both call `ClearVoice`). The requirement is "error … with retry"; for these errors retry cannot succeed.
- Suggestion: keep the clip only for failures a resend can fix (transport exception, 5xx/502, 429, 503 after a key is set). For 400 / 413 / 422 call `ClearVoice()` before setting `Error`, so the next press records again. Add a unit or Playwright case (e.g. route the transcription to a 422) proving the next press re-opens the microphone.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/source/features/ask-ai.ts:222
- Description: Recording has no length cap in the browser, while the server rejects `Data` over `TranscribeAgentAudio.MaxDataLength` (6,000,000 base64 chars ≈ 4.5 MB). A long dictation records normally, then fails only after upload with a validator 400 whose detail is whatever the validation problem says (shown verbatim if ≤ 160 chars), and with Issue 1 the retry resends the same oversized clip. The person learns the limit only after losing the dictation.
- Suggestion: stop the recorder at a max duration (or byte count from `dataavailable` with a timeslice) below the server cap, and/or check the base64 length before posting and show `TooLong`'s sentence. Name the shared limit once so client and server agree.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:970
- Description: The elapsed label carries `aria-live="polite"` and is re-rendered every second by `RunVoiceClockAsync`. Screen readers announce "0:01", "0:02", … for the whole recording, which talks over the person dictating and is the usual anti-pattern for live timers. The button's own name change to "Stop recording" plus `aria-pressed="true"` already announce the state.
- Suggestion: drop `aria-live` from the ticking span (or use `aria-live="off"` / `role="timer"`, which is off by default), keeping `role="status"` on the transcribing / error status span.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs:150
- Description: Acceptance says the button stays disabled while a turn streams **or waits for approval**, **except to stop a recording already in progress**, and that Playwright covers those states. The proof covers idle, recording + elapsed, transcribing, network error + retry, denied, and disabled-while-streaming. It does not cover the stop-while-streaming exception (start recording, send, Stop recording still enabled and works), which is the one branch of `VoiceInputDisabled` that is not the default. AwaitingInput is also unproven, though it shares `TurnInProgress` with Streaming.
- Suggestion: add the stop-during-stream case to the existing test (record, send with the delayed completions route, assert the mic is enabled with "Stop recording", click, assert transcript inserted). AwaitingInput can stay covered by `TurnInProgress` if an approval fixture is costly; say so in Results.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.dictate.cs:962
- Description: Close or New conversation during `Transcribing` calls `ReleaseVoiceCaptureAsync`, which clears the phase, but the in-flight request is not cancelled. When it returns, the handler inserts the transcript into whatever composer exists (the new conversation's) or, if the composer is gone, sets `Error` + `InsertFailed`, which the person sees the next time the panel opens. Also, on `InsertFailed` the clip is kept, so retry re-transcribes (spending the key) instead of re-inserting the transcript it already has.
- Suggestion: after the await, drop the result if `VoicePhase` is no longer `Transcribing` (or a generation counter changed); on `InsertFailed`, keep the transcript text rather than the clip, or clear the voice state.
- Status: open
