#region Purpose
// The Authentication look-up action adds the server's tenant id and domain to the draft list.
#endregion

#region Design
// In-proc SPA container, same mediator and state registrations as StaticRenderHost, with a
// scripted IWebServerApiService. The action is sent as its concrete type. The scripted
// response is the only tenant id the client stores: this test never calls Microsoft.
#endregion

namespace AuthenticationTenantLookup_;

using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Unit")]
public class ResolveEntraTenant_Action_Should_
{
  private static readonly Guid TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ResolveEntraTenant_Action_Should_>();

  public static async Task Add_The_Server_Tenant_And_Domain_To_The_Draft_List()
  {
    var server = new ScriptedWebServer(
      new ResolveEntraTenant.Response(TenantId, "contoso.com"));
    await using ServiceProvider provider = Host(server);
    using SpaTestScope scope = SpaTestScope.Create(new ScopeHost(provider));

    await scope.Send(new SiteSettingsState.ResolveEntraTenantActionSet.Action("contoso.com"));

    SiteSettingsState state = scope.Store.GetState<SiteSettingsState>();
    state.DraftAcceptedTenants.ShouldBe([new EntraAcceptedTenant(TenantId, "contoso.com")]);
    state.AcceptedTenantsMessage.ShouldBeNull();
    server.Calls.ShouldBe(1);
  }

  public static async Task Skip_The_Server_When_The_Domain_Is_Invalid()
  {
    var server = new ScriptedWebServer(
      new ResolveEntraTenant.Response(TenantId, "contoso.com"));
    await using ServiceProvider provider = Host(server);
    using SpaTestScope scope = SpaTestScope.Create(new ScopeHost(provider));

    await scope.Send(new SiteSettingsState.ResolveEntraTenantActionSet.Action("not a domain"));

    SiteSettingsState state = scope.Store.GetState<SiteSettingsState>();
    state.DraftAcceptedTenants.ShouldBeEmpty();
    state.AcceptedTenantsMessage.ShouldBe("'not a domain' is not a domain name.");
    server.Calls.ShouldBe(0);
  }

  private static ServiceProvider Host(ScriptedWebServer server)
  {
    ServiceCollection services = StaticRenderHost.CreateServices();
    services.AddScoped<IWebServerApiService>(_ => server);
    return services.BuildServiceProvider();
  }

  private sealed class ScopeHost(IServiceProvider serviceProvider) : ISpaTestApplication
  {
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
  }

  private sealed class ScriptedWebServer(object outcome) : IWebServerApiService
  {
    public int Calls { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>(
      IApiRequest request,
      CancellationToken cancellationToken) where TResponse : class
    {
      ArgumentNullException.ThrowIfNull(request);
      cancellationToken.ThrowIfCancellationRequested();
      Calls++;
      OneOf<TResponse, FileResponse, SharedProblemDetails> result = outcome switch
      {
        TResponse response => response,
        SharedProblemDetails problem => problem,
        _ => new SharedProblemDetails { Status = 500, Title = "No scripted outcome." }
      };
      return Task.FromResult(result);
    }
  }
}
