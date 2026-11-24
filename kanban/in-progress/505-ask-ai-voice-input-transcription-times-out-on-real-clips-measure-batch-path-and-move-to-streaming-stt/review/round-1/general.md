# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/505-ask-ai-voice-input-transcription-times-out-on-real` vs merge-base with `master` (dictation relay, xAI streaming client, Ask composer capture, Playwright voice tests) and `## Goal (implementer restatement)` against the original task.

## Summary

The change removes the batch `POST /v1/stt` upload and relays browser PCM to `wss://api.x.ai/v1/stt` through `/api/agent-chat/transcriptions/stream`. The API key stays on the server. Partials are the whole text so far and land in the composer while recording; stop replaces that preview with the final text and does not send the prompt. The recorded root cause (Aspire's 10 s resilience retry on the named client, then the 30 s `HttpClient` timeout) is removed with that client. Connect (10 s) and finalize (10 s) timeouts are explicit and tied to the measurements in Design. Timeout, no-speech, busy, and missing-key codes each have their own sentence.

The restatement matches the task. Part 1's named-client timeout is superseded by Part 2 once a server-relayed transcription socket exists, and the checklist says so. No misread. The `## Results` timings, streaming notes, and `### How to validate` / `### Gates` record cover this diff (build 0/0, dictation runfile 21/21, Playwright `AskVoiceInput` 2/2 and `AskFocus` 1/1, `XaiChatUpstream` 16/16, ingress golden unchanged, audit pass after the `ask-ai.ts` split). Gates were not re-run.

Checked and not filed: the ingress already carves out `api/agent-chat`, so the new socket stays on web-server; `DictationTranscript` matches the measured interim / chunk-final / utterance-final order; the browser and server share one copy of the path, rates, cap, and message names; Close and New conversation drop a late insert via the epoch; Playwright was rewritten for the stream rather than loosened to hide a failure; no new suppression silences an audit or test.

## Issues

None.
