#region Purpose
// End-to-end proof of SetRolePermissions protected-core (Administrator) and happy path (182-004).
#endregion

#region Design
// Real HTTP + passkey cookie, same isolation posture as roles-authorization-tests. Protected-core
// is a handler 409 (not store): stripping admin.roles.manage from Administrator must fail and
// leave the store unchanged. Non-Administrator roles (Member) may clear permissions (200).
#endregion

namespace SetRolePermissionsLockout_;

using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Conflict_When_Stripping_Core_Admin_Permission_From_Administrator()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    IReadOnlyList<string> before =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);

    // Full admin set minus admin.roles.manage — protected-core must reject.
    List<string> withoutManage =
    [
      PermissionIds.AdminAccess,
      PermissionIds.AdminRolesRead,
      PermissionIds.AdminPrincipalsRead,
      PermissionIds.AdminPrincipalsManage,
      PermissionIds.ProfileRead,
      PermissionIds.SettingsRead,
    ];

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    string body = JsonSerializer.Serialize(
      new { userId = Guid.NewGuid(), permissionIds = withoutManage },
      ContractSerializationDefaults.Options);

    HttpResponseMessage response = await client.PutAsync(
      $"api/Roles/{RoleIds.Administrator}/permissions",
      new StringContent(body, Encoding.UTF8, "application/json"));

    response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("Protected core");
    json.ShouldContain(PermissionIds.AdminRolesManage);

    IReadOnlyList<string> after =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    after.Order().ShouldBe(before.Order());
  }

  public static async Task Ok_When_Setting_Member_Permissions()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    IReadOnlyList<string> originalMember =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Member);
    try
    {
      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

      string[] desired = [PermissionIds.ProfileRead, PermissionIds.SettingsRead, PermissionIds.DeveloperAccess];
      string body = JsonSerializer.Serialize(
        new { userId = Guid.NewGuid(), permissionIds = desired },
        ContractSerializationDefaults.Options);

      HttpResponseMessage response = await client.PutAsync(
        $"api/Roles/{RoleIds.Member}/permissions",
        new StringContent(body, Encoding.UTF8, "application/json"));

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      string json = await response.Content.ReadAsStringAsync();
      json.ShouldContain(PermissionIds.DeveloperAccess);

      IReadOnlyList<string> stored =
        await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Member);
      stored.ShouldContain(PermissionIds.DeveloperAccess);
      stored.ShouldContain(PermissionIds.ProfileRead);
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Member, originalMember);
    }
  }

  public static async Task Ok_When_Administrator_Keeps_All_Core_Permissions()
  {
    (string sessionCookie, PrincipalId principalId) = await MintIdentitySessionCookie();

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);

    IReadOnlyList<string> original =
      await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    try
    {
      // Core admin + self-service + developer (extra grant allowed).
      List<string> withExtra =
      [
        .. RolePermissionSeed.AdminPermissions,
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
        PermissionIds.DeveloperAccess,
      ];

      using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

      string body = JsonSerializer.Serialize(
        new { userId = Guid.NewGuid(), permissionIds = withExtra },
        ContractSerializationDefaults.Options);

      HttpResponseMessage response = await client.PutAsync(
        $"api/Roles/{RoleIds.Administrator}/permissions",
        new StringContent(body, Encoding.UTF8, "application/json"));

      response.StatusCode.ShouldBe(HttpStatusCode.OK);
      IReadOnlyList<string> stored =
        await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
      stored.ShouldContain(PermissionIds.DeveloperAccess);
      foreach (string core in RolePermissionSeed.AdminPermissions)
      {
        stored.ShouldContain(core);
      }
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Administrator, original);
    }
  }

  private static async Task<(string Cookie, PrincipalId PrincipalId)> MintIdentitySessionCookie()
  {
    var authenticator = new IntegrationSoftwareAuthenticator();
    var testApiService = new TestApiService(Web.HttpClient, ContractSerializationDefaults.Options);

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

    byte[] authenticatorData = authenticator.BuildAuthenticatorData("localhost", includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    HttpResponseMessage registerResponse = await testApiService.GetHttpResponseMessage(registerCommand, CancellationToken.None);
    registerResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string? sessionCookie = setCookieValues!.FirstOrDefault
      (value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected the passkey registration to issue an identity-session cookie.");

    string cookieHeader = sessionCookie.Split(';')[0];

    using HttpClient sessionClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    sessionClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    HttpResponseMessage sessionResponse = await sessionClient.GetAsync(GetCurrentSession.Query.RouteTemplate);
    sessionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string sessionJson = await sessionResponse.Content.ReadAsStringAsync();
    GetCurrentSession.Response session =
      JsonSerializer.Deserialize<GetCurrentSession.Response>(sessionJson, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetCurrentSession deserialized to null.");
    session.IsAuthenticated.ShouldBeTrue();
    session.PrincipalId.ShouldNotBeNull();

    return (cookieHeader, session.PrincipalId.Value);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
