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

## Checklist

- [x] Add voice-input button to Ask composer matching Grok's mic icon
- [x] Wire browser mic capture via `ask-ai.ts` / `AskAiJsModule`
- [x] Integrate Grok Voice API transcription into composer text
- [x] Handle denied mic, API error, and transcribing states
- [x] Add/Update Playwright tests
- [x] Verify functionality

## Session

- Created: ses_e7b3cf52affeeONsuSseW9xQE5 (2026-10-28)
- Implementation: voice input on this branch (2026-11-24). The transcription runfile does not copy the neighbour `NoWarn` list. CA1707, CA1849, and CA2000 are fixed in that file. `vscode-window-icon` stays the pre-existing advisory: `.vscode/settings.json` is gitignored local state.

## Results

The Ask composer lead row has a microphone. A click records in the browser, the server posts the clip to xAI speech-to-text, and the transcript is inserted at the caret. The message is not sent. The API key stays on the server. Recording shows a stop icon and elapsed time. A refused microphone, an empty clip, and a failed request each show a sentence and a retry. Retry resends the same clip when the failure was the request. The button disables while a turn streams or an approval is waiting, and stays usable to stop a recording already going. `aria-disabled` is set because Fluent's `disabled` attribute is not what assistive tech or Playwright reads on `fluent-button`.

### How to validate

**Smoke** — open Ask, click the microphone (`data-qa=AskVoiceInput`), allow it, speak, and click again to stop.

**Expect** — the composer gains the transcript and does not send. The name cycles "Dictate a prompt", then "Stop recording" with `aria-pressed` true and an `m:ss` elapsed label, then "Transcribing" while the button is disabled. A denied microphone shows "Microphone access was denied." and "Try voice input again." A dropped request shows "The recording could not be transcribed." and the same retry name. Retry inserts the transcript at the caret (a typed "Please" becomes "Please Voice note." under the fake upstream) and focuses the composer. While an answer streams, Stop is visible and the microphone is disabled; it enables again when the turn ends.

**Automated gate**

- `./bin/dev build` — succeeded, 0 warnings, 0 errors, 00:00:21.54.
- `dotnet run --file source/container-apps/web/features/agent-chat/transcribe-agent-audio/transcribe-agent-audio-tests.cs` — exit 0, 10 passed (handler 5, speech request and mapping 3, validator 2).
- `tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs` is not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs it with the Playwright project. Filtered proof from that project directory: `dotnet test -c Release --no-build --no-restore -- --filter-class AskVoiceInput_Given_Wasm` — passed, 1 test.
- `ganda repo audit` — exit 0 after the transcription runfile was staged executable (`runfile-executable` had failed on the first commit because the shebang was mode 100644). 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing).

## Notes

- Ask panel: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor` (composer `MessageInput` with lead `Tag` / `Camera` / `AskEditModePill`, trail `AskSettingsMenu`, `AskSendButton`).
- JS: `source/container-apps/web/projects/web-spa/source/features/ask-ai.ts`, C# interop: `source/container-apps/web/projects/web-spa/services/ask-ai-js-module.cs`.
- Icon: FluentUI `Icons.Regular.Size20.Mic`. No vendored asset.
- Precondition: before any code, confirm the xAI speech-to-text endpoint exists in the current xAI API docs and record the endpoint, auth header, and request shape in Notes. If xAI has no speech-to-text endpoint, stop and report; do not substitute another provider. Config follows the `XAI:*` pattern per `CatalogAgentAvailability`.
- Confirmed 2026-11-24 against https://docs.x.ai/developers/model-capabilities/audio/speech-to-text. Endpoint: `POST {XAI:Endpoint}/stt` (default `https://api.x.ai/v1/stt`). Auth header: `Authorization: Bearer <XAI:ApiKey>`. Body: `multipart/form-data` with `model` (`grok-voice-transcribe-2.0`) and `file` last (browser `audio/webm` is a supported container). Response JSON field `text` is the transcript. The key is the same `XAI:ApiKey` / `XAI:Endpoint` pair Ask already uses. No other provider.
