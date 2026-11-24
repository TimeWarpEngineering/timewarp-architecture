// #region Purpose
// Ask dictation in the browser: microphone, PCM worklet, the dictation socket, the live preview in
// the composer, and the final insert. Re-exported by ask-ai.ts.
// #endregion
//
// #region Design
// StartVoiceCapture / StopVoiceCapture / CancelVoiceCapture own the microphone (task 453) and,
// since task 505, stream it. Start opens the microphone, an AudioContext with the
// ask-voice-pcm worklet (mono PCM16 frames of 100 ms at the context rate), and a WebSocket to
// the web server's dictation path. Every constant the server also uses (path, rates, cap,
// finalize time, message names) comes in from C# in the options, so this file has no copy. Frames
// sent before the server says ready are held and flushed on ready. Partial messages carry the
// whole text so far and are written into the composer at the caret where recording started (the
// preview). If the person edits that text, the preview stops writing. Stop sends done and waits
// for the final text, at most the server's finalize time plus a grace; a stall is timed-out. A
// server error, a closed socket, or the cap ends the session; VoiceCaptureEnded tells the panel's
// clock to dispatch the stop. CommitComposerText replaces the preview with the final text, or
// inserts at the caret when there is no preview, adds a space when the neighbor is a word,
// dispatches input, and focuses the textarea. It does not send. A mismatched insert epoch
// (Close or New conversation) writes nothing. This file never sees an API key.
// Split out of ask-ai.ts (task 505) so each file stays under the 500-line cap.
// #endregion

import { ComposerSelector } from "./ask-composer.js";

type VoiceOptions = {
  path: string;
  sampleRateQuery: string;
  sampleRates: number[];
  maxSeconds: number;
  finalizeSeconds: number;
  doneMessage: string;
  readyType: string;
  partialType: string;
  doneType: string;
  errorType: string;
  epoch: number;
};

type VoiceResult = {
  error: string | null;
  text: string | null;
};

type VoicePreview = {
  anchor: number;
  shown: string;
};

type VoiceSession = {
  options: VoiceOptions;
  stream: MediaStream;
  context: AudioContext | null;
  source: MediaStreamAudioSourceNode | null;
  node: AudioWorkletNode | null;
  socket: WebSocket;
  pending: ArrayBuffer[];
  ready: boolean;
  doneSent: boolean;
  sentSamples: number;
  maxSamples: number;
  sampleRate: number;
  capped: boolean;
  stopRequested: boolean;
  finished: boolean;
  finalText: string | null;
  error: string | null;
  preview: VoicePreview | null;
  settle: (() => void) | null;
};

const VoiceWorkletUrl = new URL("./ask-voice-pcm-worklet.js", import.meta.url);
const VoiceProcessorName = "ask-voice-pcm";
const VoiceClientGraceSeconds = 5;

let voice: VoiceSession | null = null;
let voiceInsertEpoch = 0;
let lastPreview: VoicePreview | null = null;

function voiceResult(error: string | null, text: string | null): VoiceResult {
  return { error, text };
}

function voiceSocketUrl(options: VoiceOptions, sampleRate: number): string {
  const url = new URL(options.path, document.baseURI);
  url.protocol = url.protocol === "https:" ? "wss:" : "ws:";
  url.searchParams.set(options.sampleRateQuery, String(sampleRate));
  return url.toString();
}

function releaseVoiceAudio(session: VoiceSession): void {
  session.node?.port.close();
  session.node?.disconnect();
  session.source?.disconnect();
  session.node = null;
  session.source = null;
  const context = session.context;
  session.context = null;
  if (context !== null && context.state !== "closed") {
    void context.close().catch(() => undefined);
  }

  for (const track of session.stream.getTracks()) {
    track.stop();
  }
}

function closeVoiceSocket(session: VoiceSession): void {
  const socket = session.socket;
  socket.onmessage = null;
  socket.onclose = null;
  socket.onerror = null;
  if (socket.readyState === WebSocket.OPEN || socket.readyState === WebSocket.CONNECTING) {
    try {
      socket.close(1000);
    } catch {
      // already closing
    }
  }
}

function endVoice(session: VoiceSession, error: string | null, text: string | null): void {
  if (session.finished) {
    return;
  }

  session.finished = true;
  session.error = error;
  session.finalText = text;
  releaseVoiceAudio(session);
  const settle = session.settle;
  session.settle = null;
  settle?.();
}

