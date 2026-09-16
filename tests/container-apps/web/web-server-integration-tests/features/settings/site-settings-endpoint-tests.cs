#region Purpose
// HTTP proof that Get/Update site settings enforce SettingsRead/SettingsWrite and 409 on stale Version.
#endregion

#region Design
// Real HTTP through the ASP.NET pipeline (not in-process mediator). First passkey claims
// Administrator so SettingsWrite is present; ForceMemberOnly strips it for the write-403 case.
// Anonymous offered read is a boolean only.
#endregion

namespace SiteSettingsEndpoints_;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure.CredentialCeremonyHelpers;

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

  public static async Task Anonymous_Get_Settings_Should_401()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/settings");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Anonymous_Offered_Should_Return_Only_Boolean()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/identity/entra/offered");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    json.ShouldContain("offered");
    json.ShouldNotContain("tenant");
    json.ShouldNotContain("bootstrap");
    json.ShouldNotContain("trusted");
    GetEntraSignInOffered.Response? parsed =
      JsonSerializer.Deserialize<GetEntraSignInOffered.Response>(json, ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
  }

  public static async Task Member_Get_Should_200()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    HttpResponseMessage response = await client.GetAsync("api/settings");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  public static async Task Member_Put_Should_403()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member]);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    const string body =
      """{"entraSignInEnabled":false,"entraAllowBootstrap":false,"passkeyPromptMode":"Soft","version":0}""";
    HttpResponseMessage response = await client.PutAsync(
      "api/settings",
      new StringContent(body, Encoding.UTF8, "application/json"));
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Administrator_Put_Stale_Version_Should_409()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await GrantAdministratorAsync(principalId);
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage get = await client.GetAsync("api/settings");
    get.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetSiteSettings.Response current =
      JsonSerializer.Deserialize<GetSiteSettings.Response>(
        await get.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetSiteSettings deserialized to null.");

    string staleBody = JsonSerializer.Serialize(
      new
      {
        entraSignInEnabled = current.EntraSignInEnabled,
        entraAllowBootstrap = current.EntraAllowBootstrap,
        passkeyPromptMode = current.PasskeyPromptMode.ToString(),
        version = current.Version + 100
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage put = await client.PutAsync(
      "api/settings",
      new StringContent(staleBody, Encoding.UTF8, "application/json"));
    put.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    string json = await put.Content.ReadAsStringAsync();
    json.ShouldContain("Concurrency conflict");
  }

  public static async Task Administrator_Put_Should_Persist_And_Advance_Version()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await GrantAdministratorAsync(principalId);
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage get = await client.GetAsync("api/settings");
    get.StatusCode.ShouldBe(HttpStatusCode.OK);
    GetSiteSettings.Response current =
      JsonSerializer.Deserialize<GetSiteSettings.Response>(
        await get.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetSiteSettings deserialized to null.");

    string body = JsonSerializer.Serialize(
      new
      {
        entraSignInEnabled = true,
        entraAllowBootstrap = true,
        passkeyPromptMode = "Required",
        version = current.Version
      },
      ContractSerializationDefaults.Options);

    HttpResponseMessage put = await client.PutAsync(
      "api/settings",
      new StringContent(body, Encoding.UTF8, "application/json"));
    put.StatusCode.ShouldBe(HttpStatusCode.OK);
    UpdateSiteSettings.Response updated =
      JsonSerializer.Deserialize<UpdateSiteSettings.Response>(
        await put.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("UpdateSiteSettings deserialized to null.");
    updated.EntraSignInEnabled.ShouldBeTrue();
    updated.EntraAllowBootstrap.ShouldBeTrue();
    updated.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    updated.Version.ShouldBe(current.Version + 1);
  }

  private static async Task GrantAdministratorAsync(PrincipalId principalId)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);
  }
}
