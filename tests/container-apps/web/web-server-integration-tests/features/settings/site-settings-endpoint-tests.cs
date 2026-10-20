#region Purpose
// HTTP proof that Get/Update site settings enforce SettingsRead/SettingsWrite and 409 on stale Version,
// and that the public assistant identity carries only the name and avatar and follows a rename.
#endregion

#region Design
// Real HTTP through the ASP.NET pipeline (not in-process mediator). First passkey claims
// Administrator so SettingsWrite is present; ForceMemberOnly strips it for the write-403 case.
// Anonymous offered read is a boolean only. Task 361: GetAssistantIdentity is anonymous and carries
// only name and avatar (the exact JSON property set); the rename test restores the name it read
// first, in a finally, so test order and a failed assertion do not matter.
#endregion

namespace SiteSettingsEndpoints_;

using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
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
      """{"entraSignInEnabled":false,"entraAllowBootstrap":false,"passkeyPromptMode":"Soft","assistantName":"Zawadi","version":0}""";
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
        assistantName = current.AssistantName,
        entraTenantAcceptanceMode = current.EntraTenantAcceptanceMode.ToString(),
        entraAcceptedTenantIds = current.EntraAcceptedTenantIds,
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

    Guid acceptedTenant = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    string body = JsonSerializer.Serialize(
      new
      {
        entraSignInEnabled = true,
        entraAllowBootstrap = true,
        passkeyPromptMode = "Required",
        assistantName = current.AssistantName,
        entraTenantAcceptanceMode = "Any",
        entraAcceptedTenantIds = new[] { acceptedTenant },
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
    updated.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Any);
    updated.EntraAcceptedTenantIds.ShouldBe([acceptedTenant]);
    updated.Version.ShouldBe(current.Version + 1);
  }

  public static async Task Anonymous_Assistant_Identity_Should_Return_Only_Name_And_Avatar()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/settings/assistant");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    using (JsonDocument document = JsonDocument.Parse(json))
    {
      document.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
      string[] properties = [.. document.RootElement.EnumerateObject().Select(static property => property.Name).Order(StringComparer.Ordinal)];
      properties.ShouldBe(["avatar", "name"]);
    }

    GetAssistantIdentity.Response? parsed =
      JsonSerializer.Deserialize<GetAssistantIdentity.Response>(json, ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    parsed.Name.ShouldNotBeNullOrWhiteSpace();
    parsed.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
  }

  public static async Task Administrator_Rename_Should_Show_In_The_Public_Identity()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await GrantAdministratorAsync(principalId);
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    using HttpClient anonymous = new() { BaseAddress = Web.HttpClient.BaseAddress };
    GetAssistantIdentity.Response before = await ReadIdentityAsync(anonymous);

    // The host is shared by the class: restore the name read first even when an assertion fails.
    try
    {
      UpdateSiteSettings.Response renamed = await PutAssistantNameAsync(client, "  Zawadi  ");
      renamed.AssistantName.ShouldBe("Zawadi");
      GetAssistantIdentity.Response after = await ReadIdentityAsync(anonymous);
      after.Name.ShouldBe("Zawadi");
      after.Avatar.ShouldNotBe(before.Avatar);
      (await ReadIdentityAsync(anonymous)).Avatar.ShouldBe(after.Avatar);
    }
    finally
    {
      UpdateSiteSettings.Response restored = await PutAssistantNameAsync(client, before.Name);
      restored.AssistantName.ShouldBe(before.Name);
    }
  }

  public static async Task Administrator_Put_Markup_Name_Should_400()
  {
    (PrincipalId principalId, string sessionCookie) = await RegisterPasskeyAndMintSessionAsync(Web);
    await GrantAdministratorAsync(principalId);
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    GetSiteSettings.Response current = await ReadSettingsAsync(client);

    string body = JsonSerializer.Serialize(
      new
      {
        entraSignInEnabled = current.EntraSignInEnabled,
        entraAllowBootstrap = current.EntraAllowBootstrap,
        passkeyPromptMode = current.PasskeyPromptMode.ToString(),
        assistantName = "<b>Zawadi</b>",
        entraTenantAcceptanceMode = current.EntraTenantAcceptanceMode.ToString(),
        entraAcceptedTenantIds = current.EntraAcceptedTenantIds,
        version = current.Version
      },
      ContractSerializationDefaults.Options);
    HttpResponseMessage put = await client.PutAsync(
      "api/settings",
      new StringContent(body, Encoding.UTF8, "application/json"));
    put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }

  private static async Task<GetAssistantIdentity.Response> ReadIdentityAsync(HttpClient client)
  {
    HttpResponseMessage response = await client.GetAsync("api/settings/assistant");
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    return JsonSerializer.Deserialize<GetAssistantIdentity.Response>(
        await response.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetAssistantIdentity deserialized to null.");
  }

  private static async Task<GetSiteSettings.Response> ReadSettingsAsync(HttpClient client)
  {
    HttpResponseMessage get = await client.GetAsync("api/settings");
    get.StatusCode.ShouldBe(HttpStatusCode.OK);
    return JsonSerializer.Deserialize<GetSiteSettings.Response>(
        await get.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetSiteSettings deserialized to null.");
  }

  private static async Task<UpdateSiteSettings.Response> PutAssistantNameAsync(HttpClient client, string assistantName)
  {
    GetSiteSettings.Response current = await ReadSettingsAsync(client);
    string body = JsonSerializer.Serialize(
      new
      {
        entraSignInEnabled = current.EntraSignInEnabled,
        entraAllowBootstrap = current.EntraAllowBootstrap,
        passkeyPromptMode = current.PasskeyPromptMode.ToString(),
        assistantName,
        entraTenantAcceptanceMode = current.EntraTenantAcceptanceMode.ToString(),
        entraAcceptedTenantIds = current.EntraAcceptedTenantIds,
        version = current.Version
      },
      ContractSerializationDefaults.Options);
    HttpResponseMessage put = await client.PutAsync(
      "api/settings",
      new StringContent(body, Encoding.UTF8, "application/json"));
    put.StatusCode.ShouldBe(HttpStatusCode.OK);
    return JsonSerializer.Deserialize<UpdateSiteSettings.Response>(
        await put.Content.ReadAsStringAsync(),
        ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("UpdateSiteSettings deserialized to null.");
  }

  private static async Task GrantAdministratorAsync(PrincipalId principalId)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);
  }
}
