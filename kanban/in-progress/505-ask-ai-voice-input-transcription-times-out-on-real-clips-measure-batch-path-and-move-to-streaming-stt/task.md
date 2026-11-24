# Ask AI voice input: transcription times out on real clips; measure batch path and move to streaming STT

## Description

Task 453 shipped voice input for the Ask AI panel as a batch flow: the browser records the whole
clip with MediaRecorder (`audio/webm;codecs=opus`, 1 s timeslices), base64s it into
`TranscribeAgentAudio.Command`, and the web server posts it as multipart to
`https://api.x.ai/v1/stt` with `grok-voice-transcribe-2.0`.

Observed on 2026-11-24 (local Aspire run): a three-word clip transcribed after several seconds; a
normal spoken prompt failed with "The recording could not be transcribed." The web-server log shows
why:

```
xAI speech-to-text request failed
TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing.
```

xAI's batch latency grows with clip length and crosses the 30 s timeout the implementer set on the named `HttpClient` on
any real prompt. The result is then thrown away. grok.com does not work this way: it streams audio
while the user talks and shows partial text with sub-second latency.

## Requirements

### Part 1: stop losing transcripts

- Measure the batch path. Record one short (3 s) and one realistic (20 s) clip from the SPA, save
  the exact bytes the server sends, and time `curl -F model=grok-voice-transcribe-2.0 -F file=@…
  https://api.x.ai/v1/stt` for each. Repeat with the same audio re-encoded as wav (16 kHz PCM) and
  mp3. Record the timings in `## Results`. The xAI voice guide lists 15 accepted formats but names
  only mp3 in examples; webm/opus from Chrome carries no duration header and is the first suspect.
- If a format change gives a large win, switch the browser capture or add a server-side transcode,
  whichever is smaller. Do not add an npm dependency.
- Set an explicit timeout on the `xai-speech-to-text` named client in
  `source/container-apps/web/features/agent-chat/xai-chat-registration-server.cs` sized from the
  measurements (30 s is a hand-picked value, not a default), and make the SPA copy for the timeout case say the clip was
  too long to transcribe in time rather than the generic failure.

### Part 2: streaming transcription

- Find xAI's streaming transcription documentation. The voice guide says audio can be transcribed
  "in a single call or stream over WebSocket" but the guide page documents only the batch call and
  the speech-to-speech `wss://api.x.ai/v1/realtime` agent endpoint. If a transcription-only
  WebSocket exists, record its URL, auth (ephemeral token or server relay), and message shape in
  `## Results`.
- If it exists: replace the batch flow with streaming. Audio chunks go browser → web server →
  xAI over a WebSocket (the key stays on the server, as today), partial transcripts flow back and
  render into the Ask box while recording. Keep `TranscribeAgentAudio` only if something still
  needs the batch path; otherwise remove it and its tests.
- If it does not exist, or needs the realtime agent endpoint with a per-session ephemeral token,
  stop after Part 1 and write that up in `## Results` with the option you recommend.

### Constraints

- All browser interaction stays TimeWarp.State actions (`tw-blazor`). No page-local JS calls
  outside the existing `ask-ai.ts` interop surface.
- The xAI key never reaches the browser.
- Keep the 453 Playwright coverage green or update it for the new flow.

## Goal (implementer restatement)

Real dictated prompts fail because the batch xAI speech-to-text call takes longer than the 30 s
timeout on the `xai-speech-to-text` client, and the transcript is thrown away. Find out why the
batch path is slow (time webm, wav, and mp3 at about 3 s and 20 s against `POST /v1/stt`), fix the
loss, and move to streaming transcription if xAI offers a transcription-only stream that keeps the
key on the server.

Acceptance:

- `## Results` holds curl timings for webm/opus, wav (16 kHz PCM), and mp3 at 3 s and 20 s.
- A format change ships only if it wins clearly (browser capture or server transcode, no npm).
- The `xai-speech-to-text` client timeout is set from the measurements, with the reason in Design.
- A timeout shows its own sentence in the Ask box (clip took too long to transcribe), not the
  generic failure.
- `## Results` records xAI's streaming transcription doc (URL, auth, message shape), or that it
  does not exist.
- If it exists and the key can stay on the server: browser → web server → xAI WebSocket, with
  partial text in the Ask box while recording, and the batch path removed if nothing needs it.
  Otherwise Part 2 stops with a written recommendation.
- Constraints hold: browser work goes through TimeWarp.State actions and the `ask-ai.ts` interop,
  the key never reaches the browser, and the 453 Playwright coverage stays green or is updated.
- Gates: `dev build`, the touched test files, and `ganda repo audit` all pass.

## Checklist

