#region Purpose
// Admin → Authentication edits a command copied from the stored site-settings response.
#endregion

#region Design
// HtmlRenderer's static host reports IsInteractive false, so it never runs the page's fetch.
// The shared InteractiveHtmlRenderer (infrastructure/) reports an interactive renderer and
// renders AuthenticationPage in the same scope that can seed or update the snapshot. The form attributes
// data-policy-version, data-entra-allow-bootstrap, and the accepted-tenant rows are the command.
// Command.From(SiteSettings) runs after the fetch, on Cancel, and after a successful save.
// A later SiteSettingsState arrives through ShouldReRender. An unedited command is replaced.
// An edited command stays and takes the new version when the policy fields are unchanged.
// Look up, Add, and Remove change the command. The stored response's tenant list stays put
// until Save. Save's own handler accepts the saved response with force, because ShouldReRender
// leaves a dirty command on the old version.
#endregion

namespace AuthenticationPageDraft_;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Admin.SiteSettings;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Unit")]
public class AuthenticationDraft_Should_
{
  private static readonly Guid TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AuthenticationDraft_Should_>();

  public static async Task Copy_The_Response_Into_The_Command_On_Open()
  {
    ScriptedWebServer server = new()
    {
      Settings = Settings(version: 4, tenants: [new EntraAcceptedTenant(TenantId, "contoso.com")]),
    };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope);