function sendVoiceDone(session: VoiceSession): void {
  if (session.doneSent || !session.ready || session.socket.readyState !== WebSocket.OPEN) {
    return;
  }

  session.doneSent = true;
  session.socket.send(session.options.doneMessage);
}

function sendVoiceFrame(session: VoiceSession, frame: ArrayBuffer): void {
  if (session.finished || session.capped || session.doneSent) {
    return;
  }

  const samples = frame.byteLength / 2;
  if (session.sentSamples + samples > session.maxSamples) {
    // The cap is the server's too. Stop listening and ask for the transcript of what was heard.
    session.capped = true;
    releaseVoiceAudio(session);
    sendVoiceDone(session);
    return;
  }

  session.sentSamples += samples;
  if (!session.ready) {
    session.pending.push(frame);
    return;
  }

  session.socket.send(frame);
}

function onVoiceMessage(session: VoiceSession, event: MessageEvent): void {
  if (typeof event.data !== "string") {
    return;
  }

  let message: { type?: unknown; text?: unknown; code?: unknown };
  try {
    message = JSON.parse(event.data) as { type?: unknown; text?: unknown; code?: unknown };
  } catch {
    return;
  }

  const options = session.options;
  if (message.type === options.readyType) {
    session.ready = true;
    for (const frame of session.pending) {
      session.socket.send(frame);
    }

    session.pending = [];
    if (session.capped || session.stopRequested) {
      // The cap or a stop came before the upstream was ready.
      sendVoiceDone(session);
    }

    return;
  }

  if (message.type === options.partialType && typeof message.text === "string") {
    showVoicePreview(session, message.text);
    return;
  }

  if (message.type === options.doneType && typeof message.text === "string") {
    endVoice(session, null, message.text);
    return;
  }

  if (message.type === options.errorType) {
    endVoice(session, typeof message.code === "string" ? message.code : "failed", null);
  }
}

function decorate(before: string, text: string, after: string): string {
  let inserted = text;
  if (inserted.length === 0) {
    return inserted;
  }

  if (before.length > 0 && !/\s/.test(before[before.length - 1] ?? "")) {
    inserted = " " + inserted;
  }

  if (after.length > 0 && !/^\s/.test(after)) {
    inserted = inserted + " ";
  }

  return inserted;
}

function showVoicePreview(session: VoiceSession, text: string): void {
  const preview = session.preview;
  if (preview === null || session.options.epoch !== voiceInsertEpoch) {
    return;
  }

  const area = document.querySelector<HTMLTextAreaElement>(ComposerSelector);
  if (area === null || area.value.slice(preview.anchor, preview.anchor + preview.shown.length) !== preview.shown) {
    // The person edited the preview, or the composer went away. Stop writing into it.
    session.preview = null;
    return;
  }

  const before = area.value.slice(0, preview.anchor);
  const after = area.value.slice(preview.anchor + preview.shown.length);
  const shown = decorate(before, text, after);
  area.value = before + shown + after;
  preview.shown = shown;
  const caret = before.length + shown.length;
  area.selectionStart = caret;
  area.selectionEnd = caret;
  area.dispatchEvent(new Event("input", { bubbles: true }));
}

function waitForVoiceEnd(session: VoiceSession, seconds: number): Promise<void> {
  if (session.finished) {
    return Promise.resolve();
  }

  return new Promise((resolve) => {
    const timer = window.setTimeout(() => {
      session.settle = null;
      endVoice(session, "timed-out", null);
      resolve();
    }, seconds * 1000);
    session.settle = () => {
      window.clearTimeout(timer);
      resolve();
    };
  });
}

export function SetVoiceInsertEpoch(epoch: number): void {
  voiceInsertEpoch = epoch;
}

export function VoiceCaptureEnded(): boolean {
  return voice !== null && (voice.finished || voice.capped);
}

