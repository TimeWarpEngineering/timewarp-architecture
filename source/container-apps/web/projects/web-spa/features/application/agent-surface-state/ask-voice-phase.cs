#region Purpose
// The Ask composer's microphone: idle, recording, transcribing, or failed.
#endregion

#region Design
// Recording keeps the stop control usable while a turn streams or an approval card is waiting.
// Error keeps the clip only when another send might succeed: a transport failure, 429, or 5xx.
// A refusal of this clip clears it, so the next press records. An insert failure keeps the
// transcript instead of the clip.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Where the Ask microphone is in a dictation.</summary>
public enum AskVoicePhase
{
  /// <summary>The microphone is off.</summary>
  Idle = 0,

  /// <summary>The browser is capturing. The button stops it.</summary>
  Recording = 1,

  /// <summary>The clip is on its way to speech-to-text. The button waits.</summary>
  Transcribing = 2,

  /// <summary>The last press failed. The button retries.</summary>
  Error = 3
}
