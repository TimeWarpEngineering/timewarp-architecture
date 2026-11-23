# Add voice input to Ask AI section

## Description

Add voice input to the Ask AI panel composer (`AgentAsk.razor` / `MessageInput`) so a person can dictate a prompt instead of typing it. Transcription will be powered by the Grok Voice API (xAI). The button uses the same microphone icon Grok uses for voice input.

Scope is Ask AI only (`source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/`): a mic button in the composer chrome, browser mic capture, Grok Voice transcription into the composer text, and clear recording / transcribing / error states.

## Requirements

- Mic button in the Ask composer (lead slot alongside Tag / Camera, or trail area — match existing composer grid). Icon matches Grok's voice-input mic icon.
- Uses Grok Voice API for speech-to-text. No other STT provider.
- Click to start/stop recording; transcribe and insert text at the composer caret (do not auto-send).
- States: idle, recording (with stop affordance + elapsed indicator), transcribing, error (mic denied, network/API failure) with retry.
- Respects existing composer rules: disabled while a turn is streaming / awaiting approval where appropriate, focus returns to composer after insert (follows task 381 focus pattern via `ask-ai.ts`).
- Accessible: `aria-label`, `aria-pressed` while recording, keyboard operable, `data-qa="AskVoiceInput"`.
- Follows template conventions: no inline styles, `--twe-*` tokens, `ask-ai.ts` + `AskAiJsModule` for DOM/media work, `[DirectComponentSideEffect]` on JS-interop.
- Playwright coverage for the button presence / states (see existing `ask-*-playwright-tests.cs`).

## Goal (implementer restatement)

A person with the Ask composer open can click a microphone, speak a prompt, and get that speech inserted as text at the caret. The message is not sent. Speech-to-text is xAI's speech-to-text API and nothing else. The same button shows recording (with how long it has been running and a stop control), transcribing, and a failure (microphone refused, or the request failed) with a way to try again. It does not start while Ask is streaming an answer or waiting for approval. After the text lands, the composer has focus again. The control is keyboard usable, named for assistive tech, and covered by the Ask Playwright suite.

Acceptance:

- The composer lead row has a mic button using FluentUI's microphone icon, `data-qa="AskVoiceInput"`, an `aria-label`, and `aria-pressed` while recording.
- The browser records through `ask-ai.ts` and `AskAiJsModule`. The API key stays on the server.
- The server posts the audio to xAI `POST /v1/stt` with the existing `XAI:ApiKey` and `XAI:Endpoint`.
- The transcript is inserted at the caret and is not sent. Focus returns to the composer.
- Idle, recording plus elapsed time, transcribing, and error with retry are visible.
- The button stays disabled while a turn is streaming or waiting for approval, except to stop a recording already in progress.
- Playwright covers the button and those states.

Fix pass for review round 1. A clip the server will refuse for the audio itself is dropped, and the next press records a new one. A failure that can succeed on another try keeps the clip and the retry sends those same bytes. The browser stops before the clip's base64 can pass the one server limit, and shows the too-long sentence instead of uploading. The elapsed time does not talk over the dictation. Closing the panel or starting a new conversation while speech-to-text is in flight drops that result. When the transcript comes back and the composer cannot take it, retry inserts that text and does not spend the key again.

Acceptance for this pass:

- A 422 leaves no stored clip. The next press shows Stop with `aria-pressed` true and does not send the old bytes.
- A transport failure, 429, or 5xx (including 503) still retries the same clip.
- A recording whose base64 would pass `TranscribeAgentAudio.MaxDataLength` is not posted. The status is "The recording is too long." The next press records.
- The elapsed span is not an `aria-live` region. The transcribing and error span keeps `role="status"`.
- Start a recording, send, and stop that recording while the turn's Stop control is visible. The microphone stays enabled until that stop.
- Waiting for approval uses the same turn-in-progress check as streaming.
- Close or New conversation during transcribing does not insert the late transcript and does not leave the insert-failed sentence. An insert failure keeps the transcript for the retry.

## Checklist

