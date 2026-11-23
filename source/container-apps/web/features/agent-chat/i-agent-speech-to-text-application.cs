#region Purpose
// Server port the transcription handler calls. The application layer does not dial xAI itself.
#endregion

#region Design
// Same key slot as IAgentChatUpstream: BeginKeyResolution opens it, IsConfiguredAsync fills it,
// and TranscribeAsync consumes it, so one dictation does not read the key twice. A missing
// implementation of BeginKeyResolution is a no-op. UseFakeUpstream in Development or Testing
// counts as configured and returns TranscribeAgentAudio.FakeTranscript.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

/// <summary>Forwards one audio clip to speech-to-text, or reports that no key is configured.</summary>
public interface IAgentSpeechToText
{
  /// <summary>Opens a one-read slot for the configuration check and the transcribe that follow.</summary>
  void BeginKeyResolution()
  {
  }

  /// <summary>True when the Development/Testing fake is on, or a key can be read.</summary>
  Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

  /// <summary>Sentence for a missing key. The default is the host setup text.</summary>
  string MissingKeyDetail(bool development) => XaiChatDefaults.HostSetupText(development);

  /// <summary>Forwards the clip. Does not log the key or the audio.</summary>
  Task<OneOf<Response, SharedProblemDetails>> TranscribeAsync
  (
    Command command,
    CancellationToken cancellationToken
  );
}
