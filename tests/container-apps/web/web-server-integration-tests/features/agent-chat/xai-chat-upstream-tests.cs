#region Purpose
// The server relay's fake upstream and declaration-only tools. No network and no API key.
#endregion

#region Design
// UseFakeUpstream is honored in Development and ignored in Production. The fake returns a
// page_context function call and, on the next turn, text that includes the tool result. When
// site_map or navigate is offered it also scripts "where" -> site_map, a site_map result ->
// a markdown link answer, and "take me there" -> navigate. Tool declarations are
// AIFunctionDeclaration, not AIFunction, so the server has no InvokeAsync.
#endregion

namespace XaiChatUpstream_;

using System.Text.Json;
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

public class XaiChatUpstream_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<XaiChatUpstream_Given_>();

  public static async Task Development_Fake_Returns_Page_Context_Then_The_Result()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: "should-not-be-sent");
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    (await upstream.IsConfiguredAsync(CancellationToken.None)).ShouldBeTrue();
    upstream.Model.ShouldBe(XaiChatDefaults.DefaultModel);

    OneOf<Response, SharedProblemDetails> first = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    first.IsT0.ShouldBeTrue();
    first.AsT0.ToolCalls.Single().Name.ShouldBe("page_context");

    Command second = UserTurn();
    second.Messages.Add(new Turn
    {
      Role = "tool",
      ToolCallId = "call-1",
      ToolResult = "route=/Counter"
    });
    OneOf<Response, SharedProblemDetails> answered = await upstream.CompleteAsync(second, CancellationToken.None);
    answered.IsT0.ShouldBeTrue();
    answered.AsT0.Text.ShouldNotBeNull();
    answered.AsT0.Text!.ShouldContain("route=/Counter");
  }

  public static async Task Where_With_Site_Map_Offered_Calls_Site_Map()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    Command command = UserTurn("Where can I add a passkey?", "page_context", "site_map", "navigate");

    OneOf<Response, SharedProblemDetails> response = await upstream.CompleteAsync(command, CancellationToken.None);

    response.IsT0.ShouldBeTrue();
    response.AsT0.ToolCalls.Single().Name.ShouldBe("site_map");
  }

  public static async Task Where_Without_Site_Map_Still_Calls_Page_Context()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    Command command = UserTurn("Where can I add a passkey?", "page_context");

    OneOf<Response, SharedProblemDetails> response = await upstream.CompleteAsync(command, CancellationToken.None);

    response.IsT0.ShouldBeTrue();
    response.AsT0.ToolCalls.Single().Name.ShouldBe("page_context");
  }

  public static async Task Site_Map_Result_Is_Answered_With_A_Link()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    Command command = UserTurn("Where can I add a passkey?", "site_map", "navigate");
    command.Messages.Add(SiteMapCall());
    command.Messages.Add(SiteMapResult());

    OneOf<Response, SharedProblemDetails> response = await upstream.CompleteAsync(command, CancellationToken.None);

    response.IsT0.ShouldBeTrue();
    response.AsT0.Text.ShouldNotBeNull();
    response.AsT0.Text!.ShouldContain("[Settings](/Settings)");
  }

  public static async Task Take_Me_There_Calls_Navigate_With_The_Linked_Url()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    Command command = UserTurn("Where can I add a passkey?", "site_map", "navigate");
    command.Messages.Add(new Turn
    {
      Role = "assistant",
      Text = "You can do that on the [Settings](/Settings) page."
    });
    command.Messages.Add(new Turn { Role = "user", Text = "Take me there" });

    OneOf<Response, SharedProblemDetails> response = await upstream.CompleteAsync(command, CancellationToken.None);

    response.IsT0.ShouldBeTrue();
    ToolCall call = response.AsT0.ToolCalls.Single();
    call.Name.ShouldBe("navigate");
    using JsonDocument arguments = JsonDocument.Parse(call.ArgumentsJson);
    arguments.RootElement.GetProperty("url").GetString().ShouldBe("/Settings");
  }

  public static async Task Navigate_Result_Is_Echoed_With_The_Tool_Name()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    Command command = UserTurn("Take me there", "site_map", "navigate");
    command.Messages.Add(new Turn
    {
      Role = "assistant",
      ToolCalls = [new ToolCall { Name = "navigate", CallId = "call-navigate", ArgumentsJson = """{"url":"/Settings"}""" }]
    });
    command.Messages.Add(new Turn { Role = "tool", ToolCallId = "call-navigate", ToolResult = "navigated" });

    OneOf<Response, SharedProblemDetails> response = await upstream.CompleteAsync(command, CancellationToken.None);

    response.IsT0.ShouldBeTrue();
    response.AsT0.Text.ShouldNotBeNull();
    response.AsT0.Text!.ShouldStartWith("navigate: ");
  }

  public static async Task Production_Ignores_The_Fake_And_A_Missing_Key()
  {
    await using ServiceProvider provider = Build("Production", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    (await upstream.IsConfiguredAsync(CancellationToken.None)).ShouldBeFalse();
    upstream.Model.ShouldBeNull();
  }

  public static async Task Stored_Key_Is_Sent_And_A_Replacement_Is_Used_Without_A_Restart()
  {
    const string first = "xai-test-key-398-aaaa";
    const string second = "xai-test-key-398-bbbb";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: null, store, factory);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      first,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> firstTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      second,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    OneOf<Response, SharedProblemDetails> secondTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    firstTurn.IsT0.ShouldBeTrue();
    secondTurn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([first, second]);
  }

  public static async Task Host_Key_Wins_Over_The_Stored_Key()
  {
    const string host = "xai-host-key-398-hhhh";
    const string stored = "xai-test-key-398-aaaa";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      stored,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: host, store, factory);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> turn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    turn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([host]);
  }

  public static async Task One_Turn_Unprotects_The_Stored_Key_Once()
  {
    const string key = "xai-test-key-398-aaaa";
    MemoryProtectedSecretStore store = new();
    CapturingChatClientFactory factory = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      key,
      Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await using ServiceProvider provider = Build("Production", useFake: false, apiKey: null, store, factory);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    upstream.BeginKeyResolution();
    (await upstream.IsConfiguredAsync(CancellationToken.None)).ShouldBeTrue();
    OneOf<Response, SharedProblemDetails> turn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    turn.IsT0.ShouldBeTrue();
    store.UnprotectCalls.ShouldBe(1);
    factory.Keys.ShouldBe([key]);
  }

  public static async Task Allow_Sends_The_Signed_In_Users_Key_And_A_Policy_Change_Is_The_Next_Turn()
  {
    const string userKey = "xai-user-key-399-aaaa";
    const string siteKey = "xai-site-key-399-ssss";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userA),
      UserXaiApiKeyNames.PurposeFor(userA),
      userKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      siteKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create());
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    CapturingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    OneOf<Response, SharedProblemDetails> userTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    SiteSettings? current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Off);
    await settings.UpdateAsync(current);
    OneOf<Response, SharedProblemDetails> offTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Required);
    await settings.UpdateAsync(current);
    OneOf<Response, SharedProblemDetails> requiredTurn =
      await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    current = await settings.GetAsync();
    current.ShouldNotBeNull();
    current.SetXaiByokMode(XaiByokMode.Allow);
    await settings.UpdateAsync(current);
    principal.Id = PrincipalId.From(userB);
    OneOf<Response, SharedProblemDetails> otherTurn = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    userTurn.IsT0.ShouldBeTrue();
    offTurn.IsT0.ShouldBeTrue();
    requiredTurn.IsT0.ShouldBeTrue();
    otherTurn.IsT0.ShouldBeTrue();
    factory.Keys.ShouldBe([userKey, siteKey, userKey, siteKey]);
    upstream.MissingKeyDetail(development: false).ShouldNotContain(userKey);
  }

  public static async Task Required_Names_Profile_And_Omits_The_Admin_Hint()
  {
    const string siteKey = "xai-site-key-399-ssss";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      SiteXaiApiKeyNames.Name,
      SiteXaiApiKeyNames.Purpose,
      siteKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      new CapturingChatClientFactory(),
      settings,
      principal);
    TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response response =
      await Configuration(provider, principal);

    response.Configured.ShouldBeFalse();
    response.AdminHint.ShouldBeNull();
    response.KeyHint.ShouldBe(XaiByokCopy.SetYourKey);
    response.KeyHint!.ShouldNotContain(siteKey);
  }

  public static async Task Off_Names_An_Admin_And_Does_Not_Use_The_User_Key()
  {
    const string userKey = "xai-user-key-399-aaaa";
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    MemoryProtectedSecretStore store = new();
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userA),
      UserXaiApiKeyNames.PurposeFor(userA),
      userKey,
      userA,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Off));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      new CapturingChatClientFactory(),
      settings,
      principal);
    TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response response =
      await Configuration(provider, principal);

    response.Configured.ShouldBeFalse();
    response.AdminHint.ShouldBe(XaiChatDefaults.AdminAiSentence);
    response.KeyHint.ShouldBe(XaiByokCopy.AskAdmin + " " + XaiChatDefaults.HostSetupText(development: false));
    response.KeyHint!.ShouldNotContain(userKey);
  }

  public static async Task Two_Keys_Alternating_Reuse_The_Cached_Clients()
  {
    Guid userA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid userB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    MemoryProtectedSecretStore store = new();
    await StoreUserKeyAsync(store, userA, "xai-user-key-399-aaaa");
    await StoreUserKeyAsync(store, userB, "xai-user-key-399-bbbb");
    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(userA) };
    TrackingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    for (int turn = 0; turn < 10; turn++)
    {
      principal.Id = PrincipalId.From(turn % 2 == 0 ? userA : userB);
      OneOf<Response, SharedProblemDetails> result =
        await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
      result.IsT0.ShouldBeTrue();
    }

    factory.Created.ShouldBe(2);
    factory.Live.ShouldBe(2);
  }

  public static async Task Distinct_Keys_Past_The_Cache_Bound_Do_Not_Keep_Every_Client()
  {
    const int keys = XaiChatUpstream.MaxCachedClients + 4;
    MemoryProtectedSecretStore store = new();
    Guid[] users = new Guid[keys];
    for (int index = 0; index < keys; index++)
    {
      users[index] = Guid.Parse($"00000000-0000-0000-0000-{index + 1:x12}");
      await StoreUserKeyAsync(store, users[index], $"xai-user-key-399-{index:x4}");
    }

    InMemorySiteSettingsStore settings = new();
    await settings.AddAsync(SiteSettings.Create(xaiByokMode: XaiByokMode.Required));
    SwitchingPrincipal principal = new() { Id = PrincipalId.From(users[0]) };
    TrackingChatClientFactory factory = new();
    await using ServiceProvider provider = Build(
      "Production",
      useFake: false,
      apiKey: null,
      store,
      factory,
      settings,
      principal);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();

    foreach (Guid userId in users)
    {
      principal.Id = PrincipalId.From(userId);
      OneOf<Response, SharedProblemDetails> result =
        await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
      result.IsT0.ShouldBeTrue();
    }

    factory.Created.ShouldBe(keys);
    factory.Live.ShouldBe(XaiChatUpstream.MaxCachedClients);
  }

  public static Task Tool_Declarations_Are_Not_Invocable()
  {
    List<AITool> tools = XaiChatMapping.ToTools
    (
      [
        new ToolDefinition
        {
          Name = "page_context",
          Description = "Read the page",
          ParametersJson = """{"type":"object","properties":{}}"""
        }
      ]
    );

    AITool tool = tools.Single();
    AIFunctionDeclaration declaration = tool.ShouldBeAssignableTo<AIFunctionDeclaration>();
    declaration.ShouldNotBeAssignableTo<AIFunction>();
    declaration.Name.ShouldBe("page_context");
    declaration.JsonSchema.ValueKind.ShouldBe(JsonValueKind.Object);
    return Task.CompletedTask;
  }

  private static Turn SiteMapCall() => new()
  {
    Role = "assistant",
    ToolCalls = [new ToolCall { Name = "site_map", CallId = "call-site-map", ArgumentsJson = "{}" }]
  };

  private static Turn SiteMapResult() => new()
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

  private static Command UserTurn(string text, params string[] toolNames) => new()
  {
    Messages = [new Turn { Role = "user", Text = text }],
    Tools = toolNames
      .Select(name => new ToolDefinition { Name = name, ParametersJson = """{"type":"object","properties":{}}""" })
      .ToList()
  };

  private static async Task StoreUserKeyAsync(MemoryProtectedSecretStore store, Guid userId, string apiKey) =>
    await store.SetAsync(
      UserXaiApiKeyNames.NameFor(userId),
      UserXaiApiKeyNames.PurposeFor(userId),
      apiKey,
      userId,
      DateTimeOffset.UnixEpoch,
      CancellationToken.None);

  private static async Task<TimeWarp.Architecture.Features.AgentChats.GetAgentChatConfiguration.Response> Configuration(
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

  private static Command UserTurn() => new()
  {
    Messages = [new Turn { Role = "user", Text = "What is on this page?" }]
  };

  private static ServiceProvider Build(
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

  private sealed class SwitchingPrincipal : ICurrentPrincipalAccessor
  {
    public PrincipalId? Id { get; set; }

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult(Id);
  }

  private sealed class CapturingChatClientFactory : IXaiChatClientFactory
  {
    public List<string> Keys { get; } = [];

    public IChatClient Create(string apiKey, string endpoint, string model)
    {
      _ = endpoint;
      _ = model;
      return new StubChatClient(apiKey, Keys);
    }
  }

  private sealed class StubChatClient : IChatClient
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

  private sealed class TrackingChatClientFactory : IXaiChatClientFactory
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

  private sealed class TrackingChatClient : IChatClient
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

  private sealed class MemoryProtectedSecretStore : IProtectedSecretStore
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

  private sealed class NamedEnvironment : IHostEnvironment
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
