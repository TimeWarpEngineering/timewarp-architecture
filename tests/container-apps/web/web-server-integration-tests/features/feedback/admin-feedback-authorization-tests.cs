#region Purpose
// End-to-end proof that the admin feedback endpoints require admin.feedback.read / .manage (task 314).
#endregion

#region Design
// Mirrors principals-authorization-tests: real HTTP, isolated clients, passkey cookie mint,
// roles via IPrincipalRoleStore. Anonymous is 401. A Member-only session is 403 on the admin
// list and on a status change. An Administrator lists, changes status, and the filer then sees
// the new status and the reply on their own item (the filer view never shows an internal
// note). Read-without-manage shrinks the Administrator bundle to admin.feedback.read: list 200,
// status change 403. The original bundle is restored in finally.
#endregion

namespace AdminFeedbackAuthorization_;

using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;
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

  public static async Task Unauthorized_Given_Anonymous_List()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await client.GetAsync("api/admin/feedback");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Forbidden_Given_Member_Only_Session()
  {
    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Member);
    using HttpClient client = Client(cookie);

    Guid itemId = await FileFeedbackAsync(client, "Member filing");

    HttpResponseMessage list = await client.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
    list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

    HttpResponseMessage change = await PutStatusAsync(client, itemId, FeedbackStatus.Planned);
    change.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Ok_Given_Administrator_And_Filer_Sees_The_Outcome()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    using HttpClient filer = Client(filerCookie);
    Guid itemId = await FileFeedbackAsync(filer, "Please triage me");

    (string adminCookie, PrincipalId adminId) = await MintIdentitySessionCookie();
    await SetRolesAsync(adminId, RoleIds.Member, RoleIds.Administrator);
    using HttpClient admin = Client(adminCookie);

    HttpResponseMessage list = await admin.GetAsync(new ListFeedback.Query().GetRouteWithQueryString());
    list.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await list.Content.ReadAsStringAsync()).ShouldContain(itemId.ToString());

    (await PutStatusAsync(admin, itemId, FeedbackStatus.Planned)).StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(admin, $"api/admin/feedback/{itemId}/replies", new { body = "On the roadmap." }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(admin, $"api/admin/feedback/{itemId}/notes", new { body = "Secret triage note" }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);

    HttpResponseMessage own = await filer.GetAsync($"api/Feedback/{itemId}");
    own.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await own.Content.ReadAsStringAsync();
    json.ShouldContain("\"status\":\"Planned\"");
    json.ShouldContain("On the roadmap.");
    json.ShouldNotContain("Secret triage note");

    (await PostAsync(filer, $"api/Feedback/{itemId}/replies", new { body = "Thank you!" }))
      .StatusCode.ShouldBe(HttpStatusCode.OK);
    (await PostAsync(filer, $"api/admin/feedback/{itemId}/replies", new { body = "Pretending to be admin" }))
      .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
  }

  public static async Task Forbidden_Manage_Given_Administrator_With_Feedback_Read_Only()
  {
    (string filerCookie, PrincipalId filerId) = await MintIdentitySessionCookie();
    await SetRolesAsync(filerId, RoleIds.Member);
    Guid itemId;
    using (HttpClient filer = Client(filerCookie))
    {
      itemId = await FileFeedbackAsync(filer, "Read-only admin");
    }

    (string cookie, PrincipalId principalId) = await MintIdentitySessionCookie();
    await SetRolesAsync(principalId, RoleIds.Member, RoleIds.Administrator);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRolePermissionStore permissionStore = scope.ServiceProvider.GetRequiredService<IRolePermissionStore>();
    IReadOnlyList<string> original = await permissionStore.GetPermissionIdsForRoleAsync(RoleIds.Administrator);
    try
    {
      await permissionStore.SetPermissionIdsForRoleAsync(
        RoleIds.Administrator,
        [PermissionIds.AdminAccess, PermissionIds.AdminFeedbackRead, PermissionIds.FeedbackFileSelf]);

      using HttpClient client = Client(cookie);
      (await client.GetAsync(new ListFeedback.Query().GetRouteWithQueryString())).StatusCode.ShouldBe(HttpStatusCode.OK);
      (await client.GetAsync($"api/admin/feedback/{itemId}")).StatusCode.ShouldBe(HttpStatusCode.OK);
      (await PutStatusAsync(client, itemId, FeedbackStatus.Declined)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      (await PostAsync(client, $"api/admin/feedback/{itemId}/notes", new { body = "nope" }))
        .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
    finally
    {
      await permissionStore.SetPermissionIdsForRoleAsync(RoleIds.Administrator, original);
    }
  }

  private static HttpClient Client(string cookie)
  {
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", cookie);
    return client;
  }

  private static async Task<Guid> FileFeedbackAsync(HttpClient client, string title)
  {
    HttpResponseMessage response = await PostAsync(
      client,
      "api/Feedback",
      new { kind = "BugReport", title, body = "Body for " + title, emailCopy = false, attachmentIds = Array.Empty<Guid>() });
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return document.RootElement.GetProperty("feedbackItemId").GetGuid();
  }

  private static Task<HttpResponseMessage> PutStatusAsync(HttpClient client, Guid itemId, FeedbackStatus status) =>
    client.PutAsync(
      $"api/admin/feedback/{itemId}/status",
      new StringContent(
        JsonSerializer.Serialize(new { status }, ContractSerializationDefaults.Options),
        Encoding.UTF8,
        "application/json"));

  private static Task<HttpResponseMessage> PostAsync(HttpClient client, string route, object body) =>
    client.PostAsync(
      route,
      new StringContent(JsonSerializer.Serialize(body, ContractSerializationDefaults.Options), Encoding.UTF8, "application/json"));

  private static async Task SetRolesAsync(PrincipalId principalId, params Guid[] roleIds)
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, roleIds);
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