- [x] Timings for webm, wav, mp3 at 3 s and 20 s recorded in `## Results`
- [x] Format change applied if it wins, explicit client timeout set, timeout copy added
      (no format change: none wins; the batch client is gone, the stream has explicit connect and
      finalize timeouts, and `AskVoiceCopy.TimedOut` is the timeout copy)
- [x] Streaming STT documentation found and recorded, or its absence recorded
- [x] Streaming flow implemented, or Part 2 stopped with a written recommendation
- [x] `dev build`, web-spa integration tests, Playwright voice tests, `ganda repo audit` green

## Depends on

- 453

## Session

- Created: 2026-11-24 from Steve's local test of 453 (short clip ok, real prompt timed out)
- 2026-11-24 implementer (Claude): measured batch and streaming against real xAI, found the root
  cause in the master AppHost traces, replaced the batch upload with a streaming relay.

## Results

### Root cause of the 30 s timeout

It was not xAI latency. The failed trace in the running master AppHost (trace
`4ae3c2eba5c755fe5ee09806a81fecb6`, 2026-11-24 01:19 UTC) shows three POSTs to
`https://api.x.ai/v1/stt` inside one request: 9.997 s, 10.000 s, 5.338 s, each
`TaskCanceledException`. The `xai-speech-to-text` named client inherited the Aspire standard
resilience handler from `ConfigureHttpClientDefaults` (aspire-service-defaults `extensions.cs`):
10 s per attempt, retries on timeout. So any clip whose call took more than 10 s was cut, the POST
was resent (spending the key again), and the 30 s `HttpClient.Timeout` ended it. The short clip
that worked took 3.34 s end to end in the same AppHost (trace `1bb67d17…`).

### Batch timings (Part 1)

Clips: xAI TTS speech played into Chrome's fake microphone and recorded with the SPA's exact
MediaRecorder settings (`audio/webm;codecs=opus`, 1 s timeslices); the webm has no duration
(`ffprobe` says N/A). wav = the same audio as 16 kHz mono PCM, mp3 = 16 kHz mono 32 kbps.
`curl -F model=grok-voice-transcribe-2.0 -F file=@… https://api.x.ai/v1/stt`, two runs each,
from this WSL host on 2026-11-24:

| Clip | Bytes | Run 1 | Run 2 |
|------|------:|------:|------:|
| 3 s webm/opus | 43,740 | 0.69 s | 0.67 s |
| 3 s wav 16 kHz | 96,078 | 0.78 s | 0.70 s |
| 3 s mp3 | 12,608 | 0.48 s | 0.48 s |
| 20 s webm/opus | 295,807 | 2.06 s | 2.22 s |
| 20 s wav 16 kHz | 639,438 | 2.76 s | 3.02 s |
| 20 s mp3 | 80,432 | 1.01 s | 1.07 s |

The server's own .NET multipart shape (model then file, `audio/webm`) took 0.55 s and
1.55–1.80 s for the same 3 s and 20 s webm. Every format transcribed the same text. webm without a
duration header is not the problem; mp3 saves about 1 s at 20 s, which is not worth a browser
encoder or a server transcode. No format change.

### Streaming STT (Part 2)

