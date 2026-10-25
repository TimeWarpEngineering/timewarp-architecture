#region Purpose
// One HtmlRenderer service host for the static SPA page contracts.
#endregion

#region Design
// StyleGuidePage_Should_ and TimeWarpPage_Should_ both render through this host so the
// registrations stay one list. JS import throws, so WebMCP treats the browser API as absent.
// A fixed authentication state supplies the cascade Routes would host. TimeProvider.System is
// registered because FilterBar's search debounce injects that clock; a static render never
// waits on it. Brand options and the signed-in principal are the only per-call inputs.
// Title and CountOpen live here because both contracts read the h1 the same way.
// Brand options register through IOptions<>. The Options namespace is a global
// using only when the api template symbol is on, and this host compiles with that symbol off,
// so the file imports the namespace itself. Sign-in buttons read the server's offered list.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using System.Security.Claims;
using Blazored.SessionStorage;
using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.State.Plus;

public static class StaticRenderHost
{
  public static ServiceCollection CreateServices(BrandOptions? brand = null, ClaimsPrincipal? user = null)
  {
    BrandOptions brandOptions = brand ?? new BrandOptions();
    ServiceCollection serviceCollection = new();
    serviceCollection.AddLogging();
    serviceCollection.AddSingleton(TimeProvider.System);
    serviceCollection.AddFluentUIComponents();
    serviceCollection.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
    serviceCollection.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
    serviceCollection.AddScoped<IJSRuntime, HeadlessJsRuntime>();
    serviceCollection.AddScoped(_ => A.Fake<ISessionStorageService>());
    serviceCollection.AddSingleton<IOptions<BrandOptions>>(Options.Create(brandOptions));
    serviceCollection.AddWebSpaGeneratedMediator();
    serviceCollection.AddTimeWarpState(options =>
    {
      options.Assemblies =
      [
        typeof(TimeWarpPage).Assembly,
        typeof(AssemblyMarker).Assembly
      ];
    });
    serviceCollection.AddTimeWarpStateBlazor();
    serviceCollection.AddActionCatalog(typeof(TimeWarpPage).Assembly);
    serviceCollection.AddScoped<PageAgentRoute>();
    serviceCollection.AddScoped<AgentCallOutcome>();
    serviceCollection.AddScoped<WebMcpApprovalGate>();
    serviceCollection.AddScoped<WebMcpDispatcher>();
    serviceCollection.AddScoped<JsWebMcpModelContext>();
    serviceCollection.AddScoped<WebMcpPublisher>();
    serviceCollection.AddScoped<NavigationManager, TestNavigationManager>();
    return serviceCollection;
  }

  public static string Title(string html)
  {
    const string open = "<h1";
    int start = html.IndexOf(open, StringComparison.Ordinal);
    start.ShouldBeGreaterThan(-1, "page rendered no h1");
    int content = html.IndexOf('>', start, StringComparison.Ordinal);
    int end = html.IndexOf("</h1", content, StringComparison.Ordinal);
    return html.Substring(content + 1, end - content - 1).Trim();
  }

  public static int CountOpen(string html, string tagName)
  {
    int count = 0;
    string open = "<" + tagName;
    int index = 0;
    while (index < html.Length)
    {
      int found = html.IndexOf(open, index, StringComparison.Ordinal);
      if (found < 0)
      {
        break;
      }

      int after = found + open.Length;
      if (after >= html.Length || !char.IsAsciiLetterOrDigit(html[after]))
      {
        count++;
      }

      index = after;
    }

    return count;
  }

  private sealed class FixedAuthenticationStateProvider : AuthenticationStateProvider
  {
    private readonly ClaimsPrincipal User;

    public FixedAuthenticationStateProvider(ClaimsPrincipal? user = null)
    {
      User = user ?? new ClaimsPrincipal(new ClaimsIdentity());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(User));
  }

  private sealed class HeadlessJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      if (identifier == "import")
      {
        throw new JSException("no browser");
      }

      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      if (identifier == "import")
      {
        throw new JSException("no browser");
      }

      return ValueTask.FromResult(default(TValue)!);
    }
  }
}