export async function StartVoiceCapture(options: VoiceOptions): Promise<VoiceResult> {
  await CancelVoiceCapture();
  if (
    typeof navigator === "undefined" ||
    navigator.mediaDevices?.getUserMedia === undefined ||
    typeof AudioContext === "undefined" ||
    typeof AudioWorkletNode === "undefined" ||
    typeof WebSocket === "undefined"
  ) {
    return voiceResult("unavailable", null);
  }

  let stream: MediaStream;
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      audio: { channelCount: 1, echoCancellation: true, noiseSuppression: true },
    });
  } catch (error) {
    const name = error instanceof DOMException ? error.name : "";
    if (name === "NotAllowedError" || name === "PermissionDeniedError" || name === "SecurityError") {
      return voiceResult("denied", null);
    }

    return voiceResult("unavailable", null);
  }

  let context: AudioContext | null = null;
  try {
    context = new AudioContext();
    if (!options.sampleRates.includes(context.sampleRate)) {
      await context.close();
      context = new AudioContext({ sampleRate: 48000 });
    }

    await context.audioWorklet.addModule(VoiceWorkletUrl);
    if (context.state === "suspended") {
      await context.resume();
    }
  } catch {
    if (context !== null) {
      void context.close().catch(() => undefined);
    }

    for (const track of stream.getTracks()) {
      track.stop();
    }

    return voiceResult("failed", null);
  }

  const sampleRate = context.sampleRate;
  let socket: WebSocket;
  try {
    socket = new WebSocket(voiceSocketUrl(options, sampleRate));
  } catch {
    void context.close().catch(() => undefined);
    for (const track of stream.getTracks()) {
      track.stop();
    }

    return voiceResult("failed", null);
  }

  socket.binaryType = "arraybuffer";
  const area = document.querySelector<HTMLTextAreaElement>(ComposerSelector);
  const session: VoiceSession = {
    options,
    stream,
    context,
    source: null,
    node: null,
    socket,
    pending: [],
    ready: false,
    doneSent: false,
    sentSamples: 0,
    maxSamples: sampleRate * options.maxSeconds,
    sampleRate,
    capped: false,
    stopRequested: false,
    finished: false,
    finalText: null,
    error: null,
    preview: area === null ? null : { anchor: area.selectionEnd ?? area.value.length, shown: "" },
    settle: null,
  };
  voiceInsertEpoch = options.epoch;
  lastPreview = null;
  socket.onmessage = (event) => onVoiceMessage(session, event);
  socket.onerror = () => undefined;
  socket.onclose = () => {
    endVoice(session, "failed", null);
  };

  try {
    const source = context.createMediaStreamSource(stream);
    const node = new AudioWorkletNode(context, VoiceProcessorName, {
      numberOfInputs: 1,
      numberOfOutputs: 1,
      channelCount: 1,
    });
    node.port.onmessage = (event: MessageEvent) => {
      if (event.data instanceof ArrayBuffer) {
        sendVoiceFrame(session, event.data);
      }
    };
    source.connect(node);
    node.connect(context.destination);
    session.source = source;
    session.node = node;
  } catch {
    releaseVoiceAudio(session);
    closeVoiceSocket(session);
    return voiceResult("failed", null);
  }

  voice = session;
  return voiceResult(null, null);
}

export async function StopVoiceCapture(): Promise<VoiceResult> {
  const session = voice;
  if (session === null) {
    return voiceResult("empty", null);
  }

  releaseVoiceAudio(session);
  if (!session.finished) {
    // Before ready, the ready handler flushes the pending audio, then asks for the transcript.
    session.stopRequested = true;
    sendVoiceDone(session);
    const grace = session.options.finalizeSeconds + VoiceClientGraceSeconds;
    await waitForVoiceEnd(session, grace);
  }

  if (voice === session) {
    voice = null;
  }

  closeVoiceSocket(session);
  lastPreview = session.preview;
  if (session.error !== null) {
    return voiceResult(session.error, null);
  }

  return voiceResult(null, session.finalText ?? "");
}

export async function CancelVoiceCapture(): Promise<void> {
  const session = voice;
  voice = null;
  if (session === null) {
    return;
  }

  session.preview = null;
  lastPreview = null;
  releaseVoiceAudio(session);
  closeVoiceSocket(session);
  const settle = session.settle;
  session.settle = null;
  settle?.();
}

export function CommitComposerText(text: string, epoch: number): boolean {
  if (epoch !== voiceInsertEpoch) {
    return false;
  }

  const area = document.querySelector<HTMLTextAreaElement>(ComposerSelector);
  if (area === null || text.length === 0) {
    return false;
  }

  const preview = lastPreview;
  lastPreview = null;
  let start = area.selectionStart ?? area.value.length;
  let end = area.selectionEnd ?? start;
  if (preview !== null && area.value.slice(preview.anchor, preview.anchor + preview.shown.length) === preview.shown) {
    start = preview.anchor;
    end = preview.anchor + preview.shown.length;
  }

  const before = area.value.slice(0, start);
  const after = area.value.slice(end);
  const inserted = decorate(before, text, after);
  area.value = before + inserted + after;
  const caret = before.length + inserted.length;
  area.selectionStart = caret;
  area.selectionEnd = caret;
  area.dispatchEvent(new Event("input", { bubbles: true }));
  area.focus();
  return true;
}
