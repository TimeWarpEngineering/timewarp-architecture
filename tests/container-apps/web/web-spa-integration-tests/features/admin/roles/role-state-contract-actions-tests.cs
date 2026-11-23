#region Purpose
// RoleState's [ContractState] actions (task 483) at run time: the generated GetRoles, SetRolePermissions and
// SetRoleAgentScopes dispatch through Mediator, the fetch seeds the drafts, and a save sends the seeded draft.
#endregion

#region Design
// In-proc SPA container with a scripted IWebServerApiService, the same shape as the ClearXaiApiKey confirmation tests.
// The generated dispatch methods are called on the store's RoleState, the way a page calls them. Mediator never saw the
// generated Actions as source, so a passing dispatch proves the [ContractState] declaration reached its Send arms.
// The save case is 464 §4f at run time: the page passes only RoleId, and the request carries the seeded permissions
// plus the one toggle, not an empty list. The page's own Command is not changed by the send.
#endregion

namespace RoleStateContractActions_;

using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Unit")]
public class RoleState_Contract_Actions_Should
{
  private static readonly Guid RoleId = Guid.Parse("5b0c4a0e-7c6b-4d43-9a54-1f2d1c0a9e01");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RoleState_Contract_Actions_Should>();

  public static async Task GetRoles_Should_Store_The_List_And_Seed_Both_Drafts()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Store.GetState<RoleState>().GetRoles();

    RoleState state = scope.Store.GetState<RoleState>();
    state.RoleList.ShouldNotBeNull().Select(role => role.RoleId).ShouldBe([RoleId]);
    state.GetDraftPermissionIds(RoleId).ShouldBe([PermissionIds.AdminRolesRead], ignoreOrder: true);
    state.GetDraftAgentScopes(RoleId).ShouldBe(["scope.read"], ignoreOrder: true);
    GetRoles.Query sent = server.Requests.OfType<GetRoles.Query>().ShouldHaveSingleItem();
    sent.UserId.ShouldNotBe(Guid.Empty);
  }

  public static async Task SetRolePermissions_Should_Send_The_Seeded_Draft()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Store.GetState<RoleState>().GetRoles();
    await scope.Send(new RoleState.SetPermissionSelectedActionSet.Action(RoleId, PermissionIds.AdminRolesManage, selected: true));

    SetRolePermissions.Command page = new() { RoleId = RoleId };
    await scope.Store.GetState<RoleState>().SetRolePermissions(page);

    SetRolePermissions.Command sent = server.Requests.OfType<SetRolePermissions.Command>().ShouldHaveSingleItem();
    sent.RoleId.ShouldBe(RoleId);
    sent.PermissionIds.ShouldBe([PermissionIds.AdminRolesRead, PermissionIds.AdminRolesManage], ignoreOrder: true);
    sent.UserId.ShouldNotBe(Guid.Empty);
    sent.ShouldNotBeSameAs(page);
    page.PermissionIds.ShouldBeEmpty();
    scope.Store.GetState<RoleState>().LastSetRolePermissionsSucceeded.ShouldBeTrue();
  }

  public static async Task SetRoleAgentScopes_Should_Send_The_Seeded_Draft()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Store.GetState<RoleState>().GetRoles();

    await scope.Store.GetState<RoleState>().SetRoleAgentScopes(new SetRoleAgentScopes.Command { RoleId = RoleId });

    SetRoleAgentScopes.Command sent = server.Requests.OfType<SetRoleAgentScopes.Command>().ShouldHaveSingleItem();
    sent.Scopes.ShouldBe(["scope.read"]);
    scope.Store.GetState<RoleState>().LastSetRoleAgentScopesSucceeded.ShouldBeTrue();
  }

  /// <summary>Answers the three Roles contracts and records every request it receives.</summary>
  private sealed class ScriptedWebServer : IWebServerApiService
  {
    public List<IApiRequest> Requests { get; } = [];

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      cancellationToken.ThrowIfCancellationRequested();
      Requests.Add(request);
      object response = request switch
      {
        GetRoles.Query => new GetRoles.Response
        (
          totalCount: 1,
          [new GetRoles.RoleDto(RoleId, "Editor", "Edits things.", [PermissionIds.AdminRolesRead], ["scope.read"])]
        ),
        SetRolePermissions.Command command => new SetRolePermissions.Response(command.PermissionIds),
        SetRoleAgentScopes.Command command => new SetRoleAgentScopes.Response(command.Scopes),
        _ => throw new InvalidOperationException($"No scripted response for {request.GetType().FullName}."),
      };
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>((TResponse)response);
    }
  }

  /// <summary>In-proc SPA container over the scripted web server.</summary>
  private sealed class RoleSpa(IWebServerApiService WebServer) : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; } = Build(WebServer);

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private static ServiceProvider Build(IWebServerApiService webServer)
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
            typeof(IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped(_ => webServer);
      return services.BuildServiceProvider();
    }
  }
}
