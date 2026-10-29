#region Purpose
// Task 356: the Ask composer's settings menu shows the conversation credential's real status
// (active, expired or absent) and sends Configure permissions to the Agent links page.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test, the edit-mode pill tests' container shape.
// AskSettingsMenu renders with HtmlRenderer, which has no fluent-menu web component, so opening
// the menu, the keyboard path and the navigation itself are the Playwright Ask surface test. Here
// the rendered markup is pinned for each credential state, with the credential set through the
// real SetConversation and NewConversation actions, never a hand-built state.
#endregion

namespace AskSettingsMenu_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public partial class AskSettingsMenu_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskSettingsMenu_Should_>();

  public static async Task Show_Absent_Without_A_Credential()
  {
    using MenuSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope);

    string button = TagOf(html, "AskSettings");
    button.ShouldContain("aria-label=\"Akili settings\"");
    button.ShouldContain("data-status=\"Absent\"");
    TagOf(html, "AskAgentAccessStatus").ShouldContain("data-status=\"Absent\"");
    html.ShouldContain("No agent credential");
    html.ShouldNotContain("Agent access active");
    html.ShouldNotContain("data-qa=\"AskAgentAccessExpiry\"");
    html.ShouldNotContain("data-qa=\"AskRenewAgentAccess\"");
    html.ShouldContain("Configure permissions");
  }

  public static async Task Show_Active_With_Its_Expiry()
  {
    using MenuSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

    await SetConversationAsync(scope, expiresAt);
    string html = await RenderAsync(scope);

    TagOf(html, "AskSettings").ShouldContain("data-status=\"Active\"");
    TagOf(html, "AskAgentAccessStatus").ShouldContain("data-status=\"Active\"");
    html.ShouldContain("Agent access active");
    html.ShouldContain("Akili can act on this app as you");
    TagOf(html, "AskAgentAccessExpiry")
      .ShouldContain($"datetime=\"{expiresAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)}\"");
    html.ShouldContain("Expires at ");
    // Task 452: the visible expiry is a local ISO 8601 instant with its offset.
    string expiry = System.Net.WebUtility.HtmlDecode(ExpiryText().Match(html).Groups["expiry"].Value);
    expiry.ShouldMatch(LocalIsoInstant);
    DateTimeOffset.Parse(expiry, CultureInfo.InvariantCulture).ShouldBe(expiresAt.AddTicks(-(expiresAt.Ticks % TimeSpan.TicksPerSecond)));
    html.ShouldNotContain("data-qa=\"AskRenewAgentAccess\"");
  }

  public static async Task Show_Expired_With_A_Renew_Item()
  {
    using MenuSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await SetConversationAsync(scope, DateTimeOffset.UtcNow.AddMinutes(-1));
    string html = await RenderAsync(scope);

    TagOf(html, "AskSettings").ShouldContain("data-status=\"Expired\"");
    html.ShouldContain("Agent access expired");
    html.ShouldNotContain("Agent access active");
    html.ShouldNotContain("data-qa=\"AskAgentAccessExpiry\"");
    TagOf(html, "AskRenewAgentAccess").ShouldNotBeNull();

    // New conversation is the renewal: it clears the expired credential for the panel to re-mint.
    await scope.Send(new AgentSurfaceState.NewConversationActionSet.Action());
    html = await RenderAsync(scope);
    TagOf(html, "AskSettings").ShouldContain("data-status=\"Absent\"");
  }

  public static async Task Keep_The_Status_Item_Out_Of_Reach()
  {
    using MenuSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope);

    TagOf(html, "AskAgentAccessStatus").ShouldContain("disabled");
    TagOf(html, "AskConfigurePermissions").ShouldNotContain("disabled");
  }

  public static Task Send_Configure_Permissions_To_Agent_Links()
  {
    AskSettingsMenu.ConfigurePermissionsRoute.ShouldBe(AgentLinksPage.GetPageUrl());
    AskSettingsMenu.ConfigurePermissionsRoute.ShouldBe("/AgentLinks");
    return Task.CompletedTask;
  }

  private static Task SetConversationAsync(SpaTestScope scope, DateTimeOffset expiresAt) =>
    scope.Send
    (
      new AgentSurfaceState.SetConversationActionSet.Action
      (
        Guid.NewGuid(),
        Guid.NewGuid(),
        ["counter:read"],
        expiresAt,
        "Test conversation"
      )
    );

  private static async Task<string> RenderAsync(SpaTestScope scope)
  {
    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AskSettingsMenu>()).ToHtmlString());
  }

  private static string TagOf(string html, string dataQa)
  {
    string? tag = OpeningTag().Matches(html)
      .Select(static match => match.Value)
      .FirstOrDefault(value => value.Contains($"data-qa=\"{dataQa}\"", StringComparison.Ordinal));
    tag.ShouldNotBeNull(dataQa);
    return tag;
  }

  [GeneratedRegex("<[^>]+>")]
  private static partial Regex OpeningTag();

  private const string LocalIsoInstant = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$";

  [GeneratedRegex("data-qa=\"AskAgentAccessExpiry\"[^>]*>Expires at (?<expiry>[^<]+)<")]
  private static partial Regex ExpiryText();

  /// <summary>In-proc SPA container; the menu needs only state and FluentUI.</summary>
  private sealed class MenuSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public MenuSpa()
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
      services.AddScoped<IJSRuntime, NoJsRuntime>();
      services.AddScoped<NavigationManager, TestNavigationManager>();

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

  /// <summary>The menu renders without interop; any JS call is a test failure.</summary>
  private sealed class NoJsRuntime : IJSRuntime
  {
    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      throw new NotSupportedException(identifier);
  }

  private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
  }
}
