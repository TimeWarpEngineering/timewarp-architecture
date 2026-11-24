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

xAI's batch latency grows with clip length and crosses the default 30 s `HttpClient` timeout on
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
  measurements (not the 30 s default), and make the SPA copy for the timeout case say the clip was
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

## Checklist

- [ ] Timings for webm, wav, mp3 at 3 s and 20 s recorded in `## Results`
- [ ] Format change applied if it wins, explicit client timeout set, timeout copy added
- [ ] Streaming STT documentation found and recorded, or its absence recorded
- [ ] Streaming flow implemented, or Part 2 stopped with a written recommendation
- [ ] `dev build`, web-spa integration tests, Playwright voice tests, `ganda repo audit` green

## Depends on

- 453

## Session

- Created: 2026-11-24 from Steve's local test of 453 (short clip ok, real prompt timed out)
