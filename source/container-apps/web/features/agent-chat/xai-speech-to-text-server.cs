#region Purpose
// Forwards one dictated clip to xAI speech-to-text, or returns the fake transcript in tests.
#endregion

#region Design
// The key stays on the server. BeginKeyResolution opens the same one-read slot as the chat
// relay, IsConfiguredAsync fills it, and TranscribeAsync consumes it. UseFakeUpstream is honored
// only in Development or Testing, and then it wins over a real key and does not dial xAI.
// The request is XaiSpeech.CreateRequest: POST {endpoint}/stt, Bearer, model then file last.
// Error bodies are not read, so a refusal that echoes the key never reaches a log or a problem.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.AgentChats.Application;
using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

/// <summary>xAI speech-to-text, or the deterministic fake transcript.</summary>
internal sealed class XaiSpeechToText : IAgentSpeechToText
{
  internal const string HttpClientName = "xai-speech-to-text";

  private static readonly Action<ILogger, Exception?> LogTransportFailed =
    LoggerMessage.Define
    (
      LogLevel.Error,
      new EventId(1, nameof(LogTransportFailed)),
      "xAI speech-to-text request failed"
    );

  private static readonly AsyncLocal<KeySlot?> OpenTurn = new();

  private readonly IHttpClientFactory HttpClientFactory;
  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly IHostEnvironment Environment;
  private readonly IServiceScopeFactory Scopes;
  private readonly ILogger<XaiSpeechToText> Logger;

  public XaiSpeechToText
  (
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    IServiceScopeFactory scopes,
    ILogger<XaiSpeechToText> logger
  )
  {
    HttpClientFactory = httpClientFactory;
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
  public async Task<OneOf<Response, SharedProblemDetails>> TranscribeAsync
  (
    Command command,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(command);
    XaiChatOptions options = Options.CurrentValue;
    if (AllowFake && options.UseFakeUpstream)
    {
      return new Response(FakeTranscript);
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

    byte[] audio;
    try
    {
      audio = Convert.FromBase64String(command.Data);
    }
    catch (FormatException)
    {
      return XaiSpeech.BadAudio();
    }

    if (audio.Length == 0)
    {
      return XaiSpeech.BadAudio();
    }

    HttpRequestMessage? request = null;
    try
    {
      try
      {
        request = XaiSpeech.CreateRequest(options.Endpoint, apiKey, command.MediaType, audio);
      }
      catch (ArgumentException)
      {
        return XaiSpeech.BadAudio();
      }

      HttpClient client = HttpClientFactory.CreateClient(HttpClientName);
      HttpResponseMessage? responseMessage = null;
      try
      {
        try
        {
          responseMessage = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
          throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
          LogTransportFailed(Logger, exception);
          return XaiSpeech.UpstreamFailed();
        }

        int status = (int)responseMessage.StatusCode;
        string? body = null;
        if (status is >= 200 and <= 299)
        {
          body = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        OneOf<string, SharedProblemDetails> mapped = XaiSpeech.Map(status, body);
        if (mapped.IsT0)
        {
          return new Response(mapped.AsT0);
        }

        return mapped.AsT1;
      }
      finally
      {
        responseMessage?.Dispose();
      }
    }
    finally
    {
      request?.Dispose();
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
}
