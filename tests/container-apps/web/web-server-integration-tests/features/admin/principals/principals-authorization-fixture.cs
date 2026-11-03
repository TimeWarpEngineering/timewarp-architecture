#region Purpose
// Shared host factory, session / token minting and role seeding helpers for the principals authorization
// test classes (task 182-002).
#endregion

#region Design
// C-create: each test class owns its HostGraph (CreateHostAsync in SetupOnce, DisposeAsync in
// CleanUpOnce) and passes its WebTestServerApplication to the helpers that touch the host. The
// fixture keeps no host state, so no host is shared across classes.
#endregion

namespace PrincipalsAuthorization_;

using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles.Application;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

internal static class PrincipalsAuthorizationFixture
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

  public static async Task<string> EnsureProfileAsync(AsyncServiceScope scope, PrincipalId principalId)
  {
    IProfileStore profiles = scope.ServiceProvider.GetRequiredService<IProfileStore>();
    string email = $"admin-{principalId.Value:N}@example.com";
    ProfileId profileId = ProfileId.From(principalId.Value);
    if (await profiles.FindAsync(profileId) is null)
    {
      TimeWarp.Architecture.Features.Profiles.Domain.Profile profile = TimeWarp.Architecture.Features.Profiles.Domain.Profile.Create(
        profileId, "Integration Admin", "en-US", "US", "system");
      profile.SetEmail(email);
      await profiles.AddAsync(profile);
    }

    return email;
  }

  public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
  {
    string json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(json, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException($"{typeof(T).Name} deserialized to null.");
  }

  public static async Task<string> RegisterAndIssueAgentBearerToken(WebTestServerApplication web)
  {
    var key = new IntegrationSoftwareAgentKey();

    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerStart =
      await web.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] registerChallenge = Base64Url.DecodeFromChars(registerStart.AsT0.Challenge);
    byte[] registerSignature = key.Sign(AgentKeyCeremonyType.Registration, registerChallenge);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(registerChallenge),
      Signature = Base64Url.EncodeToString(registerSignature)
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registerResult =
      await web.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);
    registerResult.IsT0.ShouldBeTrue("Registration setup should succeed.");

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenStart =
      await web.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] tokenChallenge = Base64Url.DecodeFromChars(tokenStart.AsT0.Challenge);
    byte[] tokenSignature = key.Sign(AgentKeyCeremonyType.TokenIssuance, tokenChallenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = registerResult.AsT0.KeyId,
      Challenge = Base64Url.EncodeToString(tokenChallenge),
      Signature = Base64Url.EncodeToString(tokenSignature),
      Scopes = [AgentScopes.IdentityRead]
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> tokenResult =
      await web.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    tokenResult.IsT0.ShouldBeTrue("Token issuance setup should succeed.");

    return tokenResult.AsT0.AccessToken;
  }

  public static async Task ForceMemberOnlyAsync(WebTestServerApplication web, PrincipalId principalId)
  {
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member]);
  }

  /// <summary>
  /// Host-graph isolation: demote every principal to Member except the listed admins.
  /// Needed because SetupOnce reuses one process and Mint leaves leftover Administrators.
  /// </summary>

  public static async Task EnsureOnlyAdminsAsync(
    IPrincipalRoleStore roleStore,
    IPrincipalStore principalStore,
    params PrincipalId[] adminPrincipalIds)
  {
    HashSet<PrincipalId> admins = [.. adminPrincipalIds];
    IReadOnlyList<Principal> principals = await principalStore.ListPrincipalsAsync();
    foreach (Principal principal in principals)
    {
      if (admins.Contains(principal.Id))
      {
        await roleStore.SetRoleIdsAsync(principal.Id, [RoleIds.Member, RoleIds.Administrator]);
      }
      else
      {
        await roleStore.SetRoleIdsAsync(principal.Id, [RoleIds.Member]);
      }
    }
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