xAI has a transcription-only WebSocket: `wss://api.x.ai/v1/stt` (schema
https://docs.x.ai/stt-streaming.ws.json, linked from the Voice API reference; checked 2026-11-24).

- Auth: `Authorization: Bearer <xAI API key>` header. That suits a server relay. No ephemeral
  token is needed and the key stays on the web server.
- Query: `sample_rate` (8000, 16000, 22050, 24000, 44100, 48000), `encoding` (`pcm` = s16le,
  `mulaw`, `alaw`, `opus` raw packets), `interim_results`, `endpointing`, `language`, `model`
  (default `grok-voice-transcribe-2.0`), and others.
- Client → server: binary frames of raw audio (about 100 ms each); text `{"type":"audio.done"}`
  to flush and finish; `{"type":"finalize"}` to force an utterance end.
- Server → client: `transcript.created` (wait for it before audio), `transcript.partial` with
  `text`, `is_final`, `speech_final`, `start`, `duration`, `words`; `transcript.done` (the
  connection closes after it); `error` with `message`.
- Observed behavior: an interim partial carries the current chunk's text so far; a chunk final
  locks it; an utterance final (`speech_final`) repeats the whole utterance; `transcript.done`
  arrives with empty `text`. So the relay builds the transcript itself (`DictationTranscript`).

Measured with real-time-paced 100 ms PCM frames, same clips:

| Clip | Connect to `transcript.created` | First partial after audio start | `audio.done` to `transcript.done` |
|------|------:|------:|------:|
| 3 s (16 kHz) | 0.75–0.90 s | 1.05–1.22 s | 0.29–0.42 s |
| 20 s (16 kHz) | 0.75–0.90 s | 1.09–1.24 s | 0.23–0.29 s |
| 8 s cut mid-sentence | 0.95 s | 1.12 s | 0.47 s (flush gave the utterance final) |
| 3 s at 48 kHz | 0.86 s | 1.05 s | 0.26 s |

### What changed

- Batch path removed: `TranscribeAgentAudio.Command`/`Response`/`Validator`, its handler, the
  multipart builder, the `xai-speech-to-text` named HttpClient (and with it the resilience
  handler that caused the bug), and their tests. `TranscribeAgentAudio` is now the stream's shared
  constants (path, sample rates, `MaxSeconds` 300, `FinalizeSeconds` 10, message types, error
  codes, fake transcript).
- Server: `TranscribeAgentAudioEndpoint` maps `/api/agent-chat/transcriptions/stream` (GET and
  HTTP/2 CONNECT; Chrome uses extended CONNECT over TLS, and MapGet alone returned 405) behind the
  same policy and schemes as the chat relay; `UseWebSockets` added to `Program.ConfigureMiddleware`.
  `DictationRelay` (application) checks the principal, sample rate, key, and admission cap, opens
  the upstream, pumps audio up, sends `partial` (whole text so far) and `done` down, and reports
  `timed-out` when the final text does not come within `FinalizeSeconds`. `XaiSpeechToText` opens
  the xAI socket with the server key and waits for `transcript.created` under a 10 s connect
  timeout. Timeouts are sized from the measurements above (10 s is about 10x the slowest connect
  and about 20x the slowest finalize).
- Browser: `ask-voice-pcm-worklet.ts` (AudioWorklet, 100 ms PCM16 frames at the context rate);
  `ask-voice.ts` (re-exported by `ask-ai.ts`, so `AskAiJsModule` still imports one module) opens
  the socket, buffers until ready, writes partial text into the composer at the
  caret as a live preview, and on stop sends done and replaces the preview with the final text
  (`CommitComposerText`). All server-shared constants come from C# (`AskVoiceStreamOptions`).
  `Dictate` stays the one TimeWarp.State action; the panel's one-second clock dispatches it when
  the socket ends on its own.
- Copy: `AskVoiceCopy.TimedOut` ("The recording took too long to transcribe. Try a shorter
  one."), `NoSpeech`, `Busy`, and `ForCode` mapping each code to a sentence.

### How to validate

Smoke: with an xAI key set (`dotnet user-secrets set "XAI:ApiKey" … --id
0e53fdd3-6f93-4d5a-9c86-040621f7929e`), `dev run`, sign in, open Ask, type a word, press the
microphone and speak a 20–30 s prompt, then press stop.

Expect: words appear in the Ask box while you speak (about 1 s behind). After stop the preview is
replaced by the final punctuated text within about a second, the prompt is not sent, and the
web-server log has no `xai` timeout. Without a key the microphone shows "Voice input needs an xAI
key." In the Aspire dashboard the dictation is one `GET /api/agent-chat/transcriptions/stream`
span, not a POST to `/v1/stt`.

### Gates

- `./bin/dev build`: succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/agent-chat/transcribe-agent-audio/transcribe-agent-audio-tests.cs`: 21/21 passed.
- `tests/container-apps/web/web-spa-playwright-tests/ask-voice-input-playwright-tests.cs`: a
  project test, not a standalone runfile (no `#if !JARIBU_MULTI` guard). Ran locally from the
  built project with `web-spa-playwright-tests --filter-class AskVoiceInput`: 2/2 passed. CI runs
  it with the Playwright suite.
- `web-server-integration-tests --filter-class XaiChatUpstream` (covers `XaiChatRegistration`):
  16/16 passed. Project test; CI runs it.
- `dev regenerate-generated`: no diff under `goldens/ingress`.
- `ganda repo audit` (first run): FAIL `source-file-line-cap`, `ask-ai.ts` at 635 lines. Fixed by
  moving the dictation code to `ask-voice.ts` and the composer selector to `ask-composer.ts`
  (`ask-ai.ts` 186 lines, `ask-voice.ts` 471). Rebuilt (`./bin/dev build` green) and reran the
  Playwright classes `AskVoiceInput` 2/2 and `AskFocus` 1/1. The `vscode-window-icon` warning
  (local `.vscode/settings.json` missing, git-ignored) is fixed by `ganda repo audit --fix`.
- `ganda repo audit --fix` (after the split): "Repository passes all audit checks."
- Not run here: `dev template-smoke` (no `#if` flag or template-symbol change; CI smokes the
  default variant on the PR) and `dev check-version` (template-only change, no platform package).
