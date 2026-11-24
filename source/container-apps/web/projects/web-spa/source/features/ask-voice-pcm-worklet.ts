// #region Purpose
// Audio worklet for Ask dictation: turns the microphone's float samples into 100 ms frames of
// mono PCM16 and posts each frame to ask-ai.ts, which sends it on the dictation socket.
// #endregion
//
// #region Design
// Task 505. xAI streaming speech-to-text takes signed 16-bit little-endian PCM at the rate the
// socket names, so the worklet does not resample: it uses the context's sampleRate, which
// ask-ai.ts checked against the accepted rates. Channel 0 only. Each frame's buffer is transferred,
// not copied. The worklet scope has no DOM types, so the few globals it uses are declared here;
// export {} keeps those declarations local to this module.
// #endregion

declare class AudioWorkletProcessor {
  readonly port: MessagePort;
  constructor();
}

declare function registerProcessor(name: string, processor: new () => AudioWorkletProcessor): void;

declare const sampleRate: number;

const FrameSeconds = 0.1;

class AskVoicePcm extends AudioWorkletProcessor {
  private frame: Int16Array;
  private filled = 0;

  constructor() {
    super();
    this.frame = new Int16Array(Math.max(1, Math.round(sampleRate * FrameSeconds)));
  }

  process(inputs: Float32Array[][]): boolean {
    const channel = inputs[0]?.[0];
    if (channel === undefined) {
      return true;
    }

    for (let index = 0; index < channel.length; index++) {
      const sample = Math.max(-1, Math.min(1, channel[index] ?? 0));
      this.frame[this.filled++] = sample < 0 ? sample * 0x8000 : sample * 0x7fff;
      if (this.filled === this.frame.length) {
        const full = this.frame.buffer as ArrayBuffer;
        this.port.postMessage(full, [full]);
        this.frame = new Int16Array(this.frame.length);
        this.filled = 0;
      }
    }

    return true;
  }
}

registerProcessor("ask-voice-pcm", AskVoicePcm);

export {};
