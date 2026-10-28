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

## Checklist

- [ ] Add voice-input button to Ask composer matching Grok's mic icon
- [ ] Wire browser mic capture via `ask-ai.ts` / `AskAiJsModule`
- [ ] Integrate Grok Voice API transcription into composer text
- [ ] Handle denied mic, API error, and transcribing states
- [ ] Add/Update Playwright tests
- [ ] Verify functionality

## Session

- Created: ses_e7b3cf52affeeONsuSseW9xQE5 (2026-10-28)

## Notes

- Ask panel: `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor` (composer `MessageInput` with lead `Tag` / `Camera` / `AskEditModePill`, trail `AskSettingsMenu`, `AskSendButton`).
- JS: `source/container-apps/web/projects/web-spa/source/features/ask-ai.ts`, C# interop: `source/container-apps/web/projects/web-spa/services/ask-ai-js-module.cs`.
- Icon intent: same mic glyph Grok uses for voice input; pick the matching FluentUI icon (`Icons.Regular.Size*` mic) unless Grok's exact asset is vendored.
- Backend will call Grok Voice API — confirm endpoint/auth/config (`XAI:*` pattern per `CatalogAgentAvailability`) during implementation.
