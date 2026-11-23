#region Purpose
// RoleState contract actions at run time: GetRoles stores the list, GetRole stores the response,
// and a save sends the permission or scope list the caller put on the command.
#endregion

#region Design
// In-proc SPA container with a scripted IWebServerApiService, the same shape as the ClearXaiApiKey confirmation tests.
// The dispatch methods are called on the store's RoleState, the way a page calls them. Mediator never saw the
// generated Actions as source, so a passing dispatch proves the [ContractState] declaration reached its Send arms.
// The save case sends the page's lists. The store has no permission or agent-scope draft. The page's own
// Command is not the instance that is sent. UpdateRole keeps the snapshot's permission and scope lists.
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

  public static async Task GetRoles_Should_Store_The_List()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Store.GetState<RoleState>().GetRoles();

    RoleState state = scope.Store.GetState<RoleState>();
    state.RoleList.ShouldNotBeNull().Select(role => role.RoleId).ShouldBe([RoleId]);
    state.RoleSnapshot.ShouldBeNull();
    GetRoles.Query sent = server.Requests.OfType<GetRoles.Query>().ShouldHaveSingleItem();
    sent.UserId.ShouldNotBe(Guid.Empty);
  }

  public static async Task GetRole_Should_Store_The_Response()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Store.GetState<RoleState>().GetRole(new GetRole.Query { RoleId = RoleId });

    GetRole.Response stored = scope.Store.GetState<RoleState>().RoleSnapshot.ShouldNotBeNull();
    stored.RoleId.ShouldBe(RoleId);
    stored.Name.ShouldBe("Editor");
    stored.Description.ShouldBe("Edits things.");
    stored.Permissions.ShouldBe([PermissionIds.AdminRolesRead]);
    stored.AgentScopes.ShouldBe(["scope.read"]);
    GetRole.Query sent = server.Requests.OfType<GetRole.Query>().ShouldHaveSingleItem();
    sent.RoleId.ShouldBe(RoleId);
    sent.UserId.ShouldNotBe(Guid.Empty);

    UpdateRole.Command copy = UpdateRole.Command.From(stored);
    copy.RoleId.ShouldBe(RoleId);
    copy.Name.ShouldBe("Editor");
    copy.Name = "draft";
    scope.Store.GetState<RoleState>().RoleSnapshot!.Name.ShouldBe("Editor");
  }

  public static async Task UpdateRole_Should_Keep_Permissions_And_Replace_The_Name()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Store.GetState<RoleState>().GetRole(new GetRole.Query { RoleId = RoleId });

    UpdateRole.Command page = UpdateRole.Command.From(scope.Store.GetState<RoleState>().RoleSnapshot!);
    page.Name = "Copy editor";
    await scope.Store.GetState<RoleState>().UpdateRole(page);

    GetRole.Response stored = scope.Store.GetState<RoleState>().RoleSnapshot.ShouldNotBeNull();
    stored.Name.ShouldBe("Copy editor");
    stored.Description.ShouldBe("Edits things.");
    stored.Permissions.ShouldBe([PermissionIds.AdminRolesRead]);
    stored.AgentScopes.ShouldBe(["scope.read"]);
    UpdateRole.Command sent = server.Requests.OfType<UpdateRole.Command>().ShouldHaveSingleItem();
    sent.ShouldNotBeSameAs(page);
    sent.Name.ShouldBe("Copy editor");
    sent.UserId.ShouldNotBe(Guid.Empty);
    page.Name.ShouldBe("Copy editor");
  }

  public static async Task SetRolePermissions_Should_Send_The_Page_List()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Store.GetState<RoleState>().GetRole(new GetRole.Query { RoleId = RoleId });

    SetRolePermissions.Command page = new()
    {
      RoleId = RoleId,
      PermissionIds = [PermissionIds.AdminRolesRead, PermissionIds.AdminRolesManage]
    };
    await scope.Store.GetState<RoleState>().SetRolePermissions(page);

    SetRolePermissions.Command sent = server.Requests.OfType<SetRolePermissions.Command>().ShouldHaveSingleItem();
    sent.RoleId.ShouldBe(RoleId);
    sent.PermissionIds.ShouldBe([PermissionIds.AdminRolesRead, PermissionIds.AdminRolesManage], ignoreOrder: true);
    sent.UserId.ShouldNotBe(Guid.Empty);
    sent.ShouldNotBeSameAs(page);
    page.PermissionIds.Count.ShouldBe(2);
    RoleState state = scope.Store.GetState<RoleState>();
    state.LastSetRolePermissionsSucceeded.ShouldBeTrue();
    state.RoleSnapshot.ShouldNotBeNull().Permissions.ShouldBe(
      [PermissionIds.AdminRolesRead, PermissionIds.AdminRolesManage],
      ignoreOrder: true);
    state.RoleSnapshot.AgentScopes.ShouldBe(["scope.read"]);
  }

  public static async Task SetRoleAgentScopes_Should_Send_The_Page_List()
  {
    ScriptedWebServer server = new();
    using RoleSpa spa = new(server);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Store.GetState<RoleState>().GetRole(new GetRole.Query { RoleId = RoleId });

    SetRoleAgentScopes.Command page = new()
    {
      RoleId = RoleId,
      Scopes = ["scope.read", "scope.write"]
    };
    await scope.Store.GetState<RoleState>().SetRoleAgentScopes(page);

    SetRoleAgentScopes.Command sent = server.Requests.OfType<SetRoleAgentScopes.Command>().ShouldHaveSingleItem();
    sent.Scopes.ShouldBe(["scope.read", "scope.write"]);
    sent.ShouldNotBeSameAs(page);
    RoleState state = scope.Store.GetState<RoleState>();
    state.LastSetRoleAgentScopesSucceeded.ShouldBeTrue();
    state.RoleSnapshot.ShouldNotBeNull().AgentScopes.ShouldBe(["scope.read", "scope.write"]);
    state.RoleSnapshot.Permissions.ShouldBe([PermissionIds.AdminRolesRead]);
  }

  /// <summary>Answers the Roles contracts these tests send and records every request it receives.</summary>
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
        GetRole.Query => new GetRole.Response
        (
          roleId: RoleId,
          name: "Editor",
          description: "Edits things.",
          permissions: [PermissionIds.AdminRolesRead],
          agentScopes: ["scope.read"]
        ),
        UpdateRole.Command => new UpdateRole.Response(),
        SetRolePermissions.Command command => new SetRolePermissions.Response([.. command.PermissionIds]),
        SetRoleAgentScopes.Command command => new SetRoleAgentScopes.Response([.. command.Scopes]),
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
