#region Purpose
// Shared host factory, session / token minting and request helpers for the admin feedback authorization
// test classes (task 314).
#endregion

#region Design
// C-create: each test class owns its HostGraph (CreateHostAsync in SetupOnce, DisposeAsync in
// CleanUpOnce) and passes its WebTestServerApplication to the helpers that touch the host. The
// fixture keeps no host state, so no host is shared across classes.
#endregion

namespace AdminFeedbackAuthorization_;

using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles.Application;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

internal static class AdminFeedbackAuthorizationFixture
{
  /// <summary>
  /// Builds the host graph a test class owns for its lifetime (C-create): the class stores it in its
  /// SetupOnce and disposes it in its CleanUpOnce.
  /// </summary>
  public static Task<HostGraph> CreateHostAsync() =>
#if(api)
    HostGraphFactory.CreateWebWithApiAsync();
#else
    HostGraphFactory.CreateWebAsync();
#endif

  public static async Task<string> IssueTokenAsync(WebTestServerApplication web, params string[] scopes)
  {
    (PrincipalId _, string token) = await IssueTokenForPrincipalAsync(web, scopes);
    return token;
  }

  public static async Task<(PrincipalId PrincipalId, string AccessToken)> IssueTokenForPrincipalAsync(WebTestServerApplication web, params string[] scopes)
  {
    (PrincipalId principalId, string _, string accessToken) =
      await CredentialCeremonyHelpers.RegisterAgentKeyAndIssueTokenAsync(
        web,
        new IntegrationSoftwareAgentKey(),
        [.. scopes]);
    return (principalId, accessToken);
  }

  public static HttpClient Bearer(WebTestServerApplication web, string accessToken)
  {
    HttpClient client = new() { BaseAddress = web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    return client;
  }

  public static Task<HttpResponseMessage> PostNoteAsync(HttpClient client, Guid itemId, string body) =>
    PostAsync(client, $"api/admin/feedback/{itemId}/notes", new { body });

  public static Task<HttpResponseMessage> PostReplyAsync(HttpClient client, Guid itemId) =>
    PostAsync(client, $"api/admin/feedback/{itemId}/replies", new { body = "Hello filer" });

  public static async Task<string> EnsureProfileAsync(WebTestServerApplication web, PrincipalId principalId)
  {
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IProfileStore profiles = scope.ServiceProvider.GetRequiredService<IProfileStore>();
    ProfileId profileId = ProfileId.From(principalId.Value);
    if (await profiles.FindAsync(profileId) is { } existing)
    {
      return existing.DisplayName;
    }

    string displayName = $"Filer {principalId.Value:N}";
    await profiles.AddAsync(TimeWarp.Architecture.Features.Profiles.Domain.Profile.Create(
      profileId, displayName, "en-US", "US", "system"));
    return displayName;
  }

  public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
  {
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException($"{typeof(T).Name} deserialized to null.");
  }

  public static HttpClient Client(WebTestServerApplication web, string cookie)
  {
    HttpClient client = new() { BaseAddress = web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", cookie);
    return client;
  }

  public static async Task<Guid> FileFeedbackAsync(HttpClient client, string title, params Guid[] attachmentIds)
  {
    HttpResponseMessage response = await PostAsync(
      client,
      "api/Feedback",
      new { kind = "BugReport", title, body = "Body for " + title, emailCopy = false, attachmentIds });
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return document.RootElement.GetProperty("feedbackItemId").GetGuid();
  }

  public static async Task<Guid> UploadAttachmentAsync(HttpClient client, string fileName, byte[] body)
  {
    using HttpRequestMessage request = new(HttpMethod.Post, UploadFeedbackAttachment.Command.RouteTemplate);
    ByteArrayContent content = new(body);
    content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
    request.Content = content;
    request.Headers.TryAddWithoutValidation(FileUploadHeaders.FileName, Uri.EscapeDataString(fileName));
    using HttpResponseMessage response = await client.SendAsync(request);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return document.RootElement.GetProperty("attachmentId").GetGuid();
  }

  public static Task<HttpResponseMessage> PutStatusAsync(HttpClient client, Guid itemId, FeedbackStatus status) =>
    client.PutAsync(
      $"api/admin/feedback/{itemId}/status",
      new StringContent(
        JsonSerializer.Serialize(new { status }, ContractSerializationDefaults.Options),
        Encoding.UTF8,
        "application/json"));

  public static Task<HttpResponseMessage> PutSeverityAsync(HttpClient client, Guid itemId, FeedbackSeverity severity) =>
    client.PutAsync(
      $"api/admin/feedback/{itemId}/severity",
      new StringContent(
        JsonSerializer.Serialize(new { severity }, ContractSerializationDefaults.Options),
        Encoding.UTF8,
        "application/json"));

  public static Task<HttpResponseMessage> PostAsync(HttpClient client, string route, object body) =>
    client.PostAsync(
      route,
      new StringContent(JsonSerializer.Serialize(body, ContractSerializationDefaults.Options), Encoding.UTF8, "application/json"));

  public static async Task SetRolesAsync(WebTestServerApplication web, PrincipalId principalId, params Guid[] roleIds)
  {
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, roleIds);
  }

  public static async Task<(string Cookie, PrincipalId PrincipalId)> MintIdentitySessionCookie(WebTestServerApplication web)
  {
    var authenticator = new IntegrationSoftwareAuthenticator();
    var testApiService = new TestApiService(web.HttpClient, ContractSerializationDefaults.Options);

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

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

    using HttpClient sessionClient = new() { BaseAddress = web.HttpClient.BaseAddress };
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

  public static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
