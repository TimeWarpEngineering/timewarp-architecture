#region Purpose
// Task 361: the assistant name and avatar reach every SPA label from one state, AgentSurfaceState.
#endregion

#region Design
// Runs the real LoadAssistantIdentity handler in an in-proc SPA container with a scripted
// IWebServerApiService (same shape as the Ask readiness tests), then checks the state, the derived
// Ctrl-K row's runtime label (AskAssistantDisplayOverride, task 301's override), and the Ask panel
// markup rendered with HtmlRenderer. HtmlRenderer does not run OnAfterRenderAsync, so the agent is
// never built: the header and the signed-out state carry the name here, and the Playwright rename
// test proves the greeting and the top-bar button in a browser. The refresh tests dispatch the real
// UpdateAssistantName and ResetStore actions and check that AssistantIdentityRefreshHandler reloads
// the identity, so a rename by any caller (the admin page or the agent) reaches every label.
#endregion

namespace AssistantIdentity_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;
using TimeWarp.Foundation.Types;

[TestTag("Unit")]
public class AssistantIdentity_Should_
{
  private const string ZawadiAvatar = "data:image/svg+xml;base64,PHN2Zy8+";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AssistantIdentity_Should_>();

  public static Task Default_To_Akili_Before_The_Server_Answers()
  {
    using AskSpa spa = new(new ScriptedWebServer(new SharedProblemDetails { Status = 500, Title = "Down" }));
    using SpaTestScope scope = SpaTestScope.Create(spa);

    AgentSurfaceState state = scope.Store.GetState<AgentSurfaceState>();
    state.AssistantName.ShouldBe("Akili");
    state.AskLabel.ShouldBe("Ask Akili");
    state.AssistantAvatar.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static async Task Apply_The_Configured_Name_And_Avatar()
  {
    using AskSpa spa = new(new ScriptedWebServer(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar)));
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());

