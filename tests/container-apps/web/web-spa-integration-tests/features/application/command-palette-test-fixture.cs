#region Purpose
// Shared private helpers and nested test doubles (probes, PaletteSpa) for the CommandPalette_Should_ partial class.
#endregion

namespace CommandPalette_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Spa;

[TestTag("Unit")]
public partial class CommandPalette_Should_
{

  private static async Task<string> RenderShellModalsAsync(IServiceProvider services, string? activeModalId)
  {
    await using HtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () =>
    {
      ParameterView parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
      {
        [nameof(ModalController.ActiveModalId)] = activeModalId,
        [nameof(ModalController.ModalContainers)] = (RenderFragment)(builder =>
        {
          builder.OpenComponent<CommandPalette>(0);
          builder.CloseComponent();
        }),
      });
      return (await renderer.RenderComponentAsync<ModalController>(parameters)).ToHtmlString();
    });
  }

  private static ActionRosterEntry Probe(string name, Type actionType) =>
    new(name, typeof(CounterState), actionType, 0, null, static (_, _) => Task.CompletedTask);

  private static ActionCatalogEntry Metadata(string name, Type actionType, string? displayName, ActionVisibility visibility = ActionVisibility.Human) =>
    new
    (
      name,
      "Run the probe.",
      [],
      visibility,
      typeof(CounterState),
      actionType,
      [],
      "{}",
      static (_, _, _) => Task.CompletedTask,
      displayName
    );

  // Distinct probe Action types: roster rows join catalog metadata by Action type.
  private sealed class ProbeOnce;

  private sealed class ProbeTwice;

  private sealed class AskOverride : IActionDisplayOverride
  {
    public ActionDisplay? Find(ActionRosterEntry action) =>
      action.Name == "AgentSurface.OpenAskPanel" ? new ActionDisplay("Ask Akili", "Open the docked assistant panel.") : null;
  }

  private static CommandPaletteRow Row(string name, string description) =>
    new(name, description, CommandPaletteRowKind.Page, "/" + name);

  /// <summary>In-proc SPA container with a fixed principal; <c>null</c> permissions = anonymous.</summary>
  private sealed class PaletteSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal User { get; }

    public PaletteSpa(string[]? permissions)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
          [
            new Claim("sub", Guid.NewGuid().ToString()),
            .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
          ],
          authenticationType: "test"));

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
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(User));
      services.AddScoped(_ => A.Fake<IJSRuntime>());
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

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }
}
