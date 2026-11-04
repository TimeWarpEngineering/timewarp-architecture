#region Purpose
// Shared builders and test doubles for the xAI chat upstream test classes: service provider factory,
// turn builders, a switching principal, capturing / tracking chat client factories and an in-memory secret store.
#endregion

namespace XaiChatUpstream_;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;
using TimeWarp.Architecture.Secrets;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

internal static class XaiChatUpstreamFixture
{
  public static Turn SiteMapCall() => new()
  {
    Role = "assistant",
    ToolCalls = [new ToolCall { Name = "site_map", CallId = "call-site-map", ArgumentsJson = "{}" }]
  };

  public static Turn SiteMapResult() => new()
  {
    Role = "tool",
    ToolCallId = "call-site-map",
    ToolResult = """
      {"destinations":[
        {"title":"Counter","url":"/Counter","group":"","icon":"x","description":"Count things."},
        {"title":"Settings","url":"/Settings","group":"","icon":"x","description":"Manage passkeys and linked accounts."}
      ]}
      """
  };

  public static Command UserTurn(string text, params string[] toolNames) => new()
  {
    Messages = [new Turn { Role = "user", Text = text }],
    Tools = toolNames
      .Select(name => new ToolDefinition { Name = name, ParametersJson = """{"type":"object","properties":{}}""" })
      .ToList()
  };

  public static async Task StoreUserKeyAsync(MemoryProtectedSecretStore store, Guid userId, string apiKey) =>
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userId),
      UserXaiApiKeyNames.PurposeFor(userId),
      apiKey,
      userId,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);

  public static async Task<TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response> Configuration(
    ServiceProvider provider,
    ICurrentPrincipalAccessor principal)
  {
    TimeWarp.Architecture.Features.AgentChats.Application.GetAgentChatConfiguration.Handler handler = new(
      principal,
      provider.GetRequiredService<IAgentChatUpstream>(),
      provider.GetRequiredService<IOptions<XaiChatOptions>>(),
      provider.GetRequiredService<IHostEnvironment>(),
      provider.GetRequiredService<ISiteXaiApiKey>());
    OneOf<TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response, SharedProblemDetails> result =
      await handler.Handle(
        new TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Query(),
        CancellationToken.None);
    result.IsT0.ShouldBeTrue();
    return result.AsT0;
  }

  public static Command UserTurn() => new()
  {
    Messages = [new Turn { Role = "user", Text = "What is on this page?" }]
  };

  public static ServiceProvider Build(
    string environmentName,
    bool useFake,
    string? apiKey,
    IProtectedSecretStore? store = null,
    IXaiChatClientFactory? clients = null,
    ISiteSettingsStore? settings = null,
    ICurrentPrincipalAccessor? principal = null)
  {
    Dictionary<string, string?> values = new()
    {
      ["XAI:UseFakeUpstream"] = useFake ? "true" : "false"
    };
    if (apiKey is not null)
    {
      values["XAI:ApiKey"] = apiKey;
    }

    IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    ServiceCollection services = new();
    services.AddLogging();
    services.AddSingleton<IHostEnvironment>(new NamedEnvironment(environmentName));
    if (store is not null)
    {
      services.AddSingleton(store);
    }

    if (clients is not null)
    {
      services.AddSingleton(clients);
    }

    if (settings is not null)
    {
      services.AddSingleton(settings);
    }

    if (principal is not null)
    {
      services.AddSingleton(principal);
    }

    XaiChatRegistration.ConfigureServices(services, configuration);
    return services.BuildServiceProvider();
  }

  internal sealed class SwitchingPrincipal : ICurrentPrincipalAccessor
  {
    public PrincipalId? Id { get; set; }

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult(Id);
  }

  internal sealed class CapturingChatClientFactory : IXaiChatClientFactory
  {
    public List<string> Keys { get; } = [];

    public IChatClient Create(string apiKey, string endpoint, string model)
    {
      _ = endpoint;
      _ = model;
      return new StubChatClient(apiKey, Keys);
    }
  }

  internal sealed class StubChatClient : IChatClient
  {
    private readonly string ApiKey;
    private readonly List<string> Keys;

    public StubChatClient(string apiKey, List<string> keys)
    {
      ApiKey = apiKey;
      Keys = keys;
    }

    public Task<ChatResponse> GetResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default)
    {
      _ = messages;
      _ = options;
      _ = cancellationToken;
      Keys.Add(ApiKey);
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
  }

  internal sealed class TrackingChatClientFactory : IXaiChatClientFactory
  {
    private readonly List<TrackingChatClient> Clients = [];

    public int Created => Clients.Count;

    public int Live => Clients.Count(client => !client.Disposed);

    public IChatClient Create(string apiKey, string endpoint, string model)
    {
      _ = apiKey;
      _ = endpoint;
      _ = model;
      TrackingChatClient client = new();
      Clients.Add(client);
      return client;
    }
  }

  internal sealed class TrackingChatClient : IChatClient
  {
    public bool Disposed { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default)
    {
      _ = messages;
      _ = options;
      _ = cancellationToken;
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default) =>
      throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => Disposed = true;
  }

  internal sealed class MemoryProtectedSecretStore : IProtectedSecretStore
  {
    private readonly Dictionary<string, (string Plaintext, ProtectedSecretRecord Record)> Rows = [];

    public bool StorageAvailable => true;

    public int UnprotectCalls { get; private set; }

    public Task<ProtectedSecretRecord?> FindAsync(string name, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
        ? row.Record
        : null);
    }

    public Task<ProtectedSecretWrite> SetAsync(
      string name,
      string purpose,
      string plaintext,
      Guid setByPrincipalId,
      DateTimeOffset setAt,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      bool inserted = !Rows.ContainsKey(name);
      Rows[name] = (plaintext, new ProtectedSecretRecord(
        name,
        purpose,
        ProtectedSecretSuffix.From(plaintext),
        setAt,
        setByPrincipalId));
      return Task.FromResult(inserted ? ProtectedSecretWrite.Inserted : ProtectedSecretWrite.Replaced);
    }

    public Task<string?> UnprotectAsync(string name, string purpose, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      UnprotectCalls++;
      if (!Rows.TryGetValue(name, out (string Plaintext, ProtectedSecretRecord Record) row)
        || !string.Equals(row.Record.Purpose, purpose, StringComparison.Ordinal))
      {
        return Task.FromResult<string?>(null);
      }

      return Task.FromResult<string?>(row.Plaintext);
    }

    public Task<bool> ClearAsync(string name, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult(Rows.Remove(name));
    }
  }

  internal sealed class NamedEnvironment : IHostEnvironment
  {
    public NamedEnvironment(string environmentName)
    {
      EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }

    public string ApplicationName { get; set; } = "tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }
}
