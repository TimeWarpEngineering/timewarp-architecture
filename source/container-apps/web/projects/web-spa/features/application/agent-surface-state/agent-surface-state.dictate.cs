#region Purpose
// The Ask composer's microphone: record, stop, and insert the transcript. It does not send.
#endregion

#region Design
// One action. Idle or a microphone failure starts capture. Recording stops it and sends the clip.
// Error with a stored clip sends that clip again, so a refused request does not re-open the
// microphone. Transcribing ignores a second press. The handler does not dispatch. It re-renders
// before the speech request so the button can show Transcribing while the handler is still open,
// the same way StartPick shows Picking. The key stays on the server: the browser posts base64 to
// TranscribeAgentAudio and never sees XAI:ApiKey. InsertComposerText places the transcript at the
// caret and focuses the composer. A turn that is streaming does not start a recording; the
// component keeps the stop control enabled when a recording is already going.
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
        AskVoiceCaptureResult started = await AskAiJsModule.StartVoiceCaptureAsync(jsRuntime);
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
        if (!string.IsNullOrEmpty(stopped.Error) || string.IsNullOrWhiteSpace(stopped.Data))
        {
          AgentSurfaceState.ClearVoice();
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = AskVoiceCopy.Empty;
          return;
        }

        AgentSurfaceState.VoiceMediaType = string.IsNullOrWhiteSpace(stopped.MediaType)
          ? "audio/webm"
          : stopped.MediaType;
        AgentSurfaceState.VoiceData = stopped.Data;
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
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = AskVoiceCopy.Failed;
          return;
        }

        if (response.IsT0)
        {
          bool inserted = await AskAiJsModule.InsertComposerTextAsync(jsRuntime, response.AsT0.Text);
          if (!inserted)
          {
            AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
            AgentSurfaceState.VoiceError = AskVoiceCopy.InsertFailed;
            return;
          }

          AgentSurfaceState.ClearVoice();
          return;
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
        AgentSurfaceState.VoiceError = response.IsT2
          ? FailureText(response.AsT2)
          : AskVoiceCopy.Failed;
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
    if (VoicePhase != AskVoicePhase.Recording)
    {
      ClearVoice();
      return ValueTask.CompletedTask;
    }

    return ReleaseRecordingAsync(jsRuntime);
  }

  private async ValueTask ReleaseRecordingAsync(IJSRuntime jsRuntime)
  {
    await AskAiJsModule.CancelVoiceCaptureAsync(jsRuntime);
    ClearVoice();
  }

  private void ClearVoice()
  {
    VoicePhase = AskVoicePhase.Idle;
    VoiceError = null;
    VoiceStartedAt = null;
    VoiceMediaType = null;
    VoiceData = null;
  }
}