    AgentSurfaceState state = scope.Store.GetState<AgentSurfaceState>();
    state.AssistantName.ShouldBe("Zawadi");
    state.AskLabel.ShouldBe("Ask Zawadi");
    state.AssistantAvatar.ShouldBe(ZawadiAvatar);
  }

  public static async Task Keep_The_Current_Name_When_The_Answer_Is_Invalid_Or_Fails()
  {
    ScriptedWebServer server = new(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar));
    using AskSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());

    server.Outcome = new GetAssistantIdentity.Response("<b>Bad</b>", ZawadiAvatar);
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());
    scope.Store.GetState<AgentSurfaceState>().AssistantName.ShouldBe("Zawadi");

    server.Outcome = new SharedProblemDetails { Status = 503, Title = "Site settings unavailable" };
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());
    scope.Store.GetState<AgentSurfaceState>().AssistantName.ShouldBe("Zawadi");
    server.Calls.ShouldBe(3);
  }

  public static async Task Label_The_Derived_Ctrl_K_Row_With_The_Name()
  {
    using AskSpa spa = new(new ScriptedWebServer(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar)));
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ActionRosterEntry openAsk = ActionRoster.All.Single(static action => action.Name == "AgentSurface.OpenAskPanel");
    ActionRosterEntry signOut = ActionRoster.All.Single(static action => action.Name == "Profile.SignOut");
    AskAssistantDisplayOverride displayOverride = new(scope.Store);

    displayOverride.Find(openAsk).ShouldNotBeNull().Name.ShouldBe("Ask Akili");
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());

    displayOverride.Find(openAsk).ShouldNotBeNull().Name.ShouldBe("Ask Zawadi");
    displayOverride.Find(signOut).ShouldBeNull();
  }

  public static async Task Name_The_Ask_Panel_Header_And_Show_The_Avatar()
  {
    ScriptedWebServer server = new(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar));
    using AskSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());
    server.Outcome = new SharedProblemDetails { Status = 401, Title = "Unauthorized" };
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());
    await scope.Send(new AgentSurfaceState.OpenAskPanelActionSet.Action());

    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AgentAsk>()).ToHtmlString());

    html.ShouldContain("data-qa=\"AskTitle\"");
    html.ShouldContain(">Zawadi</h2>");
    html.ShouldContain("alt=\"Zawadi\"");
    html.ShouldContain("data-qa=\"AskAvatar\"");
    html.ShouldContain("PHN2Zy8");
    html.ShouldContain("Sign in to use Zawadi");
    html.ShouldNotContain("Akili");
    html.ShouldNotContain(">Ask<");
  }

  public static async Task Name_The_Settings_Button_And_Its_Status()
  {
    using AskSpa spa = new(new ScriptedWebServer(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar)));
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());

    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AskSettingsMenu>()).ToHtmlString());

    html.ShouldContain("aria-label=\"Zawadi settings\"");
    html.ShouldContain("title=\"Zawadi settings\"");
    html.ShouldContain("Zawadi runs under your session without a conversation credential");
    html.ShouldNotContain("Ask settings");
    html.ShouldNotContain("Ask runs");
  }

  public static async Task Reload_The_Identity_After_An_Assistant_Rename()
  {
    ScriptedWebServer server = new(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar));
    server.Outcomes[typeof(UpdateAssistantName.Response)] = new UpdateAssistantName.Response("Zawadi", 1);
    using AskSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.Store.GetState<AgentSurfaceState>().AssistantName.ShouldBe("Akili");

    await scope.Send
    (
      new SiteSettingsState.UpdateAssistantNameActionSet.Action
      (
        new UpdateAssistantName.Command { AssistantName = "Zawadi", Version = 0 }
      )
    );

    AgentSurfaceState state = scope.Store.GetState<AgentSurfaceState>();
    state.AssistantName.ShouldBe("Zawadi");
    state.AskLabel.ShouldBe("Ask Zawadi");
    state.AssistantAvatar.ShouldBe(ZawadiAvatar);
  }

  public static async Task Skip_The_Reload_When_The_Rename_Fails()
  {
    ScriptedWebServer server = new(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar));
    server.Outcomes[typeof(UpdateAssistantName.Response)] = new SharedProblemDetails { Status = 409, Title = "Conflict" };
    using AskSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send
    (
      new SiteSettingsState.UpdateAssistantNameActionSet.Action
      (
        new UpdateAssistantName.Command { AssistantName = "Zawadi", Version = 0 }
      )
    );

    scope.Store.GetState<SiteSettingsState>().AssistantNameSaveFailed.ShouldBeTrue();
    scope.Store.GetState<AgentSurfaceState>().AssistantName.ShouldBe("Akili");
    server.Calls.ShouldBe(1);
  }

  public static async Task Reload_The_Identity_After_A_Store_Reset()
  {
    using AskSpa spa = new(new ScriptedWebServer(new GetAssistantIdentity.Response("Zawadi", ZawadiAvatar)));
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadAssistantIdentityActionSet.Action());

    await scope.Send(new ApplicationState.ResetStoreActionSet.Action());

    AgentSurfaceState state = scope.Store.GetState<AgentSurfaceState>();
    state.AssistantName.ShouldBe("Zawadi");
    state.AssistantAvatar.ShouldBe(ZawadiAvatar);
  }

  /// <summary>
  /// Answers a request with its response type's entry in <see cref="Outcomes"/>, else with
  /// <see cref="Outcome"/>: a response or a problem.
  /// </summary>
  private sealed class ScriptedWebServer(object outcome) : IWebServerApiService
  {
    public object Outcome { get; set; } = outcome;

    public Dictionary<Type, object> Outcomes { get; } = [];

    public int Calls { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      Calls++;
      object outcome = Outcomes.TryGetValue(typeof(TResponse), out object? byType) ? byType : Outcome;
      OneOf<TResponse, FileResponse, SharedProblemDetails> result = outcome switch
      {
        TResponse response => response,
        SharedProblemDetails problem => problem,
        _ => new SharedProblemDetails { Status = 500, Title = $"No scripted outcome for {typeof(TResponse).Name}." },
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
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new AnonymousAuthenticationStateProvider());
      services.AddScoped(_ => A.Fake<IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
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
