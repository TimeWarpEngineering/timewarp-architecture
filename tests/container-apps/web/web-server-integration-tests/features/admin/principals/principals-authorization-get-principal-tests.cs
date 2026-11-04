#region Purpose
// End-to-end proof that GetPrincipal requires admin.principals.read and gates profile PII on
// admin.profiles.read (task 328).
#endregion

#region Design
// Mirrors roles-authorization-tests: real HTTP, isolated clients, passkey cookie mint, grant via
// IPrincipalRoleStore. Task 328: GetPrincipal shares admin.principals.read (401 anonymous, 403
// Member, 200 Administrator with the profile). PII gating uses the reachable shape: a
// non-Administrator role (Operator) granted admin.access + admin.principals.read without
// admin.profiles.read gets 200 from List and Get, but no display name or email. Administrator
// cannot drop admin.profiles.read (protected core). List and SetRoles are in
// principals-authorization-list-set-roles-tests.
#endregion

namespace PrincipalsAuthorization_;

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Identity;
using static PrincipalsAuthorization_.PrincipalsAuthorizationFixture;

public class Get_Principal_Returns_
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
  internal static void Register() => RegisterTests<Get_Principal_Returns_>();

  public static async Task Unauthorized_Given_Anonymous_GetPrincipal()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync($"api/admin/principals/{Guid.NewGuid()}");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_GetPrincipal_Given_Passkey_Member_Only_Session()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);
    await ForceMemberOnlyAsync(Web, principalId);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage response = await client.GetAsync($"api/admin/principals/{principalId}");

    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_GetPrincipal_Given_Administrator_Includes_Profile()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);
    string email = await EnsureProfileAsync(scope, principalId);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage response = await client.GetAsync($"api/admin/principals/{principalId}");

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetPrincipal.Response detail = await ReadAsync<GetPrincipal.Response>(response);
    detail.Principal.PrincipalId.Value.ShouldBe(principalId.Value);
    detail.Principal.RoleIds.ShouldContain(RoleIds.Administrator);
    detail.Principal.Profile.ShouldNotBeNull();
    detail.Principal.Profile.DisplayName.ShouldBe("Integration Admin");
    detail.Principal.Profile.Email.ShouldBe(email);

    ListPrincipals.Response list = await ReadAsync<ListPrincipals.Response>(
      await client.GetAsync(new ListPrincipals.Query { UserId = Guid.NewGuid(), Search = "integration admin" }.GetRouteWithQueryString()));
    list.Items.Select(item => item.PrincipalId.Value).ShouldContain(principalId.Value);
    list.Items.Single(item => item.PrincipalId.Value == principalId.Value).Email.ShouldBe(email);
  }

  public static async Task Ok_Principals_Reader_Without_Profiles_Read_Sees_No_Profile_Fields()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie(Web);
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    IReadOnlyList<Guid> originalRoleIds = await roleStore.GetRoleIdsAsync(principalId);
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Operator]);
    string email = await EnsureProfileAsync(scope, principalId);

    IReadOnlyList<string> originalOperatorPermissions =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Operator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Operator,
        [.. originalOperatorPermissions, PermissionIds.AdminAccess, PermissionIds.AdminPrincipalsRead]);

      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

      HttpResponseMessage listResponse = await client.GetAsync(
        new ListPrincipals.Query { UserId = Guid.NewGuid() }.GetRouteWithQueryString());
      listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
      string listJson = await listResponse.Content.ReadAsStringAsync();
      listJson.ShouldNotContain(email);
      listJson.ShouldNotContain("Integration Admin");
      ListPrincipals.Response list = JsonSerializer
        .Deserialize<ListPrincipals.Response>(listJson, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();
      list.Items.ShouldAllBe(item => item.DisplayName == null && item.Email == null);

      HttpResponseMessage searchResponse = await client.GetAsync(
        new ListPrincipals.Query { UserId = Guid.NewGuid(), Search = "integration" }.GetRouteWithQueryString());
      searchResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

      HttpResponseMessage getResponse = await client.GetAsync($"api/admin/principals/{principalId}");
      getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
      string getJson = await getResponse.Content.ReadAsStringAsync();
      getJson.ShouldNotContain(email);
      JsonSerializer.Deserialize<GetPrincipal.Response>(getJson, ContractSerializationDefaults.Options)
        .ShouldNotBeNull().Principal.Profile.ShouldBeNull();
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Operator, originalOperatorPermissions);
      await roleStore.SetRoleIdsAsync(principalId, [.. originalRoleIds]);
    }
  }
}
