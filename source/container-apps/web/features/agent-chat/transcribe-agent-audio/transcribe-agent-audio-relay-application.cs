#region Purpose
// Runs one Ask dictation: admits the caller, opens the speech upstream, relays audio up and the
// transcript back, and ends with the final text or one error code.
#endregion

#region Design
// The endpoint policy already requires a principal. The relay checks again so a miswired endpoint
// is not an open proxy to the key. A missing key sends needs-key and does not dial the provider.
// BeginKeyResolution runs before that check so the open reuses the same read. Admission is the
// chat relay's cap and the lease is held for the whole dictation (two at once per principal, so a
// chat turn can still run while one person records).
// Two loops share one cancellation. The audio pump is the only writer to the upstream: binary
// frames go up until the browser sends Done, then Complete starts the finalize timer. Audio past
// TranscribeAgentAudio.MaxAudioBytes is dropped and the stream is completed, so a browser that
// ignores the cap cannot spend the key for longer. The event loop is the only writer to the
// browser: partial carries the whole text so far, done carries the final text. If the final
// events do not arrive within FinalizeSeconds the browser gets timed-out. An upstream close after
// Complete is the end of the transcript. An upstream close or error before it is failed. An
// empty final transcript is no-speech. A browser that closes its socket cancels both loops and
// gets nothing. No message carries an upstream body or the key. Failures that end without a
// transcript are logged with a fixed reason only (no key, audio, or text), so a stalled or refused
// upstream shows in the web-server log the way task 505's timeout did.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

/// <summary>What the browser side of a dictation delivered.</summary>
public enum DictationFrameKind
{
  /// <summary>Binary audio; Count bytes are in the buffer.</summary>
  Audio = 0,

  /// <summary>The person stopped.</summary>
  Done = 1,

  /// <summary>The browser closed the socket or sent something else.</summary>
  Closed = 2
}

/// <summary>One frame from the browser.</summary>
public readonly record struct DictationFrame(DictationFrameKind Kind, int Count);

/// <summary>The browser side of a dictation. The server wraps the accepted WebSocket.</summary>
public interface IDictationChannel
{
  /// <summary>Reads the next frame into <paramref name="buffer"/>.</summary>
  ValueTask<DictationFrame> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

  /// <summary>Sends one JSON text message.</summary>
  ValueTask SendAsync(string json, CancellationToken cancellationToken);
}

/// <summary>Relays one dictation between the browser and <see cref="IAgentSpeechToText"/>.</summary>
public sealed class DictationRelay
{
  private const int FrameBytes = 64 * 1024;

  private static readonly Action<ILogger, string, Exception?> LogDictationFailed =
    LoggerMessage.Define<string>
    (
      LogLevel.Warning,
      new EventId(1, nameof(LogDictationFailed)),
      "Dictation ended without a transcript: {Reason}"
    );

  private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
  private readonly IAgentSpeechToText Speech;
  private readonly AgentChatAdmission Admission;
  private readonly ILogger<DictationRelay>? Logger;

  /// <summary>Creates the relay. The logger is optional; it never receives the key, audio, or text.</summary>
  public DictationRelay
  (
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IAgentSpeechToText speech,
    AgentChatAdmission admission,
    ILogger<DictationRelay>? logger = null
  )
  {
    CurrentPrincipalAccessor = currentPrincipalAccessor;
    Speech = speech;
    Admission = admission;
    Logger = logger;
  }

  /// <summary>How long to wait for the final transcript after Done. Tests shorten it.</summary>
  public TimeSpan FinalizeTimeout { get; init; } = TimeSpan.FromSeconds(FinalizeSeconds);

