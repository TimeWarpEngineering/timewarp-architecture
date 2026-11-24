#region Purpose
// The Ask composer's microphone: record, stop, and insert the transcript. It does not send.
#endregion

#region Design
// One action. Idle or an error starts a dictation. Recording stops it. Task 505 made it a stream:
// start opens the microphone and the dictation socket with AskVoiceStreamOptions (the one copy of
// the path, rates, cap, and finalize time is TranscribeAgentAudio). While recording, ask-ai.ts
// writes the partial transcript into the composer as a preview. Stop asks for the final text and
// CommitComposerText replaces the preview with it. It does not send. There is no stored clip any
// more: an error shows its sentence (AskVoiceCopy.ForCode) and the next press records again; the
// preview text already in the composer stays. Error with a stored transcript (the composer could
// not take it) inserts that text and does not record. A second press while busy is ignored. A
// result that arrives after the phase left Transcribing (Close or New conversation) is ignored.
// The handler does not dispatch; it re-renders before the stop so the button can show
// Transcribing. The key stays on the server. A streaming turn does not start a recording; stop
// stays enabled when one is going.
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
      IJSRuntime jsRuntime
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
            await StopAndInsertAsync();
            return;
          }

          if (AgentSurfaceState.VoicePhase == AskVoicePhase.Error && AgentSurfaceState.VoiceTranscript is not null)
          {
            await InsertStoredAsync();
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
        AgentSurfaceState.ClearVoice();
        int epoch = AgentSurfaceState.VoiceEpoch;
        AskVoiceCaptureResult started = await AskAiJsModule.StartVoiceCaptureAsync
        (
          jsRuntime,
          new AskVoiceStreamOptions { Epoch = epoch }
        );
        if (!AgentSurfaceState.IsPanelOpen || AgentSurfaceState.VoiceEpoch != epoch)
        {
          await AskAiJsModule.CancelVoiceCaptureAsync(jsRuntime);
          if (!AgentSurfaceState.IsPanelOpen)
          {
            AgentSurfaceState.ClearVoice();
          }

          return;
        }

        if (!string.IsNullOrEmpty(started.Error))
        {
          AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
          AgentSurfaceState.VoiceError = StartError(started.Error);
          return;
        }

        AgentSurfaceState.VoicePhase = AskVoicePhase.Recording;
        AgentSurfaceState.VoiceStartedAt = DateTimeOffset.UtcNow;
        AgentSurfaceState.VoiceError = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        if (!AgentSurfaceState.IsPanelOpen)
        {
          await AgentSurfaceState.ReleaseVoiceCaptureAsync(jsRuntime);
        }
      }

      private async Task StopAndInsertAsync()
      {
        AgentSurfaceState.VoicePhase = AskVoicePhase.Transcribing;
        AgentSurfaceState.VoiceStartedAt = null;
        AgentSurfaceState.VoiceError = null;
        subscriptions.ReRenderSubscribers<AgentSurfaceState>();
        int epoch = AgentSurfaceState.VoiceEpoch;
        AskVoiceCaptureResult stopped = await AskAiJsModule.StopVoiceCaptureAsync(jsRuntime);
        if (!StillTranscribing(epoch))
        {
          return;
        }

        if (!string.IsNullOrEmpty(stopped.Error))
        {
          Fail(AskVoiceCopy.ForCode(stopped.Error));
          return;
        }

        if (string.IsNullOrWhiteSpace(stopped.Text))
        {
          Fail(AskVoiceCopy.NoSpeech);
          return;
        }

        AgentSurfaceState.VoiceTranscript = stopped.Text;
        await InsertStoredAsync();
      }

      private async Task InsertStoredAsync()
      {
        string? text = AgentSurfaceState.VoiceTranscript;
        if (string.IsNullOrWhiteSpace(text))
        {
          Fail(AskVoiceCopy.InsertFailed);
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

        bool inserted = await AskAiJsModule.CommitComposerTextAsync(jsRuntime, text, epoch);
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

      /// <summary>Shows the sentence. The next press records again.</summary>
      private void Fail(string sentence)
      {
        AgentSurfaceState.ClearVoice();
        AgentSurfaceState.VoicePhase = AskVoicePhase.Error;
        AgentSurfaceState.VoiceError = sentence;
      }

      private static string StartError(string error) =>
        error == "failed" ? AskVoiceCopy.StartFailed : AskVoiceCopy.ForCode(error);
    }
  }

  /// <summary>Stops a live capture and forgets the transcript. Idle close does not touch JavaScript.</summary>
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
    VoiceTranscript = null;
  }
}
