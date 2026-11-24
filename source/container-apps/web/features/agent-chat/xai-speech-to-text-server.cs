#region Purpose
// Opens xAI streaming speech-to-text for one dictation, or the fake stream in tests.
#endregion

#region Design
// The key stays on the server. BeginKeyResolution opens the same one-read slot as the chat
// relay, IsConfiguredAsync fills it, and OpenAsync consumes it. UseFakeUpstream is honored only
// in Development or Testing, and then it wins over a real key and does not dial xAI.
// The upstream is a ClientWebSocket to XaiSpeech.StreamUri with Authorization: Bearer. OpenAsync
// waits for transcript.created (the docs say audio sent before it is not ready to be taken) under
// ConnectTimeout. Measured 0.75-0.95 s from connect to created on 2026-11-24, so 10 s is a
// stall. A failed or timed-out open is logged without the exception message (a refused handshake
// could carry the request) and returns a generic 502.
// No HttpClient: the batch POST and its named client are gone (task 505). That client inherited
// the Aspire standard resilience handler (10 s per attempt, retries), which cut real clips at 10 s
// and resent the POST until the 30 s HttpClient timeout. A WebSocket does not go through it.
// The fake stream answers the first audio frame with an interim "Voice", and Complete with the
// utterance FakeTranscript followed by done, so browser proofs see a partial then the final text.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats.Application;

/// <summary>xAI streaming speech-to-text, or the deterministic fake stream.</summary>
internal sealed class XaiSpeechToText : IAgentSpeechToText
{
  internal static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

  private static readonly Action<ILogger, string, Exception?> LogOpenFailed =
    LoggerMessage.Define<string>
    (
      LogLevel.Error,
      new EventId(1, nameof(LogOpenFailed)),
      "xAI speech-to-text stream did not open: {Reason}"
    );

  private static readonly AsyncLocal<KeySlot?> OpenTurn = new();

  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly IHostEnvironment Environment;
  private readonly IServiceScopeFactory Scopes;
  private readonly ILogger<XaiSpeechToText> Logger;

  public XaiSpeechToText
  (
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    IServiceScopeFactory scopes,
    ILogger<XaiSpeechToText> logger
  )
  {
    Options = options;
    Environment = environment;
    Scopes = scopes;
    Logger = logger;
  }

  private bool AllowFake =>
    Environment.IsDevelopment() || Environment.IsEnvironment("Testing");

  /// <inheritdoc />
  public void BeginKeyResolution()
  {
    OpenTurn.Value = new KeySlot();
  }

  /// <inheritdoc />
  public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
  {
    XaiChatOptions options = Options.CurrentValue;
    if (AllowFake && options.UseFakeUpstream)
    {
      return true;
    }

    ResolvedKey resolved = await ResolveApiKeyAsync(cancellationToken).ConfigureAwait(false);
    if (OpenTurn.Value is { } slot)
    {
      slot.Key = resolved.ApiKey;
      slot.Mode = resolved.Mode;
      slot.Filled = true;
    }

    return !string.IsNullOrWhiteSpace(resolved.ApiKey);
  }

  /// <inheritdoc />
  public string MissingKeyDetail(bool development) =>
    XaiByokCopy.MissingKeyDetail(OpenTurn.Value?.Mode ?? XaiByokMode.Allow, development);