- [x] Add voice-input button to Ask composer matching Grok's mic icon
- [x] Wire browser mic capture via `ask-ai.ts` / `AskAiJsModule`
- [x] Integrate Grok Voice API transcription into composer text
- [x] Handle denied mic, API error, and transcribing states
- [x] Add/Update Playwright tests
- [x] Verify functionality
- [x] Drop a clip-bound transcription failure so the next press records
- [x] Cap a recording at the one server size limit and show the too-long sentence
- [x] Keep the elapsed time from being announced every second
- [x] Prove stop still works while a turn is streaming
- [x] Ignore a late transcript after close, and retry an insert without re-transcribing

## Session

- Created: ses_e7b3cf52affeeONsuSseW9xQE5 (2026-10-28)
- Implementation: voice input on this branch (2026-11-24). The transcription runfile does not copy the neighbour `NoWarn` list. CA1707, CA1849, and CA2000 are fixed in that file. `vscode-window-icon` stays the pre-existing advisory: `.vscode/settings.json` is gitignored local state.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20a5b-2025-7222-b734-35baea4994c2, max-turns 500, Done — 2026-11-23T22:37:16Z
- Review oracle round 1: claude (claude-opus-5-5), effort 3, roster general, 2026-11-24. 5 open (1 bug, 3 suggestion, 1 nit). Fix pass requested.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b7af8303-96a8-46f0-bf64-9707456fb5ae, max-turns 200, artifacts general.md, merged.md — 2026-11-23T22:39:31Z
- Fix pass for review round 1 (2026-11-24): a clip the server refuses for the audio itself is dropped, the browser stops at `TranscribeAgentAudio.MaxDataLength`, the elapsed span is `role="timer"`, stop stays usable while an answer streams, and New conversation during transcribing drops the late transcript. An insert failure keeps the transcript. `vscode-window-icon` stays the same pre-existing advisory.
- Review oracle round 2: claude (claude-opus-5-5), effort 2, roster general, turn cap 120, 2026-11-24. Reviewed fix delta 9249b9240. M1–M5 fixed; one new pre-existing nit M6 accepted as wontfix. Disposition accepted-exceptions, 0 open.

## Results

The Ask composer lead row has a microphone. A click records in the browser, the server posts the clip to xAI speech-to-text, and the transcript is inserted at the caret. The message is not sent. The API key stays on the server. Recording shows a stop icon and elapsed time. A refused microphone, an empty clip, and a failed request each show a sentence and a retry. Retry resends the same clip when the failure can succeed on another try (transport, 429, or 5xx, including 503). A 400, 413, or 422 drops the clip, so the next press records. The browser and the dictate handler both stop before the clip's base64 passes `TranscribeAgentAudio.MaxDataLength` (6,000,000), the only copy of that limit, and show "The recording is too long." The elapsed span is `role="timer"` and is not an `aria-live` region. The transcribing and error span keeps `role="status"`. The button disables while a turn streams or an approval is waiting, and stays usable to stop a recording already going. `aria-disabled` is set because Fluent's `disabled` attribute is not what assistive tech or Playwright reads on `fluent-button`. Close or New conversation during transcribing clears the voice phase and publishes a new insert epoch, so a result that arrives afterward is ignored. When the composer cannot take a transcript, the text is kept and the clip is not, so retry inserts that text and does not call speech-to-text again.

A transcript whose insert call is already running when Close publishes the new epoch can still land. The proven path is the speech request still in flight.

### How to validate

**Smoke** — open Ask, click the microphone (`data-qa=AskVoiceInput`), allow it, speak, and click again to stop. For the refusal path, stop a clip the server answers with 422, then click the microphone again. For the stream path, start a recording, send the composer, and click the microphone while Stop is visible.

**Expect** — the composer gains the transcript and does not send. The name cycles "Dictate a prompt", then "Stop recording" with `aria-pressed` true and an `m:ss` elapsed label, then "Transcribing" while the button is disabled. A denied microphone shows "Microphone access was denied." and "Try voice input again." A dropped request shows "The recording could not be transcribed." and the same retry name. Retry inserts the transcript at the caret (a typed "Please" becomes "Please Voice note." under the fake upstream) and focuses the composer. A 422 shows "The recording had no speech." The next press shows "Stop recording" with `aria-pressed` true and does not send the old bytes. A clip past the size limit shows "The recording is too long." and is not posted; the next press records. While an answer streams, Stop is visible. A recording already going stays enabled until that stop; after it, the microphone is disabled until the turn ends. New conversation while the label is "Transcribing" returns the name to "Dictate a prompt" and does not insert the late transcript. Waiting for approval uses the same `TurnInProgress` check as streaming (`Streaming` or `AwaitingInput` in `AgentAsk.razor`). The Playwright file does not stage an approval card.

