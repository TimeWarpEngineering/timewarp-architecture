#region Purpose
// Admin → AI reloads site settings on open and keeps an unedited assistant draft current.
#endregion

#region Design
// HtmlRenderer's static host reports IsInteractive false, so it never runs the page's fetch.
// This host subclasses StaticHtmlRenderer and reports an interactive renderer, then renders
// AiPage in the same scope that seeded the stale snapshot. The draft is the form attribute
// data-assistant-version, not SiteSettingsState.Version: the bug was the form keeping the
// sign-in token after state could already have been refreshed. The agent-rename case
// dispatches UpdateAssistantName after the page has rendered and checks the same attribute.
#endregion

namespace AiPageAssistantDraft_;

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Admin.Ai;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Testing;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Unit")]
public class AiPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AiPage_Should_>();

  public static async Task Replace_A_Stale_Snapshot_With_The_Server_Version()
  {
    ScriptedWebServer server = new();
    server.Settings = Settings("Akili", version: 1);
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new SiteSettingsState.FetchSiteSettingsActionSet.Action());
    scope.Store.GetState<SiteSettingsState>().Version.ShouldBe(1);

    server.Settings = Settings("Zawadi", version: 7);
    string html = await RenderAsync(scope);

    scope.Store.GetState<SiteSettingsState>().Version.ShouldBe(7);
    html.ShouldContain("data-assistant-version=\"7\"");
    html.ShouldContain("data-assistant-name=\"Zawadi\"");
    html.ShouldNotContain("data-assistant-version=\"1\"");
    server.SettingsReads.ShouldBe(2);
  }

  public static async Task Copy_An_Agent_Rename_Into_An_Unedited_Draft()
  {
    ScriptedWebServer server = new();
    server.Settings = Settings("Akili", version: 3);
    server.Renamed = new UpdateAssistantName.Response("Nia", 9);
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new SiteSettingsState.FetchSiteSettingsActionSet.Action());

    string html = await RenderAsync(scope, async () =>
    {
      await scope.Send
      (
        new SiteSettingsState.UpdateAssistantNameActionSet.Action
        (
          new UpdateAssistantName.Command { AssistantName = "Nia", Version = 3 }
        )
      );
    });

    html.ShouldContain("data-assistant-version=\"9\"");
    html.ShouldContain("data-assistant-name=\"Nia\"");
  }

  private static GetSiteSettings.Response Settings(string name, long version) =>
    new(
      entraSignInEnabled: true,
      entraAllowBootstrap: false,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: name,
      version: version);

  private static async Task<string> RenderAsync(SpaTestScope scope, Func<Task>? afterOpen = null)
  {
    IServiceProvider services = scope.ServiceProvider;
    await using InteractiveHtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      HtmlRootComponent root = renderer.BeginRenderingComponent(typeof(PageHost), ParameterView.Empty);
      await root.QuiescenceTask;
      if (afterOpen is not null)
      {
        await afterOpen();
        await renderer.Dispatcher.InvokeAsync(() => { });
      }

      return WebUtility.HtmlDecode(root.ToHtmlString());
    });
    return html;
  }

  private sealed class PageHost : ComponentBase
  {
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<AiPage>(0);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }

  /// <summary>Static HTML rendering with <see cref="RendererInfo.IsInteractive"/> set.</summary>
  private sealed class InteractiveHtmlRenderer : StaticHtmlRenderer
  {
    public InteractiveHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
      : base(services, loggerFactory)
    {
    }

    protected override RendererInfo RendererInfo { get; } = new("Test", isInteractive: true);
  }

  private sealed class DraftSpa(IWebServerApiService server) : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; } = Build(server);

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private static ServiceProvider Build(IWebServerApiService server)
    {
      ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "admin")], authenticationType: "test");
      ServiceCollection services = StaticRenderHost.CreateServices(user: new ClaimsPrincipal(identity));
      services.AddScoped<IWebServerApiService>(_ => server);
      return services.BuildServiceProvider();
    }
  }

  /// <summary>
  /// Answers GetSiteSettings from <see cref="Settings"/> and UpdateAssistantName from
  /// <see cref="Renamed"/>. Any other response type is a problem that names the type.
  /// </summary>
  private sealed class ScriptedWebServer : IWebServerApiService
  {
    public GetSiteSettings.Response? Settings { get; set; }

    public UpdateAssistantName.Response? Renamed { get; set; }

    public int SettingsReads { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = request;
      _ = cancellationToken;
      object? outcome = null;
      if (typeof(TResponse) == typeof(GetSiteSettings.Response))
      {
        SettingsReads++;
        outcome = Settings;
      }
      else if (typeof(TResponse) == typeof(UpdateAssistantName.Response))
      {
        outcome = Renamed;
      }
      else if (typeof(TResponse) == typeof(GetXaiApiKeyStatus.Response))
      {
        outcome = new GetXaiApiKeyStatus.Response(
          managedByHost: false,
          configured: false,
          storageAvailable: true,
          canChange: true,
          needsReplacement: false,
          suffix: null,
          setAt: null,
          setByPrincipalId: null,
          setupHint: XaiChatDefaults.DeployedSetupHint);
      }
      else if (typeof(TResponse) == typeof(GetAssistantIdentity.Response))
      {
        outcome = new GetAssistantIdentity.Response(
          Renamed?.AssistantName ?? Settings?.AssistantName ?? AssistantNameRules.Default,
          GetAssistantIdentity.MockAvatar);
      }

      OneOf<TResponse, FileResponse, SharedProblemDetails> result = outcome switch
      {
        TResponse response => response,
        _ => new SharedProblemDetails { Status = 500, Title = $"No scripted outcome for {typeof(TResponse).Name}." },
      };
      return Task.FromResult(result);
    }
  }
}