    html.ShouldContain("data-policy-version=\"4\"");
    html.ShouldContain("data-entra-sign-in-enabled=\"true\"");
    html.ShouldContain("data-entra-allow-bootstrap=\"false\"");
    html.ShouldContain("data-qa=\"AuthenticationCancel\"");
    html.ShouldContain(AuthenticationPage.FormatAcceptedTenant(new EntraAcceptedTenant(TenantId, "contoso.com")));
    scope.Store.GetState<SiteSettingsState>().SiteSettings.ShouldNotBeNull().EntraAcceptedTenants
      .ShouldBe([new EntraAcceptedTenant(TenantId, "contoso.com")]);
  }

  public static async Task Cancel_Restores_The_Snapshot()
  {
    ScriptedWebServer server = new() { Settings = Settings(version: 3) };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope, page =>
    {
      SetAllowBootstrap(page, true);
      Invoke(page, "HandleCancel");
      return Task.CompletedTask;
    });

    html.ShouldContain("data-entra-allow-bootstrap=\"false\"");
    html.ShouldContain("data-policy-version=\"3\"");
  }

  public static async Task Apply_A_Looked_Up_Tenant_To_The_Command()
  {
    ScriptedWebServer server = new()
    {
      Settings = Settings(version: 2),
      Lookups = [new ResolveEntraTenant.Response(TenantId, "contoso.com")],
    };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope, page =>
    {
      SetText(page, "DomainDraft", "contoso.com");
      return InvokeAsync(page, "LookUpAsync");
    });

    html.ShouldContain(AuthenticationPage.FormatAcceptedTenant(new EntraAcceptedTenant(TenantId, "contoso.com")));
    SiteSettingsState state = scope.Store.GetState<SiteSettingsState>();
    state.ResolvedTenant.ShouldBe(new EntraAcceptedTenant(TenantId, "contoso.com"));
    state.SiteSettings.ShouldNotBeNull().EntraAcceptedTenants.ShouldBeEmpty();
    server.LookupCalls.ShouldBe(1);
  }

  public static async Task Add_And_Remove_Edit_The_Command()
  {
    ScriptedWebServer server = new() { Settings = Settings(version: 2) };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    string formatted = AuthenticationPage.FormatAcceptedTenant(EntraAcceptedTenant.IdOnly(TenantId));

    string added = await RenderAsync(scope, page =>
    {
      SetText(page, "GuidDraft", TenantId.ToString("D"));
      Invoke(page, "AddTenant");
      return Task.CompletedTask;
    });
    added.ShouldContain(formatted);
    scope.Store.GetState<SiteSettingsState>().SiteSettings.ShouldNotBeNull().EntraAcceptedTenants.ShouldBeEmpty();

    string removed = await RenderAsync(scope, page =>
    {
      SetText(page, "GuidDraft", TenantId.ToString("D"));
      Invoke(page, "AddTenant");
      Invoke(page, "RemoveTenant", TenantId);
      return Task.CompletedTask;
    });
    removed.ShouldNotContain(formatted);
  }

  public static async Task Keep_An_Unsaved_Edit_And_Take_The_New_Version()
  {
    ScriptedWebServer server = new()
    {
      Settings = Settings(version: 3),
      Renamed = new UpdateAssistantName.Response("Nia", 9),
    };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope, async page =>
    {
      SetAllowBootstrap(page, true);
      await scope.Send
      (
        new SiteSettingsState.UpdateAssistantNameActionSet.Action
        (
          new UpdateAssistantName.Command { AssistantName = "Nia", Version = 3 }
        )
      );
    });

    html.ShouldContain("data-entra-allow-bootstrap=\"true\"");
    html.ShouldContain("data-policy-version=\"9\"");
    html.ShouldContain("data-entra-sign-in-enabled=\"true\"");
  }

  public static async Task Keep_The_Old_Version_When_The_Stored_Policy_Changes()
  {
    ScriptedWebServer server = new()
    {
      Settings = Settings(version: 3),
      Saved = Saved(signIn: false, bootstrap: false, version: 8),
    };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope, async page =>
    {
      SetAllowBootstrap(page, true);
      await scope.Send
      (
        new SiteSettingsState.UpdateSiteSettingsActionSet.Action(PolicyCommand(signIn: false, version: 3))
      );
    });

    html.ShouldContain("data-entra-allow-bootstrap=\"true\"");
    html.ShouldContain("data-entra-sign-in-enabled=\"true\"");
    html.ShouldContain("data-policy-version=\"3\"");
    html.ShouldNotContain("data-policy-version=\"8\"");
  }

  public static async Task Move_The_Form_To_The_Saved_Response()
  {
    ScriptedWebServer server = new()
    {
      Settings = Settings(version: 3),
      Saved = Saved(signIn: true, bootstrap: true, version: 8),
    };
    using DraftSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    string html = await RenderAsync(scope, page =>
    {
      SetAllowBootstrap(page, true);
      return InvokeAsync(page, "HandleValidSubmit");
    });

    html.ShouldContain("data-entra-allow-bootstrap=\"true\"");
    html.ShouldContain("data-policy-version=\"8\"");
    scope.Store.GetState<SiteSettingsState>().SiteSettings.ShouldNotBeNull().Version.ShouldBe(8);
  }

  private static GetSiteSettings.Response Settings
  (
    long version,
    List<EntraAcceptedTenant>? tenants = null
  ) =>
    new(
      entraSignInEnabled: true,
      entraAllowBootstrap: false,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: "Akili",
      version: version,
      entraAcceptedTenants: tenants);

  private static UpdateSiteSettings.Response Saved(bool signIn, bool bootstrap, long version) =>
    new(
      entraSignInEnabled: signIn,
      entraAllowBootstrap: bootstrap,
      passkeyPromptMode: PasskeyPromptMode.Soft,
      assistantName: "Akili",
      version: version);

  private static UpdateSiteSettings.Command PolicyCommand(bool signIn, long version) =>
    new()
    {
      EntraSignInEnabled = signIn,
      EntraAllowBootstrap = false,
      PasskeyPromptMode = PasskeyPromptMode.Soft,
      EntraTenantAcceptanceMode = EntraTenantAcceptanceMode.Restricted,
      Version = version,
    };

  private static async Task<string> RenderAsync
  (
    SpaTestScope scope,
    Func<AuthenticationPage, Task>? afterOpen = null
  )
  {
    IServiceProvider services = scope.ServiceProvider;
    PageCapture capture = new();
    await using InteractiveHtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      HtmlRootComponent root = renderer.BeginRenderingComponent(typeof(PageHost), ParameterView.FromDictionary(new Dictionary<string, object?>
      {
        [nameof(PageHost.Capture)] = capture,
      }));
      await root.QuiescenceTask;
      AuthenticationPage page = capture.Page ?? throw new InvalidOperationException("AuthenticationPage did not render.");
      if (afterOpen is not null)
      {
        await afterOpen(page);
        await renderer.Dispatcher.InvokeAsync(() => { });
      }

      return WebUtility.HtmlDecode(root.ToHtmlString());
    });
    return html;
  }

  private static void SetAllowBootstrap(AuthenticationPage page, bool value)
  {
    UpdateSiteSettings.Command command = (UpdateSiteSettings.Command)Property(page, "Command");
    command.EntraAllowBootstrap = value;
  }

  private static void SetText(AuthenticationPage page, string propertyName, string value)
  {
    PropertyInfo property = PropertyInfoOf(page, propertyName);
    property.SetValue(page, value);
  }

  private static object Property(AuthenticationPage page, string name)
  {
    PropertyInfo property = PropertyInfoOf(page, name);
    return property.GetValue(page) ?? throw new InvalidOperationException(name);
  }

  private static PropertyInfo PropertyInfoOf(AuthenticationPage page, string name) =>
    page.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
      ?? throw new InvalidOperationException(name);

  private static void Invoke(AuthenticationPage page, string methodName, params object?[] args)
  {
    MethodInfo method = Method(page, methodName, args);
    method.Invoke(page, args);
    StateHasChanged(page);
  }

  private static async Task InvokeAsync(AuthenticationPage page, string methodName)
  {
    MethodInfo method = Method(page, methodName);
    Task task = (Task?)method.Invoke(page, null) ?? throw new InvalidOperationException(methodName);
    await task;
    StateHasChanged(page);
  }

  private static MethodInfo Method(AuthenticationPage page, string methodName, object?[]? args = null)
  {
    Type[] types = args is null ? [] : args.Select(static arg => arg?.GetType() ?? typeof(object)).ToArray();
    MethodInfo? method = types.Length == 0
      ? page.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
      : page.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic, types);
    return method ?? throw new InvalidOperationException(methodName);
  }

  private static void StateHasChanged(AuthenticationPage page)
  {
    MethodInfo render = typeof(ComponentBase).GetMethod
    (
      "StateHasChanged",
      BindingFlags.Instance | BindingFlags.NonPublic
    ) ?? throw new InvalidOperationException("StateHasChanged");
    render.Invoke(page, null);
  }

  private sealed class PageCapture
  {
    public AuthenticationPage? Page { get; set; }
  }

  private sealed class PageHost : ComponentBase
  {
    [Parameter] public PageCapture? Capture { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<AuthenticationPage>(0);
        child.AddComponentReferenceCapture(1, instance =>
        {
          if (Capture is not null)
          {
            Capture.Page = (AuthenticationPage)instance;
          }
        });
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
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

  private sealed class ScriptedWebServer : IWebServerApiService
  {
    public GetSiteSettings.Response? Settings { get; set; }

    public UpdateAssistantName.Response? Renamed { get; set; }

    public UpdateSiteSettings.Response? Saved { get; set; }

    public IReadOnlyList<ResolveEntraTenant.Response> Lookups { get; set; } = [];

    public int LookupCalls { get; private set; }

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
        outcome = Settings;
      }
      else if (typeof(TResponse) == typeof(UpdateAssistantName.Response))
      {
        outcome = Renamed;
      }
      else if (typeof(TResponse) == typeof(UpdateSiteSettings.Response))
      {
        outcome = Saved;
      }
      else if (typeof(TResponse) == typeof(ResolveEntraTenant.Response))
      {
        outcome = Lookups[LookupCalls];
        LookupCalls++;
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