  /// <summary>Runs the dictation until done, an error, or the browser leaves.</summary>
  public async Task RunAsync(IDictationChannel channel, int sampleRate, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(channel);
    PrincipalId? principalId = await CurrentPrincipalAccessor
      .GetCurrentPrincipalIdAsync(cancellationToken)
      .ConfigureAwait(false);
    if (principalId is null)
    {
      await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.Unauthenticated), cancellationToken)
        .ConfigureAwait(false);
      return;
    }

    if (!IsSupportedSampleRate(sampleRate))
    {
      await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.BadRequest), cancellationToken)
        .ConfigureAwait(false);
      return;
    }

    Speech.BeginKeyResolution();
    if (!await Speech.IsConfiguredAsync(cancellationToken).ConfigureAwait(false))
    {
      await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.NeedsKey), cancellationToken)
        .ConfigureAwait(false);
      return;
    }

    using AgentChatAdmission.AdmissionLease lease = await Admission
      .TryAdmitAsync(principalId.Value.Value.ToString("D"), cancellationToken)
      .ConfigureAwait(false);
    if (!lease.Admitted)
    {
      await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.Busy), cancellationToken)
        .ConfigureAwait(false);
      return;
    }

    OneOf<IAgentSpeechSession, SharedProblemDetails> opened = await Speech
      .OpenAsync(sampleRate, cancellationToken)
      .ConfigureAwait(false);
    if (opened.IsT1)
    {
      Log("upstream did not open");
      await SendQuietlyAsync(channel, DictationMessages.Error(CodeFor(opened.AsT1)), cancellationToken)
        .ConfigureAwait(false);
      return;
    }

    await using IAgentSpeechSession stream = opened.AsT0;
    if (!await SendQuietlyAsync(channel, DictationMessages.Ready, cancellationToken).ConfigureAwait(false))
    {
      return;
    }

    await RelayAsync(channel, stream, sampleRate, cancellationToken).ConfigureAwait(false);
  }

  private async Task RelayAsync
  (
    IDictationChannel channel,
    IAgentSpeechSession stream,
    int sampleRate,
    CancellationToken cancellationToken
  )
  {
    var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    CancellationTokenSource finalize = new();
    Task pump = Task.CompletedTask;
    try
    {
      PumpState state = new()
      {
        Token = session.Token,
        CancelSession = session.CancelAsync,
        StartFinalizeTimer = () => finalize.CancelAfter(FinalizeTimeout)
      };
      pump = PumpAudioAsync(channel, stream, sampleRate, state);
      await RelayEventsAsync(channel, stream, state, finalize.Token, session.Token).ConfigureAwait(false);
    }
    finally
    {
      await session.CancelAsync().ConfigureAwait(false);
      try
      {
        await pump.ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
      }

      finalize.Dispose();
      session.Dispose();
    }
  }

  private static async Task PumpAudioAsync
  (
    IDictationChannel channel,
    IAgentSpeechSession stream,
    int sampleRate,
    PumpState state
  )
  {
    CancellationToken token = state.Token;
    byte[] buffer = ArrayPool<byte>.Shared.Rent(FrameBytes);
    long limit = MaxAudioBytes(sampleRate);
    long sent = 0;
    try
    {
      while (!token.IsCancellationRequested)
      {
        DictationFrame frame;
        try
        {
          frame = await channel.ReceiveAsync(buffer, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
          return;
        }
        catch (Exception exception) when (IsTransport(exception))
        {
          frame = new DictationFrame(DictationFrameKind.Closed, 0);
        }

        if (frame.Kind == DictationFrameKind.Closed)
        {
          await state.CancelSession().ConfigureAwait(false);
          return;
        }

        if (state.Completed)
        {
          continue;
        }

        if (frame.Kind == DictationFrameKind.Audio && frame.Count <= 0)
        {
          continue;
        }

        if (frame.Kind == DictationFrameKind.Audio && sent + frame.Count <= limit)
        {
          sent += frame.Count;
          if (!await TryUpstreamAsync(() => stream.SendAudioAsync(buffer.AsMemory(0, frame.Count), token))
            .ConfigureAwait(false))
          {
            return;
          }

          continue;
        }

        state.Completed = true;
        state.StartFinalizeTimer();
        if (!await TryUpstreamAsync(() => stream.CompleteAsync(token)).ConfigureAwait(false))
        {
          return;
        }
      }
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  private static async Task<bool> TryUpstreamAsync(Func<ValueTask> send)
  {
    try
    {
      await send().ConfigureAwait(false);
      return true;
    }
    catch (OperationCanceledException)
    {
      return false;
    }
    catch (Exception exception) when (IsTransport(exception))
    {
      // The upstream is gone. The event loop sees the close and reports it.
      return false;
    }
  }

  private async Task RelayEventsAsync
  (
    IDictationChannel channel,
    IAgentSpeechSession stream,
    PumpState state,
    CancellationToken finalize,
    CancellationToken token
  )
  {
    DictationTranscript transcript = new();
    using var events = CancellationTokenSource.CreateLinkedTokenSource(token, finalize);
    while (true)
    {
      SpeechEvent? next;
      try
      {
        next = await stream.ReceiveAsync(events.Token).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (finalize.IsCancellationRequested && !token.IsCancellationRequested)
      {
        Log(ErrorCodes.TimedOut);
        await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.TimedOut), token).ConfigureAwait(false);
        return;
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (Exception exception) when (IsTransport(exception))
      {
        next = state.Completed ? null : new SpeechEvent(SpeechEventKind.Error, string.Empty);
      }

      if (next is null)
      {
        if (state.Completed)
        {
          await FinishAsync(channel, transcript, token).ConfigureAwait(false);
        }
        else
        {
          Log("upstream closed before done");
          await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.Failed), token).ConfigureAwait(false);
        }

        return;
      }

      SpeechEvent speechEvent = next.Value;
      if (speechEvent.Kind == SpeechEventKind.Error)
      {
        Log("upstream error event");
        await SendQuietlyAsync(channel, DictationMessages.Error(ErrorCodes.Failed), token).ConfigureAwait(false);
        return;
      }

      bool changed = transcript.Apply(speechEvent);
      if (speechEvent.Kind == SpeechEventKind.Done)
      {
        await FinishAsync(channel, transcript, token).ConfigureAwait(false);
        return;
      }

      if (changed && !await SendQuietlyAsync(channel, DictationMessages.Partial(transcript.Text), token)
        .ConfigureAwait(false))
      {
        return;
      }
    }
  }

  private void Log(string reason)
  {
    if (Logger is not null)
    {
      LogDictationFailed(Logger, reason, null);
    }
  }

  private static async Task FinishAsync(IDictationChannel channel, DictationTranscript transcript, CancellationToken token)
  {
    string text = transcript.Text;
    string message = text.Length == 0
      ? DictationMessages.Error(ErrorCodes.NoSpeech)
      : DictationMessages.Done(text);
    await SendQuietlyAsync(channel, message, token).ConfigureAwait(false);
  }

  private static async Task<bool> SendQuietlyAsync(IDictationChannel channel, string json, CancellationToken token)
  {
    try
    {
      await channel.SendAsync(json, token).ConfigureAwait(false);
      return true;
    }
    catch (OperationCanceledException)
    {
      return false;
    }
    catch (Exception exception) when (IsTransport(exception))
    {
      // The browser left. Nothing else can be told.
      return false;
    }
  }

  private static string CodeFor(SharedProblemDetails problem) => problem.Status switch
  {
    503 => ErrorCodes.NeedsKey,
    429 => ErrorCodes.Busy,
    400 => ErrorCodes.BadRequest,
    _ => ErrorCodes.Failed
  };

  private static bool IsTransport(Exception exception) =>
    exception is WebSocketException or IOException or ObjectDisposedException or InvalidOperationException;

  private sealed class PumpState
  {
    private int CompletedFlag;

    public required CancellationToken Token { get; init; }

    public required Func<Task> CancelSession { get; init; }

    public required Action StartFinalizeTimer { get; init; }

    public bool Completed
    {
      get => Volatile.Read(ref CompletedFlag) == 1;
      set => Volatile.Write(ref CompletedFlag, value ? 1 : 0);
    }
  }
}

/// <summary>JSON the relay sends to the browser.</summary>
public static class DictationMessages
{
  /// <summary>The upstream is ready.</summary>
  public static string Ready { get; } = Write(MessageTypes.Ready, null, null);

  /// <summary>The whole transcript so far.</summary>
  public static string Partial(string text) => Write(MessageTypes.Partial, "text", text);

  /// <summary>The final transcript.</summary>
  public static string Done(string text) => Write(MessageTypes.Done, "text", text);

  /// <summary>One of <see cref="ErrorCodes"/>.</summary>
  public static string Error(string code) => Write(MessageTypes.Error, "code", code);

  private static string Write(string type, string? name, string? value)
  {
    ArrayBufferWriter<byte> output = new();
    using (Utf8JsonWriter writer = new(output))
    {
      writer.WriteStartObject();
      writer.WriteString("type", type);
      if (name is not null)
      {
        writer.WriteString(name, value);
      }

      writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(output.WrittenSpan);
  }
}
