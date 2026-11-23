#region Purpose
// The Ask composer's microphone: record, stop, and insert the transcript. It does not send.
#endregion

#region Design
// One action. Idle or a microphone failure starts capture. Recording stops it. The clip is sent
// only when its base64 fits TranscribeAgentAudio.MaxDataLength, the one copy of that limit. Past
// it, the bytes are dropped and the too-long sentence is shown. Error with a stored clip sends
// that clip again. The clip is kept only for a transport failure, 429, or 5xx. A 400, 413, or 422
// clears it so the next press records. Error with a stored transcript inserts that text and does
// not call the server. Transcribing ignores a second press. A result that arrives after the phase
// has left Transcribing (Close or New conversation) is ignored. The handler does not dispatch. It
// re-renders before the speech request so the button can show Transcribing. The key stays on the
// server. InsertComposerText places the transcript at the caret when the insert epoch still
// matches. A streaming turn does not start a recording; stop stays enabled when one is going.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

partial class AgentSurfaceState
{
  public static class DictateActionSet
  {
    [CommandPaletteOptOut("Ask composer's microphone; it needs the open panel and the browser mic, and it does not send a prompt.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      Subscriptions subscriptions,
      IJSRuntime jsRuntime,
      IWebServerApiService api
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        if (AgentSurfaceState.VoiceBusy)
        {
          return;
        }

        AgentSurfaceState.VoiceBusy = true;
        try
        {
          if (AgentSurfaceState.VoicePhase == AskVoicePhase.Recording)
          {
            await StopAndTranscribeAsync(cancellationToken);
            return;
          }

          if (AgentSurfaceState.VoicePhase == AskVoicePhase.Error && AgentSurfaceState.VoiceTranscript is not null)
          {
            await InsertStoredAsync();
            return;
          }

          if (AgentSurfaceState.VoicePhase == AskVoicePhase.Error && AgentSurfaceState.VoiceData is not null)
          {
            await TranscribeStoredAsync(cancellationToken);
            return;
          }

          await StartAsync();
        }
        finally
        {
          AgentSurfaceState.VoiceBusy = false;
        }
      }

      private async Task StartAsync()
      {
        AskVoiceCaptureResult started = await AskAiJsModule.StartVoiceCaptureAsync
        (
          jsRuntime,
          TranscribeAgentAudio.MaxDataLength
        );
        if (!AgentSurfaceState.IsPanelOpen)
        {
          await AskAiJsModule.CancelVoiceCaptureAsync(jsRuntime);
          AgentSurfaceState.ClearVoice();
          return;
        }

        if (!string.IsNullOrEmpty(started.Error))
        {
          AgentSurfaceState.ClearVoice();
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = StartError(started.Error);
          return;
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Recording;
        AgentSurfaceState.VoiceStartedAt = DateTimeOffset.UtcNow;
        AgentSurfaceState.VoiceError = null;
        AgentSurfaceState.VoiceMediaType = null;
        AgentSurfaceState.VoiceData = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        if (!AgentSurfaceState.IsPanelOpen)
        {
          await AgentSurfaceState.ReleaseVoiceCaptureAsync(jsRuntime);
        }
      }

      private async Task StopAndTranscribeAsync(CancellationToken cancellationToken)
      {
        AgentSurfaceState.VoicePhase = AskVoicePhase.Transcribing;
        AgentSurfaceState.VoiceStartedAt = null;
        AgentSurfaceState.VoiceError = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        AskVoiceCaptureResult stopped = await AskAiJsModule.StopVoiceCaptureAsync(jsRuntime);
        if (!StillTranscribing(AgentSurfaceState.VoiceEpoch))
        {
          return;
        }

        if (stopped.Error == "too-long")
        {
          RefuseClip(AskVoiceCopy.TooLong);
          return;
        }

        string? data = stopped.Data;
        if (!string.IsNullOrEmpty(stopped.Error) || string.IsNullOrWhiteSpace(data))
        {
          RefuseClip(AskVoiceCopy.Empty);
          return;
        }

        if (data.Length > TranscribeAgentAudio.MaxDataLength)
        {
          RefuseClip(AskVoiceCopy.TooLong);
          return;
        }

        AgentSurfaceState.VoiceMediaType = string.IsNullOrWhiteSpace(stopped.MediaType)
          ? "audio/webm"
          : stopped.MediaType;
        AgentSurfaceState.VoiceData = data;
        await TranscribeStoredAsync(cancellationToken);
      }

      private async Task TranscribeStoredAsync(CancellationToken cancellationToken)
      {
        string? mediaType = AgentSurfaceState.VoiceMediaType;
        string? data = AgentSurfaceState.VoiceData;
        if (string.IsNullOrWhiteSpace(mediaType) || string.IsNullOrWhiteSpace(data))
        {
          AgentSurfaceState.ClearVoice();
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = AskVoiceCopy.Empty;
          return;
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Transcribing;
        AgentSurfaceState.VoiceStartedAt = null;
        AgentSurfaceState.VoiceError = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        int epoch = AgentSurfaceState.VoiceEpoch;
        await AskAiJsModule.SetVoiceInsertEpochAsync(jsRuntime, epoch);
        if (!StillTranscribing(epoch))
        {
          return;
        }

        OneOf<TranscribeAgentAudio.Response, FileResponse, SharedProblemDetails> response;
        try
        {
          response = await api.GetResponse<TranscribeAgentAudio.Response>
          (
            new TranscribeAgentAudio.Command { MediaType = mediaType, Data = data },
            cancellationToken
          );
        }
        catch (Exception)
        {
          if (!StillTranscribing(epoch))
          {
            return;
          }

          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = AskVoiceCopy.Failed;
          return;
        }

        if (!StillTranscribing(epoch))
        {
          return;
        }

        if (response.IsT0)
        {
          AgentSurfaceState.VoiceTranscript = response.AsT0.Text;
          AgentSurfaceState.VoiceData = null;
          AgentSurfaceState.VoiceMediaType = null;
          await InsertStoredAsync();
          return;
        }

        string sentence = response.IsT2 ? FailureText(response.AsT2) : AskVoiceCopy.Failed;
        if (!response.IsT2 || !KeepsClip(response.AsT2.Status))
        {
          AgentSurfaceState.ClearVoice();
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
        AgentSurfaceState.VoiceError = sentence;
      }

      private async Task InsertStoredAsync()
      {
        string? text = AgentSurfaceState.VoiceTranscript;
        if (string.IsNullOrWhiteSpace(text))
        {
          RefuseClip(AskVoiceCopy.InsertFailed);
          return;
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Transcribing;
        AgentSurfaceState.VoiceStartedAt = null;
        AgentSurfaceState.VoiceError = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        int epoch = AgentSurfaceState.VoiceEpoch;
        await AskAiJsModule.SetVoiceInsertEpochAsync(jsRuntime, epoch);
        if (!StillTranscribing(epoch))
        {
          return;
        }

        bool inserted = await AskAiJsModule.InsertComposerTextAsync(jsRuntime, text, epoch);
        if (!StillTranscribing(epoch))
        {
          return;
        }

        if (!inserted)
        {
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = AskVoiceCopy.InsertFailed;
          return;
        }

        AgentSurfaceState.ClearVoice();
      }

      private bool StillTranscribing(int epoch) =>
        AgentSurfaceState.VoicePhase == AskVoicePhase.Transcribing && AgentSurfaceState.VoiceEpoch == epoch;

      /// <summary>Drops the clip. The next press records instead of sending the same bytes.</summary>
      private void RefuseClip(string sentence)
      {
        AgentSurfaceState.ClearVoice();
        AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
        AgentSurfaceState.VoiceError = sentence;
      }

      private static bool KeepsClip(int? status)
      {
        if (status is null)
        {
          return false;
        }

        int code = status.Value;
        return code == 429 || code is >= 500 and <= 599;
      }

      private static string StartError(string error) => error switch
      {
        "denied" => AskVoiceCopy.Denied,
        "unavailable" => AskVoiceCopy.Unavailable,
        "empty" => AskVoiceCopy.Empty,
        _ => AskVoiceCopy.StartFailed
      };

      private static string FailureText(SharedProblemDetails problem)
      {
        if (problem.Status == 503)
        {
          return AskVoiceCopy.NeedsKey;
        }

        if (!string.IsNullOrWhiteSpace(problem.Detail) && problem.Detail.Length <= 160)
        {
          return problem.Detail;
        }

        return AskVoiceCopy.Failed;
      }
    }
  }

  /// <summary>Stops a live capture and forgets the clip. Idle close does not touch JavaScript.</summary>
  internal ValueTask ReleaseVoiceCaptureAsync(IJSRuntime jsRuntime)
  {
    ArgumentNullException.ThrowIfNull(jsRuntime);
    if (VoicePhase == AskVoicePhase.Recording)
    {
      return ReleaseRecordingAsync(jsRuntime);
    }

    bool dropInsert = VoicePhase == AskVoicePhase.Transcribing;
    ClearVoice();
    if (!dropInsert)
    {
      return ValueTask.CompletedTask;
    }

    return PublishVoiceEpochAsync(jsRuntime);
  }

  private async ValueTask PublishVoiceEpochAsync(IJSRuntime jsRuntime)
  {
    await AskAiJsModule.SetVoiceInsertEpochAsync(jsRuntime, VoiceEpoch);
  }

  private async ValueTask ReleaseRecordingAsync(IJSRuntime jsRuntime)
  {
    await AskAiJsModule.CancelVoiceCaptureAsync(jsRuntime);
    ClearVoice();
  }

  private void ClearVoice()
  {
    VoiceEpoch++;
    VoicePhase = AskVoicePhase.Idle;
    VoiceError = null;
    VoiceStartedAt = null;
    VoiceMediaType = null;
    VoiceData = null;
    VoiceTranscript = null;
  }
}