  /// <inheritdoc />
  public async Task<OneOf<IAgentSpeechSession, SharedProblemDetails>> OpenAsync
  (
    int sampleRate,
    CancellationToken cancellationToken
  )
  {
    XaiChatOptions options = Options.CurrentValue;
    if (AllowFake && options.UseFakeUpstream)
    {
      return new FakeSpeechSession();
    }

    string? apiKey;
    XaiByokMode mode;
    if (OpenTurn.Value is { Filled: true } slot)
    {
      apiKey = slot.Key;
      mode = slot.Mode;
      OpenTurn.Value = null;
    }
    else
    {
      ResolvedKey resolved = await ResolveApiKeyAsync(cancellationToken).ConfigureAwait(false);
      apiKey = resolved.ApiKey;
      mode = resolved.Mode;
    }

    if (string.IsNullOrWhiteSpace(apiKey))
    {
      return AgentChatProblems.NotConfigured(XaiByokCopy.MissingKeyDetail(mode, Environment.IsDevelopment()));
    }

    Uri uri = XaiSpeech.StreamUri(options.Endpoint, sampleRate);
    ClientWebSocket? socket = new();
    try
    {
      socket.Options.SetRequestHeader("Authorization", "Bearer " + apiKey);
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(ConnectTimeout);
      try
      {
        await socket.ConnectAsync(uri, timeout.Token).ConfigureAwait(false);
        string? first = await XaiSpeechSession.ReceiveTextAsync(socket, timeout.Token).ConfigureAwait(false);
        if (first is null || !XaiSpeech.IsCreated(first))
        {
          LogOpenFailed(Logger, "no transcript.created", null);
          return AgentChatProblems.UpstreamFailed();
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (OperationCanceledException)
      {
        LogOpenFailed(Logger, "timed out", null);
        return AgentChatProblems.UpstreamFailed();
      }
      catch (WebSocketException exception)
      {
        LogOpenFailed(Logger, exception.WebSocketErrorCode.ToString(), null);
        return AgentChatProblems.UpstreamFailed();
      }

      XaiSpeechSession opened = new(socket);
      socket = null;
      return opened;
    }
    finally
    {
      socket?.Dispose();
    }
  }

  private async Task<ResolvedKey> ResolveApiKeyAsync(CancellationToken cancellationToken)
  {
    await using AsyncServiceScope scope = Scopes.CreateAsyncScope();
    IXaiApiKeyResolver? resolver = scope.ServiceProvider.GetService<IXaiApiKeyResolver>();
    if (resolver is not null)
    {
      ResolvedXaiApiKey resolved = await resolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
      return new ResolvedKey(resolved.ApiKey, resolved.Mode);
    }

    ISiteXaiApiKey keys = scope.ServiceProvider.GetRequiredService<ISiteXaiApiKey>();
    string? apiKey = await keys.ResolveApiKeyAsync(cancellationToken).ConfigureAwait(false);
    return new ResolvedKey(apiKey, XaiByokMode.Allow);
  }

  private readonly record struct ResolvedKey(string? ApiKey, XaiByokMode Mode);

  private sealed class KeySlot
  {
    public bool Filled { get; set; }

    public string? Key { get; set; }

    public XaiByokMode Mode { get; set; }
  }

  /// <summary>The Development/Testing stream: one interim on audio, the fake transcript on complete.</summary>
  private sealed class FakeSpeechSession : IAgentSpeechSession
  {
    private readonly Channel<SpeechEvent> Events = Channel.CreateUnbounded<SpeechEvent>();
    private bool SentInterim;

    public ValueTask SendAudioAsync(ReadOnlyMemory<byte> audio, CancellationToken cancellationToken)
    {
      if (!SentInterim)
      {
        SentInterim = true;
        Events.Writer.TryWrite(new SpeechEvent(SpeechEventKind.Interim, "Voice"));
      }

      return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
      Events.Writer.TryWrite
      (
        new SpeechEvent(SpeechEventKind.UtteranceFinal, TranscribeAgentAudio.FakeTranscript)
      );
      Events.Writer.TryWrite(new SpeechEvent(SpeechEventKind.Done, string.Empty));
      Events.Writer.TryComplete();
      return ValueTask.CompletedTask;
    }

    public async ValueTask<SpeechEvent?> ReceiveAsync(CancellationToken cancellationToken)
    {
      if (await Events.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)
        && Events.Reader.TryRead(out SpeechEvent speechEvent))
      {
        return speechEvent;
      }

      return null;
    }

    public ValueTask DisposeAsync()
    {
      Events.Writer.TryComplete();
      return ValueTask.CompletedTask;
    }
  }
}

/// <summary>One open xAI transcription socket.</summary>
internal sealed class XaiSpeechSession : IAgentSpeechSession
{
  private const int MaxMessageBytes = 1024 * 1024;

  private readonly ClientWebSocket Socket;

  internal XaiSpeechSession(ClientWebSocket socket)
  {
    Socket = socket;
  }

  public ValueTask SendAudioAsync(ReadOnlyMemory<byte> audio, CancellationToken cancellationToken) =>
    Socket.SendAsync(audio, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);

  public ValueTask CompleteAsync(CancellationToken cancellationToken) =>
    Socket.SendAsync
    (
      Encoding.UTF8.GetBytes(XaiSpeech.AudioDoneMessage).AsMemory(),
      WebSocketMessageType.Text,
      endOfMessage: true,
      cancellationToken
    );

  public async ValueTask<SpeechEvent?> ReceiveAsync(CancellationToken cancellationToken)
  {
    while (true)
    {
      string? text = await ReceiveTextAsync(Socket, cancellationToken).ConfigureAwait(false);
      if (text is null)
      {
        return null;
      }

      SpeechEvent speechEvent = XaiSpeech.Parse(text);
      if (speechEvent.Kind != SpeechEventKind.Ignored)
      {
        return speechEvent;
      }
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (Socket.State == WebSocketState.Open)
    {
      using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
      try
      {
        await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
      {
        // Closing a socket that is already going away.
      }
    }

    Socket.Dispose();
  }

  /// <summary>The next text message, or null on close. Binary frames are skipped.</summary>
  internal static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
  {
    ArrayBufferWriter<byte> message = new();
    while (true)
    {
      Memory<byte> target = message.GetMemory(16 * 1024);
      ValueWebSocketReceiveResult result = await socket.ReceiveAsync(target, cancellationToken).ConfigureAwait(false);
      if (result.MessageType == WebSocketMessageType.Close)
      {
        return null;
      }

      message.Advance(result.Count);
      if (message.WrittenCount > MaxMessageBytes)
      {
        return null;
      }

      if (!result.EndOfMessage)
      {
        continue;
      }

      if (result.MessageType == WebSocketMessageType.Text)
      {
        return Encoding.UTF8.GetString(message.WrittenSpan);
      }

      message.ResetWrittenCount();
    }
  }
}