**Automated gate**

First implementation (not re-run on this pass; the transcription runfile was not edited):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors, 00:00:21.54.
- `dotnet run --file source/container-apps/web/features/agent-chat/transcribe-agent-audio/transcribe-agent-audio-tests.cs` — exit 0, 10 passed (handler 5, speech request and mapping 3, validator 2).
- `tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs` is not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs it with the Playwright project. Filtered proof from that project directory: `dotnet test -c Release --no-build --no-restore -- --filter-class AskVoiceInput_Given_Wasm` — passed, 1 test.
- `ganda repo audit` — exit 0 after the transcription runfile was staged executable (`runfile-executable` had failed on the first commit because the shebang was mode 100644). 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing).

Fix pass for review round 1:

- `./bin/dev build` — exit 0, 0 warnings, 0 errors, 00:00:21.51. An earlier build in this pass failed CA1508 on a direct phase comparison after the transcribing assignment. The check calls `StillTranscribing`.
- `tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs` is not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs it with the Playwright suite. From that project directory: `dotnet test -c Release --no-build --no-restore -- --filter-class AskVoiceInput_Given_Wasm` — exit 0, 2 passed, 24s 347ms. The class covers the original retry and `Refused_Clip_Records_Again_And_Stop_Works_While_Streaming` (422 records again, a too-long clip is not posted, stop works while AskStop is visible, New conversation drops the late transcript).
- `transcribe-agent-audio-tests.cs` was not edited and was not re-run.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). Not changed.

### Review

- Round 1 (2026-11-24): effort 3, reviewer roster general. M1–M5 raised (1 bug, 3 suggestion, 1 nit).
- Round 2 (2026-11-24): effort 2, reviewer roster general, scope fix delta 9249b9240. M1–M5 verified fixed. M6 (nit, new, pre-existing): `VoiceBusy` outlives Close / New conversation during transcribing, so one press during the stale request is ignored; wontfix, decided by the review oracle.
- Final counts: bug 1 fixed; suggestion 3 fixed; nit 1 fixed, 1 wontfix; 0 open.
- Disposition: **accepted-exceptions** (M6 wontfix, rationale in `review/disposition.md`).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md` (final ledger), `review/disposition.md`.

## Review

- Two rounds. Disposition accepted-exceptions, 0 open. See `### Review` under Results.
- Round 2: Done
## Notes

- Ask panel: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor` (composer `MessageInput` with lead `Tag` / `Camera` / `AskEditModePill`, trail `AskSettingsMenu`, `AskSendButton`).
- JS: `source/container-apps/web/projects/web-spa/source/features/ask-ai.ts`, C# interop: `source/container-apps/web/projects/web-spa/services/ask-ai-js-module.cs`.
- Icon: FluentUI `Icons.Regular.Size20.Mic`. No vendored asset.
- Precondition: before any code, confirm the xAI speech-to-text endpoint exists in the current xAI API docs and record the endpoint, auth header, and request shape in Notes. If xAI has no speech-to-text endpoint, stop and report; do not substitute another provider. Config follows the `XAI:*` pattern per `CatalogAgentAvailability`.
- Confirmed 2026-11-24 against https://docs.x.ai/developers/model-capabilities/audio/speech-to-text. Endpoint: `POST {XAI:Endpoint}/stt` (default `https://api.x.ai/v1/stt`). Auth header: `Authorization: Bearer <XAI:ApiKey>`. Body: `multipart/form-data` with `model` (`grok-voice-transcribe-2.0`) and `file` last (browser `audio/webm` is a supported container). Response JSON field `text` is the transcript. The key is the same `XAI:ApiKey` / `XAI:Endpoint` pair Ask already uses. No other provider.
