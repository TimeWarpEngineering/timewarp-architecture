#region Purpose
// Task 347: the "Chats are recorded." notice above the Ask composer renders from configuration,
// hides on dismiss, and stays dismissed after a reload in the same browser.
#endregion

#region Design
// Same in-proc SPA container shape as the readiness tests: the real LoadChatConfiguration handler
// runs against a scripted IWebServerApiService, and AskPrivacyBanner renders with HtmlRenderer.
// Local storage is the real Blazored service over BrowserStorage, an IJSRuntime that answers
// localStorage.getItem / setItem from a dictionary. A "reload" is a second container (new store,
// new state) over the same BrowserStorage, then the LoadPrivacyNoticeDismissal action AgentAsk
// dispatches on its first render.
#endregion

namespace AskPrivacyBanner_;

using Blazored.LocalStorage;
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

[TestTag("Unit")]
public class AskPrivacyBanner_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskPrivacyBanner_Should_>();

  public static async Task Render_The_Configured_Notice_With_A_Privacy_Link_And_Dismiss()
  {
    using BannerSpa spa = new(Response(recordChats: true, privacyUrl: "/Privacy"), new BrowserStorage());
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

    string html = await RenderAsync(scope);

    html.ShouldContain("data-qa=\"AskPrivacyNotice\"");
    html.ShouldContain(XaiChatDefaults.PrivacyNotice);
    html.ShouldContain("href=\"/Privacy\"");
    html.ShouldContain("data-qa=\"AskPrivacyDismiss\"");
    html.ShouldContain("aria-label=\"Dismiss notice\"");
  }

  public static async Task Render_Nothing_When_The_App_Does_Not_Record_Chats()
  {
    using BannerSpa spa = new(Response(recordChats: false, privacyUrl: "/Privacy"), new BrowserStorage());
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

    (await RenderAsync(scope)).ShouldNotContain("AskPrivacyNotice");
  }

  public static async Task Omit_The_Link_When_PrivacyUrl_Is_Blank_Or_Unsafe()
  {
    foreach (string? privacyUrl in new[] { null, "  ", "javascript:alert(1)", "//evil.example" })
    {
      using BannerSpa spa = new(Response(recordChats: true, privacyUrl), new BrowserStorage());
      using SpaTestScope scope = SpaTestScope.Create(spa);
      await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());

      scope.Store.GetState<AgentSurfaceState>().PrivacyUrl.ShouldBeNull();
      string html = await RenderAsync(scope);
      html.ShouldContain("data-qa=\"AskPrivacyNotice\"");
      html.ShouldNotContain("data-qa=\"AskPrivacyLink\"");
    }
  }

  public static async Task Hide_On_Dismiss_And_Stay_Dismissed_After_Reload()
  {
    BrowserStorage browser = new();
    using (BannerSpa spa = new(Response(recordChats: true, privacyUrl: null), browser))
    using (SpaTestScope scope = SpaTestScope.Create(spa))
    {
      await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());
      await scope.Send(new AgentSurfaceState.LoadPrivacyNoticeDismissalActionSet.Action());
      (await RenderAsync(scope)).ShouldContain("data-qa=\"AskPrivacyNotice\"");

      await scope.Send(new AgentSurfaceState.DismissPrivacyNoticeActionSet.Action());

      (await RenderAsync(scope)).ShouldNotContain("AskPrivacyNotice");
      browser.Items.ShouldContainKey(AskPrivacyNotice.DismissedStorageKey);
    }

    using (BannerSpa reloaded = new(Response(recordChats: true, privacyUrl: null), browser))
    using (SpaTestScope scope = SpaTestScope.Create(reloaded))
    {
      await scope.Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action());
      scope.Store.GetState<AgentSurfaceState>().PrivacyNoticeDismissed.ShouldBeFalse();

      await scope.Send(new AgentSurfaceState.LoadPrivacyNoticeDismissalActionSet.Action());

      scope.Store.GetState<AgentSurfaceState>().PrivacyNoticeDismissed.ShouldBeTrue();
      (await RenderAsync(scope)).ShouldNotContain("AskPrivacyNotice");
    }
  }

  private static GetAgentChatConfiguration.Response Response(bool recordChats, string? privacyUrl) =>
    new()
    {
      Configured = true,
      SetupCommand = XaiChatDefaults.SetupCommand,
      Model = "grok-4.7",
      RecordChats = recordChats,
      PrivacyNotice = XaiChatDefaults.PrivacyNotice,
      PrivacyUrl = privacyUrl,
    };

  private static async Task<string> RenderAsync(SpaTestScope scope)
  {
    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AskPrivacyBanner>()).ToHtmlString());
  }

  /// <summary>Answers every request with the configured chat settings.</summary>
  private sealed class ScriptedWebServer(GetAgentChatConfiguration.Response response) : IWebServerApiService
  {
    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      OneOf<TResponse, FileResponse, SharedProblemDetails> result = response as TResponse
        ?? throw new InvalidOperationException($"No scripted outcome for {typeof(TResponse).Name}.");
      return Task.FromResult(result);
    }
  }

  /// <summary>Stands in for the browser's window.localStorage. Outlives a container, as a browser outlives a reload.</summary>
  private sealed class BrowserStorage : IJSRuntime
  {
    public Dictionary<string, string> Items { get; } = [];

    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      string key = (string)args![0]!;
      switch (identifier)
      {
        case "localStorage.getItem":
          return ValueTask.FromResult((TValue)(object?)Items.GetValueOrDefault(key)!);
        case "localStorage.setItem":
          Items[key] = (string)args[1]!;
          return ValueTask.FromResult(default(TValue)!);
        default:
          throw new NotSupportedException(identifier);
      }
    }
  }

  /// <summary>In-proc SPA container with a scripted web-server API and dictionary-backed local storage.</summary>
  private sealed class BannerSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public BannerSpa(GetAgentChatConfiguration.Response response, BrowserStorage browser)
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
      services.AddBlazoredLocalStorage();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new AnonymousAuthenticationStateProvider());
      services.AddScoped<IJSRuntime>(_ => browser);
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<IWebServerApiService>(_ => new ScriptedWebServer(response));

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
