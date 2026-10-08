#region Purpose
// Catalog tools for the in-app model and WebMCP: mapping, permissions, approval, and a fake client
// that dispatches a real store action.
#endregion

#region Design
// The host is the same Aspire SPA session the catalog roster uses. No live model and no secret:
// the scripted IChatClient returns one function call, then text. Approval is a ToolApprovalResponse
// the session writes before the store runs. WebMCP registration is the same tool list applied to a
// stand-in model context; a null context registers nothing.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Integration")]
public class CatalogAgent_Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CatalogAgent_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Schema_Names_Increment_Amount_And_Omits_CreateRole_UserId()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();

    string increment = CatalogAgentSchema.For(catalog.Find("Counter.IncrementCounter").ShouldNotBeNull());
    increment.ShouldContain("amount");
    increment.ShouldContain("integer");

    string createRole = CatalogAgentSchema.For(catalog.Find("Role.CreateRole").ShouldNotBeNull());
    createRole.ShouldContain("name");
    createRole.ShouldNotContain("userId");
    createRole.ShouldNotContain("UserId");
    return Task.CompletedTask;
  }

  public static Task Binds_Json_Amount_To_Int()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry increment = catalog.Find("Counter.IncrementCounter").ShouldNotBeNull();
    JsonElement amount = JsonSerializer.SerializeToElement(5);

    object?[] bound = CatalogAgentArguments.Bind
    (
      increment,
      new Dictionary<string, object?> { ["Amount"] = amount }
    );

    bound.Length.ShouldBe(1);
    bound[0].ShouldBe(5);
    return Task.CompletedTask;
  }

  public static async Task Select_Is_Page_Scoped_Permission_Filtered_And_Drops_Human()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);
    ClaimsPrincipal developer = Principal(PermissionIds.DeveloperAccess);
    ClaimsPrincipal anonymous = new(new ClaimsIdentity());

    IReadOnlyList<CatalogAgentTool> counter = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Counter?from=palette", CancellationToken.None
    );
    Names(counter).ShouldBe(["Counter.IncrementCounter"]);
    counter[0].RequiresApproval.ShouldBeTrue();

    IReadOnlyList<CatalogAgentTool> settings = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Settings/", CancellationToken.None
    );
    Names(settings).ShouldBe
    (
      [
        "Credentials.FetchCredentials",
        "Credentials.RevokeCredential",
        "Credentials.RenameCredential",
      ]
    );
    settings.Single(tool => tool.Name == "Credentials.FetchCredentials").RequiresApproval.ShouldBeFalse();
    settings.Single(tool => tool.Name == "Credentials.RevokeCredential").RequiresApproval.ShouldBeTrue();

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        developer, authorization, catalog.Entries, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBeEmpty();

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/Admin/Roles", CancellationToken.None
      )
    ).ShouldBeEmpty();

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/StyleGuide", CancellationToken.None
      )
    ).ShouldBeEmpty();

    (
      await CatalogAgentToolSet.SelectAsync
      (
        anonymous, authorization, catalog.Entries, "/Counter", CancellationToken.None
      )
    ).ShouldBeEmpty();

    foreach (CatalogAgentTool tool in settings)
    {
      tool.Entry.Visibility.HasFlag(ActionVisibility.Agent).ShouldBeTrue(tool.Name);
    }
  }

  public static async Task Fake_Client_Dispatches_Increment_Only_After_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      [
        new CatalogAgentTool
        (
          "Counter.IncrementCounter",
          "Add an amount to the demo counter.",
          CatalogAgentSchema.For(catalog.Find("Counter.IncrementCounter").ShouldNotBeNull()),
          RequiresApproval: true,
          catalog.Find("Counter.IncrementCounter").ShouldNotBeNull()
        ),
      ]
    );
    ChatOptions options = new() { Tools = [.. functions.Tools] };

    using ScriptedChatClient rejecting = new();
    string rejected = await CatalogAgentSession.RunAsync
    (
      rejecting,
      scope.ServiceProvider,
      [new ChatMessage(ChatRole.User, "Add 5 to the counter")],
      options,
      static _ => false,
      CancellationToken.None
    );
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    rejected.ShouldBe("Done.");

    using ScriptedChatClient approving = new();
    string approved = await CatalogAgentSession.RunAsync
    (
      approving,
      scope.ServiceProvider,
      [new ChatMessage(ChatRole.User, "Add 5 to the counter")],
      options,
      static _ => true,
      CancellationToken.None
    );

    int count = scope.Store.GetState<CounterState>().Count;
    count.ShouldBe(15);
    approved.ShouldBe("Done.");
    Console.WriteLine
    (
      $"AGENT-PROOF action=Counter.IncrementCounter amount=5 approved count={count} reply={approved}"
    );
  }

  public static Task ConfigureServices_Does_Not_Register_A_Chat_Client()
  {
    ServiceCollection services = new();
    // Qualified: Microsoft.Extensions.Configuration is a global using only when the api flag is on.
    Microsoft.Extensions.Configuration.IConfiguration configuration =
      new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
    Program.ConfigureServices(services, configuration, "Testing");

    bool registered = services.Any(descriptor => descriptor.ServiceType == typeof(IChatClient));
    registered.ShouldBeFalse();
    Console.WriteLine($"NO-MODEL-PROOF ConfigureServices IChatClient registered={registered}");
    return Task.CompletedTask;
  }

  public static Task Host_Has_No_Chat_Client()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    bool configured = CatalogAgentAvailability.IsConfigured(scope.ServiceProvider);
    configured.ShouldBeFalse();
    scope.ServiceProvider.GetService<IWebMcpModelContext>().ShouldBeNull();
    Console.WriteLine($"NO-MODEL-PROOF host IChatClient configured={configured}");
    return Task.CompletedTask;
  }

  public static async Task WebMcp_Registers_Page_Tools_And_Absent_Api_Is_A_NoOp()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);

    IReadOnlyList<WebMcpToolDescriptor> counter = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Counter", CancellationToken.None
    );
    Names(counter).ShouldBe(["Counter.IncrementCounter", PageAgentContext.ToolName]);

    IReadOnlyList<WebMcpToolDescriptor> settings = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Settings", CancellationToken.None
    );
    Names(settings).ShouldBe
    (
      [
        "Credentials.FetchCredentials",
        "Credentials.RevokeCredential",
        "Credentials.RenameCredential",
        PageAgentContext.ToolName,
      ]
    );
    settings.ShouldNotContain(tool => tool.Name == "Credentials.AddPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");

    IReadOnlyList<WebMcpToolDescriptor> styleGuide = await WebMcpPublisher.DescribeAsync
    (
      Principal(PermissionIds.DeveloperAccess),
      authorization,
      catalog,
      "/StyleGuide",
      CancellationToken.None
    );
    Names(styleGuide).ShouldBe([PageAgentContext.ToolName]);

    WebMcpApplyResult absent = await WebMcpRegistration.ApplyAsync(null, counter, CancellationToken.None);
    absent.Available.ShouldBeFalse();
    absent.Registered.ShouldBe(0);

    RecordingModelContext recording = new();
    WebMcpApplyResult applied = await WebMcpRegistration.ApplyAsync(recording, counter, CancellationToken.None);
    applied.Available.ShouldBeTrue();
    applied.Registered.ShouldBe(counter.Count);
    Names(recording.Tools).ShouldBe(Names(counter));
    Console.WriteLine($"WEBMCP-PROOF path=/Counter tools={string.Join(",", Names(counter))} registered={applied.Registered}");
    Console.WriteLine($"WEBMCP-PROOF path=/Settings tools={string.Join(",", Names(settings))}");
  }

  public static async Task WebMcp_Invoke_Dispatches_After_In_App_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    DateTime started = DateTime.UtcNow;
    while (!scope.Store.GetState<AgentSurfaceState>().HasPendingApproval)
    {
      if (invoke.IsCompleted || DateTime.UtcNow - started > TimeSpan.FromSeconds(5))
      {
        break;
      }

      await Task.Delay(20);
    }

    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeTrue(invoke.IsCompleted ? await invoke : "still waiting");
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(approved: true);
    string result = await invoke;
    int count = scope.Store.GetState<CounterState>().Count;
    count.ShouldBe(15);
    result.ShouldContain("IncrementCounter");
    Console.WriteLine($"WEBMCP-PROOF invoke Counter.IncrementCounter approved count={count} result={result}");
  }

  private static ClaimsPrincipal Principal(params string[] permissions)
  {
    List<Claim> claims = [new Claim(ClaimTypes.Name, "catalog-agent")];
    foreach (string permission in permissions)
    {
      claims.Add(new Claim(PermissionIds.ClaimType, permission));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
  }

  private static string[] Names(IReadOnlyList<CatalogAgentTool> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private static string[] Names(IReadOnlyList<WebMcpToolDescriptor> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private sealed class RecordingModelContext : IWebMcpModelContext
  {
    public List<WebMcpToolDescriptor> Tools { get; } = [];

    public ValueTask<WebMcpApplyResult> ReplaceAsync
    (
      IReadOnlyList<WebMcpToolDescriptor> tools,
      CancellationToken cancellationToken
    )
    {
      Tools.Clear();
      Tools.AddRange(tools);
      return ValueTask.FromResult(new WebMcpApplyResult(Available: true, Registered: tools.Count));
    }
  }

  private sealed class ScriptedChatClient : IChatClient
  {
    private bool OfferedCall;

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      if (!OfferedCall)
      {
        OfferedCall = true;
        FunctionCallContent call = new
        (
          "call-increment",
          "Counter.IncrementCounter",
          new Dictionary<string, object?> { ["amount"] = 5 }
        );
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
      }

      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatMessage message in response.Messages)
      {
        yield return new ChatResponseUpdate(message.Role, message.Contents);
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }
}
