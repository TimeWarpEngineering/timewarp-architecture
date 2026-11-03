#region Purpose
// End-to-end proof that ListPrincipals / SetPrincipalRoles require permissions, last-administrator
// protection, and the agent bearer refusal (task 182-002).
#endregion

#region Design
// Mirrors roles-authorization-tests: real HTTP, isolated clients, passkey cookie mint, grant via
// IPrincipalRoleStore, then assert 403 → 200. Read-without-manage: shrink Administrator permission
// set to principals.read only → List 200, Set 403. Agent bearer remains 401 (cookie-scheme policy).
// GetPrincipal and profile PII gating are in principals-authorization-get-principal-tests.
#endregion

namespace PrincipalsAuthorization_;

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Identity;
using static PrincipalsAuthorization_.PrincipalsAuthorizationFixture;

public class List_And_Set_Roles_Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  public static async Task SetupOnce() => Graph = await PrincipalsAuthorizationFixture.CreateHostAsync();

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<List_And_Set_Roles_Returns_>();

  public static async Task Unauthorized_Given_Anonymous_List()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/admin/principals");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_Given_Passkey_Member_Only_Session()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);
    // Task 180: first human passkey claims Administrator — force Member-only for this case.
    await ForceMemberOnlyAsync(Web, principalId);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new ListPrincipals.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_List_Given_Administrator_Via_Role_Store()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);

    // Scope required: under postgres IPrincipalRoleStore is scoped (EfPrincipalRoleStore).
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    var query = new ListPrincipals.Query { UserId = Guid.NewGuid() };
    HttpResponseMessage response = await client.GetAsync(query.GetRouteWithQueryString());

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain(principalId.ToString());
  }

  public static async Task Ok_SetRoles_Given_Administrator_Via_Role_Store()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();

    // Peer admin so demoting the caller is allowed (last-admin guard, task 182-004).
    Principal secondAdmin = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(secondAdmin);
    await EnsureOnlyAdminsAsync(roleStore, principalStore, principalId, secondAdmin.Id);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    string body = JsonSerializer.Serialize(
      new
      {
        userId = Guid.NewGuid(),
        roleIds = new[] { RoleIds.Member, RoleIds.Developer }
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage response = await client.PutAsync(
      $"api/admin/principals/{principalId}/roles",
      new StringContent(body, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    IReadOnlyList<Guid> stored = await roleStore.GetRoleIdsAsync(principalId);
    stored.ShouldContain(RoleIds.Developer);
    stored.ShouldContain(RoleIds.Member);
  }

  public static async Task Conflict_SetRoles_When_Demoting_Last_Administrator()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    // Sole admin among all host-graph principals (prior tests leave leftover admins).
    await EnsureOnlyAdminsAsync(roleStore, principalStore, principalId);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    // No second admin: stripping Administrator must 409 (last-admin).
    string body = JsonSerializer.Serialize(
      new
      {
        userId = Guid.NewGuid(),
        roleIds = new[] { RoleIds.Member, RoleIds.Developer }
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage response = await client.PutAsync(
      $"api/admin/principals/{principalId}/roles",
      new StringContent(body, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("Last administrator");

    // Store unchanged.
    IReadOnlyList<Guid> stored = await roleStore.GetRoleIdsAsync(principalId);
    stored.ShouldContain(RoleIds.Administrator);
  }

  public static async Task Ok_SetRoles_Demote_One_Admin_When_Two_Exist()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();

    Principal peerAdmin = Principal.Create(PrincipalKind.Human);
    await principalStore.AddPrincipalAsync(peerAdmin);
    await EnsureOnlyAdminsAsync(roleStore, principalStore, principalId, peerAdmin.Id);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    string body = JsonSerializer.Serialize(
      new
      {
        userId = Guid.NewGuid(),
        roleIds = new[] { RoleIds.Member }
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage response = await client.PutAsync(
      $"api/admin/principals/{principalId}/roles",
      new StringContent(body, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    IReadOnlyList<Guid> stored = await roleStore.GetRoleIdsAsync(principalId);
    stored.ShouldBe([RoleIds.Member]);
  }

  public static async Task Forbidden_SetRoles_Given_Administrator_With_Principals_Read_Only()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    IReadOnlyList<string> originalAdminPermissions =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Administrator,
        [
          PermissionIds.AdminAccess,
          PermissionIds.AdminRolesRead,
          PermissionIds.AdminPrincipalsRead,
          PermissionIds.ProfileRead,
          PermissionIds.SettingsRead,
        ]);

      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

      var query = new ListPrincipals.Query { UserId = Guid.NewGuid() };
      HttpResponseMessage listResponse = await client.GetAsync(query.GetRouteWithQueryString());
      listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

      string body = JsonSerializer.Serialize(
        new
        {
          userId = Guid.NewGuid(),
          roleIds = new[] { RoleIds.Member, RoleIds.Developer }
        },
        ContractSerializationDefaults.Options);

      HttpResponseMessage setResponse = await client.PutAsync(
        $"api/admin/principals/{principalId}/roles",
        new StringContent(body, Encoding.UTF8, "application/json"));
      setResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Administrator, originalAdminPermissions);
    }
  }

  public static async Task Unauthorized_Given_Agent_Bearer_Token_No_Cookie()
  {
    string accessToken = await RegisterAndIssueAgentBearerToken(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    HttpResponseMessage response = await client.GetAsync("api/admin/principals");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }
}
