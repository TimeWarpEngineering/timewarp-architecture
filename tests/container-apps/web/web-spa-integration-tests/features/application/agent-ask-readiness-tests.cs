#region Purpose
// Task 293: the Ask readiness mapping and what the Ask panel renders for each probe outcome.
#endregion

#region Design
// Two layers. ChatReadinessProbe is pure, so its mapping is pinned outcome by outcome: a 200 with a
// key, a 200 without one, a 401, and the failures that used to read as "AI not configured" (403, 500,
// the transport's synthetic 499, a file body). The second layer runs the real
// LoadChatConfiguration handler in an in-proc SPA container (no Aspire, same shape as the command
// palette tests) with a scripted IWebServerApiService, opens the docked panel (task 292 moved Ask
// off ModalController) and renders AgentAsk with HtmlRenderer, then checks the markup: the
// user-secrets command appears only when the server said there is no key, a 401 asks the user to
// sign in, and an error shows its status and detail with Retry. HtmlRenderer does not run OnAfterRenderAsync, so a configured probe
// renders "Starting Ask…" here; the Playwright test proves the chat itself.
#endregion

namespace AgentAskReadiness_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;
using TimeWarp.Foundation.Types;

[TestTag("Unit")]
public class AgentAskReadiness_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AgentAskReadiness_Should_>();

  public static Task Map_Configured_Response_To_Configured_With_Model()
  {
    ChatProbeResult result = ChatReadinessProbe.FromResponse(new GetAgentChatConfiguration.Response
    {
      Configured = true,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = "grok-4.7",
    });

    result.Readiness.ShouldBe(CatalogAgentReadiness.Configured);
    result.Model.ShouldBe("grok-4.7");
    result.Problem.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Map_NoKey_Response_To_NotConfigured_With_Command()
  {
    ChatProbeResult result = ChatReadinessProbe.FromResponse(new GetAgentChatConfiguration.Response
    {
      Configured = false,
      SetupCommand = "",
      Model = "ignored",
    });

    result.Readiness.ShouldBe(CatalogAgentReadiness.NotConfigured);
    result.SetupCommand.ShouldBe(XaiChatDefaults.SetupCommand);
    result.Model.ShouldBeNull();
    result.Problem.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Map_401_To_Unauthenticated_Not_NotConfigured()
  {
    ChatProbeResult result = ChatReadinessProbe.FromProblem(new SharedProblemDetails { Status = 401, Title = "Unauthorized" });

    result.Readiness.ShouldBe(CatalogAgentReadiness.Unauthenticated);
    result.Problem.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Map_Other_Failures_To_Error_With_Status_And_Detail()
  {
    ChatReadinessProbe.FromProblem(new SharedProblemDetails { Status = 403, Title = "Forbidden" })
      .ShouldBe(new ChatProbeResult(CatalogAgentReadiness.Error, XaiChatDefaults.SetupCommand, null, "403 Forbidden"));

    ChatReadinessProbe.FromProblem(new SharedProblemDetails { Status = 500, Title = "Internal Server Error", Detail = "boom" })
      .Problem.ShouldBe("500 Internal Server Error: boom");

    ChatReadinessProbe.FromProblem(new SharedProblemDetails { Status = 499, Title = "Operation Cancelled", Detail = "The request was cancelled." })
      .Readiness.ShouldBe(CatalogAgentReadiness.Error);

    ChatReadinessProbe.FromProblem(new SharedProblemDetails())
      .Problem.ShouldBe("No status Request failed");

    ChatProbeResult file = ChatReadinessProbe.FromFileResponse();
    file.Readiness.ShouldBe(CatalogAgentReadiness.Error);
    file.Problem.ShouldNotBeNullOrWhiteSpace();
    return Task.CompletedTask;
  }

  public static async Task Render_SetupCommand_Only_When_Server_Says_NoKey()
  {
    string html = await ProbeAndRenderAsync(new GetAgentChatConfiguration.Response
    {
      Configured = false,
      SetupCommand = XaiChatDefaults.SetupCommand,
    }, CatalogAgentReadiness.NotConfigured);

    html.ShouldContain("data-qa=\"AgentAskNotConfigured\"");
    html.ShouldContain("data-qa=\"AgentAskSetupCommand\"");
    html.ShouldContain("--id " + XaiChatDefaults.UserSecretsId);
    html.ShouldNotContain("data-qa=\"AgentAskSignIn\"");
    html.ShouldNotContain("data-qa=\"AgentAskError\"");
  }

  public static async Task Render_SignIn_For_401_Without_The_Key_Hint()
  {
    string html = await ProbeAndRenderAsync(new SharedProblemDetails { Status = 401, Title = "Unauthorized" }, CatalogAgentReadiness.Unauthenticated);

    html.ShouldContain("Sign in to use Ask");
    html.ShouldContain("data-qa=\"AgentAskSignInButton\"");
    html.ShouldNotContain("AI not configured");
    html.ShouldNotContain("user-secrets");
  }

  public static async Task Render_Error_Status_And_Retry_Without_The_Key_Hint()
  {
    string html = await ProbeAndRenderAsync
    (
      new SharedProblemDetails { Status = 500, Title = "Internal Server Error", Detail = "upstream exploded" },
      CatalogAgentReadiness.Error
    );

    html.ShouldContain("data-qa=\"AgentAskError\"");
    html.ShouldContain("500 Internal Server Error: upstream exploded");
    html.ShouldContain("data-qa=\"AgentAskRetry\"");
    html.ShouldNotContain("AI not configured");
    html.ShouldNotContain("user-secrets");
  }

  public static async Task Render_Configured_Without_Any_Warning()
  {
    string html = await ProbeAndRenderAsync(new GetAgentChatConfiguration.Response
    {
      Configured = true,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = "grok-4.7",
    }, CatalogAgentReadiness.Configured);

    html.ShouldContain("data-qa=\"AgentAskStarting\"");
    html.ShouldNotContain("AI not configured");
    html.ShouldNotContain("Sign in to use Ask");
    html.ShouldNotContain("data-qa=\"AgentAskError\"");
  }

  public static async Task Reprobe_Replaces_A_Signed_Out_Answer()
  {
    ScriptedWebServer server = new(new SharedProblemDetails { Status = 401, Title = "Unauthorized" });
    using AskSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());
    scope.Store.GetState<AgentSurfaceState>().ChatReadiness.ShouldBe(CatalogAgentReadiness.Unauthenticated);

    server.Outcome = new GetAgentChatConfiguration.Response { Configured = true, SetupCommand = XaiChatDefaults.SetupCommand, Model = "grok-4.7" };
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

    AgentSurfaceState state = scope.Store.GetState<AgentSurfaceState>();
    state.ChatReadiness.ShouldBe(CatalogAgentReadiness.Configured);
    state.ChatModel.ShouldBe("grok-4.7");
    state.ChatProblem.ShouldBeNull();
    server.Calls.ShouldBe(2);
  }

  private static async Task<string> ProbeAndRenderAsync(object outcome, CatalogAgentReadiness expected)
  {
    using AskSpa spa = new(new ScriptedWebServer(outcome));
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());
    scope.Store.GetState<AgentSurfaceState>().ChatReadiness.ShouldBe(expected);
    await scope.Send(new AgentSurfaceState.OpenAskPanelActionSet.Action());

    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AgentAsk>()).ToHtmlString());
  }

  /// <summary>Answers every request with <see cref="Outcome"/>: a response, a problem, or a file.</summary>
  private sealed class ScriptedWebServer(object outcome) : IWebServerApiService
  {
    public object Outcome { get; set; } = outcome;

    public int Calls { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      Calls++;
      OneOf<TResponse, FileResponse, SharedProblemDetails> result = Outcome switch
      {
        TResponse response => response,
        FileResponse file => file,
        SharedProblemDetails problem => problem,
        _ => throw new InvalidOperationException($"No scripted outcome for {typeof(TResponse).Name}."),
      };
      return Task.FromResult(result);
    }
  }

  /// <summary>In-proc SPA container whose web-server API is scripted.</summary>
  private sealed class AskSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public AskSpa(IWebServerApiService webServer)
    {
      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new AnonymousAuthenticationStateProvider());
      services.AddScoped(_ => A.Fake<IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped(_ => webServer);
      services.AddScoped<AskConversationThreads>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }
}
