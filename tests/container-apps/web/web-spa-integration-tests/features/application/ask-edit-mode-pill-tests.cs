#region Purpose
// Task 355: the Ask composer's edit-mode pill shows the current mode and offers both modes as a
// radio menu, and choosing a mode is the existing SetEditMode action.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test, the starter-prompt tests' container shape.
// AskEditModePill renders with HtmlRenderer, which has no fluent-menu web component, so opening the
// menu and the keyboard path are the Playwright Ask surface test. Here the rendered markup is pinned:
// the pill's label, accessible name and data-mode follow the state, and the checked radio item
// moves with it after the action is sent through the real pipeline.
#endregion

namespace AskEditModePill_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public partial class AskEditModePill_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskEditModePill_Should_>();

  public static async Task Show_Ask_First_By_Default_With_Both_Options()
  {
    using PillSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope);

    string pill = TagOf(html, "AskEditMode");
    pill.ShouldContain("aria-label=\"Edit mode: Ask before editing\"");
    pill.ShouldContain("data-mode=\"AskBeforeEditing\"");
    html.ShouldContain("Ask first");
    html.ShouldContain("Review and approve each change");
    html.ShouldContain("Always allow edits for this conversation");
    TagOf(html, "AskBeforeEditing").ShouldContain("role=\"menuitemradio\"");
    TagOf(html, "AskAutomaticallyEdit").ShouldContain("role=\"menuitemradio\"");
    IsChecked(html, "AskBeforeEditing").ShouldBeTrue();
    IsChecked(html, "AskAutomaticallyEdit").ShouldBeFalse();
  }

  public static async Task Reflect_The_Mode_SetEditMode_Chose()
  {
    using PillSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit));
    string html = await RenderAsync(scope);

    scope.Store.GetState<AgentSurfaceState>().EditMode.ShouldBe(AgentEditMode.AutomaticallyEdit);
    string pill = TagOf(html, "AskEditMode");
    pill.ShouldContain("aria-label=\"Edit mode: Automatically edit\"");
    pill.ShouldContain("data-mode=\"AutomaticallyEdit\"");
    html.ShouldContain("Auto-edit");
    html.ShouldNotContain(">Ask first<");
    IsChecked(html, "AskAutomaticallyEdit").ShouldBeTrue();
    IsChecked(html, "AskBeforeEditing").ShouldBeFalse();

    await scope.Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AskBeforeEditing));
    html = await RenderAsync(scope);
    TagOf(html, "AskEditMode").ShouldContain("data-mode=\"AskBeforeEditing\"");
    IsChecked(html, "AskBeforeEditing").ShouldBeTrue();
  }

  private static async Task<string> RenderAsync(SpaTestScope scope)
  {
    IServiceProvider services = scope.ServiceProvider;
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<AskEditModePill>()).ToHtmlString());
  }

  private static string TagOf(string html, string dataQa)
  {
    string? tag = OpeningTag().Matches(html)
      .Select(static match => match.Value)
      .FirstOrDefault(value => value.Contains($"data-qa=\"{dataQa}\"", StringComparison.Ordinal));
    tag.ShouldNotBeNull(dataQa);
    return tag;
  }

  // Fluent renders a checked item with a bare checked attribute (or checked="").
  private static bool IsChecked(string html, string dataQa) =>
    CheckedAttribute().IsMatch(TagOf(html, dataQa));

  [GeneratedRegex("<[^>]+>")]
  private static partial Regex OpeningTag();

  [GeneratedRegex("\\schecked(\\s|=|>|/)")]
  private static partial Regex CheckedAttribute();

  /// <summary>In-proc SPA container; the pill needs only state and FluentUI.</summary>
  private sealed class PillSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public PillSpa()
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

  /// <summary>The pill renders without interop; any JS call is a test failure.</summary>
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
