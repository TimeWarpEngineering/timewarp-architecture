#region Purpose
// Forwards one completion to xAI, or to the in-process fake when tests ask for it.
#endregion

#region Design
// Chat completions only (GetChatClient.AsIChatClient). The Responses API on adapter 10.10.0
// TypeLoadExceptions when a tool call meets OpenAI 2.14. The real client is created on first
// use so startup never dials xAI and a PostConfigure in tests wins over the key.
// UseFakeUpstream is honored only in Development or Testing, and then it wins over a real key
// (XaiFakeChatClient scripts the reply).
// BeginKeyResolution opens a slot on the caller's async flow. IsConfiguredAsync fills that slot
// with the key and the mode, and GetClientAsync consumes the key, so a completion that already
// checked configuration reads the database once. The slot has to be opened by the caller: a value
// stored inside IsConfiguredAsync does not flow back. The mode is a field on that slot, so
// MissingKeyDetail and ResolvedMode read the same resolution. A call that did not open a slot
// resolves on its own, and CompleteAsync uses the mode returned with the client.
// Real clients are rented from XaiChatClientCache and released in CompleteAsync's finally, so a
// client evicted mid-call is disposed only after that call returns. Dispose of this upstream
// disposes the cache and the fake.
// ResolveApiKeyAsync asks IXaiApiKeyResolver when it is registered and otherwise the site key.
// Relay-to-chat mapping is XaiChatMapping. Failures are logged and replaced with
// AgentChatProblems.UpstreamFailed so the client never sees exception text.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.AgentChats.Application;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>xAI chat completions, or the deterministic fake.</summary>
internal sealed class XaiChatUpstream : IAgentChatUpstream, IDisposable
{
  private static readonly Action<ILogger, Exception?> LogUpstreamFailed =
    LoggerMessage.Define
    (
      LogLevel.Error,
      new EventId(1, nameof(LogUpstreamFailed)),
      "xAI chat completion failed"
    );

  private readonly IOptionsMonitor<XaiChatOptions> Options;
  private readonly IHostEnvironment Environment;
  private readonly IServiceScopeFactory Scopes;
  private readonly ILogger<XaiChatUpstream> Logger;
  /// <summary>Live chat clients kept per key. Older keys are evicted past this count.</summary>
  internal const int MaxCachedClients = XaiChatClientCache.MaxCachedClients;

  private static readonly AsyncLocal<KeySlot?> OpenTurn = new();
  private readonly XaiFakeChatClient Fake = new();
  private readonly XaiChatClientCache Cache;

  public XaiChatUpstream
  (
    IOptionsMonitor<XaiChatOptions> options,
    IHostEnvironment environment,
    IServiceScopeFactory scopes,
    IXaiChatClientFactory clients,
    ILogger<XaiChatUpstream> logger
  )
  {
    Options = options;
    Environment = environment;
    Scopes = scopes;
    Cache = new XaiChatClientCache(clients);
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
  public string? Model
  {
    get
    {
      XaiChatOptions options = Options.CurrentValue;
      bool hostOrFake = (AllowFake && options.UseFakeUpstream) || !string.IsNullOrWhiteSpace(options.ApiKey);
      if (!hostOrFake)
      {
        return null;
      }

      return string.IsNullOrWhiteSpace(options.Model) ? XaiChatDefaults.DefaultModel : options.Model;
    }
  }

  /// <inheritdoc />
  public async Task<OneOf<Response, SharedProblemDetails>> CompleteAsync
  (
    Command command,
    CancellationToken cancellationToken
  )
  {
    XaiChatOptions options = Options.CurrentValue;
    IChatClient? client;
    XaiByokMode mode;
    try
    {
      (client, mode) = await GetClientAsync(options, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
      LogUpstreamFailed(Logger, exception);
      return AgentChatProblems.UpstreamFailed();
    }

    if (client is null)
    {
      return AgentChatProblems.NotConfigured(
        XaiByokCopy.MissingKeyDetail(mode, Environment.IsDevelopment()));
    }

    try
    {
      ChatOptions chatOptions;
      List<ChatMessage> messages;
      try
      {
        chatOptions = XaiChatMapping.ToChatOptions(command);
        messages = XaiChatMapping.ToMessages(command);
      }
      catch (JsonException exception)
      {
        LogUpstreamFailed(Logger, exception);
        return new SharedProblemDetails
        {
          Title = "Invalid tool schema",
          Status = 400,
          Detail = "A tool schema or argument payload was not valid JSON."
        };
      }

      ChatResponse chatResponse;
      try
      {
        chatResponse = await client.GetResponseAsync(messages, chatOptions, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is not OperationCanceledException)
      {
        LogUpstreamFailed(Logger, exception);
        return AgentChatProblems.UpstreamFailed();
      }

      var mapped = XaiChatMapping.ToResponse(chatResponse);
      if (string.IsNullOrWhiteSpace(mapped.Text) && mapped.ToolCalls.Count == 0)
      {
        return AgentChatProblems.EmptyModelResponse();
      }

      return mapped;
    }
    finally
    {
      Release(client);
    }
  }

  /// <summary>Disposes every cached client. An in-flight completion may already have finished.</summary>
  public void Dispose()
  {
    Cache.Dispose();
    Fake.Dispose();
  }

  /// <inheritdoc />
  public XaiByokMode ResolvedMode => OpenTurn.Value?.Mode ?? XaiByokMode.Allow;

  /// <inheritdoc />
  public string MissingKeyDetail(bool development) =>
    XaiByokCopy.MissingKeyDetail(ResolvedMode, development);

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

  private async Task<(IChatClient? Client, XaiByokMode Mode)> GetClientAsync(
    XaiChatOptions options,
    CancellationToken cancellationToken)
  {
    if (AllowFake && options.UseFakeUpstream)
    {
      return (Fake, OpenTurn.Value?.Mode ?? XaiByokMode.Allow);
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
      return (null, mode);
    }

    string endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
      ? XaiChatDefaults.DefaultEndpoint
      : options.Endpoint;
    string model = string.IsNullOrWhiteSpace(options.Model)
      ? XaiChatDefaults.DefaultModel
      : options.Model;

    return (Cache.Rent(apiKey, endpoint, model), mode);
  }

  private void Release(IChatClient client)
  {
    if (ReferenceEquals(client, Fake))
    {
      return;
    }

    Cache.Release(client);
  }

  /// <summary>Key and mode from one resolution. The mode travels with the return value.</summary>
  private readonly record struct ResolvedKey(string? ApiKey, XaiByokMode Mode);

  /// <summary>One turn's resolved key. The caller opens it; <see cref="IsConfiguredAsync"/> fills it.</summary>
  private sealed class KeySlot
  {
    public bool Filled { get; set; }

    public string? Key { get; set; }

    public XaiByokMode Mode { get; set; }
  }
}
