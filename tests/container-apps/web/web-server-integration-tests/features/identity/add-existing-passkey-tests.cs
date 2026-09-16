#region Purpose
// End-to-end tests for add-existing-passkey merge and post-merge sign-in.
#endregion

#region Design
// Two software authenticators, two principals. Signed in as target C, assert source S's
// passkey, merge. Then passkey login on the moved credential signs in as C; a stale
// session cookie for S is rejected by OnValidatePrincipal.
#endregion

namespace AddExistingPasskey_;

using System.Buffers.Text;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Merge_Given_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private const string RpId = "localhost";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Merge_Given_>();

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

  private static TestApiService TestApiService => new(Web.HttpClient, ContractSerializationDefaults.Options);

  public static async Task Complete_Merges_Source_Into_Caller_And_Moves_Credentials()
  {
    IntegrationSoftwareAuthenticator sourceAuthenticator = new();
    (PrincipalId sourceId, string? sourceCookie) = await RegisterPasskeyWithCookie(sourceAuthenticator);

    IntegrationSoftwareAuthenticator targetAuthenticator = new();
    PrincipalId targetId = await RegisterPasskey(targetAuthenticator);
    sourceId.ShouldNotBe(targetId);

    CompleteAddExistingPasskey.Command complete = await BuildMergeCommand(sourceAuthenticator);
    OneOf<CompleteAddExistingPasskey.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAddExistingPasskey.Response>(complete, CancellationToken.None);

    result.IsT0.ShouldBeTrue($"Expected merge success, got {Format(result)}");
    result.AsT0.CredentialsMoved.ShouldBeGreaterThan(0);
    result.AsT0.SourcePrincipalId.ShouldBe(sourceId);

    IPrincipalStore store = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Principal? retired = await store.GetPrincipalAsync(sourceId);
    retired.ShouldNotBeNull();
    retired.IsActive.ShouldBeFalse();
    retired.MergedIntoPrincipalId.ShouldBe(targetId);

    CompletePasskeyAuthentication.Command authenticate = await BuildAuthenticateCommand(sourceAuthenticator);
    OneOf<CompletePasskeyAuthentication.Response, FileResponse, SharedProblemDetails> login =
      await Web.GetResponse<CompletePasskeyAuthentication.Response>(authenticate, CancellationToken.None);
    login.IsT0.ShouldBeTrue();
    login.AsT0.PrincipalId.ShouldBe(targetId);

    if (sourceCookie is not null)
    {
      using HttpClient isolated = new() { BaseAddress = Web.HttpClient.BaseAddress };
      isolated.DefaultRequestHeaders.Add("Cookie", sourceCookie);
      HttpResponseMessage sessionResponse = await isolated.GetAsync(GetCurrentSession.Query.RouteTemplate);
      string json = await sessionResponse.Content.ReadAsStringAsync();
      GetCurrentSession.Response? session =
        JsonSerializer.Deserialize<GetCurrentSession.Response>(json, ContractSerializationDefaults.Options);
      session.ShouldNotBeNull();
      session.IsAuthenticated.ShouldBeFalse();
    }
  }

  public static async Task Complete_Already_On_This_Account_Should_409()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);
    CompleteAddExistingPasskey.Command complete = await BuildMergeCommand(authenticator);
    OneOf<CompleteAddExistingPasskey.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAddExistingPasskey.Response>(complete, CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Status.ShouldBe(409);
    result.AsT2.Title.ShouldBe("Already on this account");
  }

  public static async Task Complete_Quarantined_Source_Should_403()
  {
    IntegrationSoftwareAuthenticator sourceAuthenticator = new();
    PrincipalId sourceId = await RegisterPasskey(sourceAuthenticator);
    IntegrationSoftwareAuthenticator targetAuthenticator = new();
    await RegisterPasskey(targetAuthenticator);

    IPrincipalStore store = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Principal? source = await store.GetPrincipalAsync(sourceId);
    source.ShouldNotBeNull();
    source.Quarantine();
    await store.UpdatePrincipalAsync(source);

    CompleteAddExistingPasskey.Command complete = await BuildMergeCommand(sourceAuthenticator);
    OneOf<CompleteAddExistingPasskey.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAddExistingPasskey.Response>(complete, CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Status.ShouldBe(403);
    result.AsT2.Title.ShouldBe("Account cannot be merged");
  }

  public static async Task Complete_Merged_Source_Should_403()
  {
    IntegrationSoftwareAuthenticator sourceAuthenticator = new();
    PrincipalId sourceId = await RegisterPasskey(sourceAuthenticator);
    IntegrationSoftwareAuthenticator targetAuthenticator = new();
    PrincipalId targetId = await RegisterPasskey(targetAuthenticator);

    IPrincipalStore store = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Principal? source = await store.GetPrincipalAsync(sourceId);
    source.ShouldNotBeNull();
    source.MergeInto(targetId);
    await store.UpdatePrincipalAsync(source);

    CompleteAddExistingPasskey.Command complete = await BuildMergeCommand(sourceAuthenticator);
    OneOf<CompleteAddExistingPasskey.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAddExistingPasskey.Response>(complete, CancellationToken.None);

    result.IsT2.ShouldBeTrue();
    result.AsT2.Status.ShouldBe(403);
    result.AsT2.Title.ShouldBe("Account cannot be merged");
  }

  public static async Task Choose_Existing_Should_Attach_Entra_To_Passkey_Principal()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    PrincipalId principalId = await RegisterPasskey(authenticator);

    Guid tenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    Guid objectId = Guid.NewGuid();
    string issuer = System.Text.Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tenantId));
    TimeWarp.Architecture.Features.Identity.Application.EntraIdTokenClaims claims =
      new(tenantId, objectId, issuer, "Chooser");
    TimeWarp.Architecture.Features.Identity.Application.IParkedEntraClaimsStore parkedStore =
      Web.WebApplicationHost.ServiceProvider.GetRequiredService<TimeWarp.Architecture.Features.Identity.Application.IParkedEntraClaimsStore>();
    string parkId = parkedStore.Park(
      new TimeWarp.Architecture.Features.Identity.Application.ParkedEntraClaims(claims, "/Settings"));

    CompletePasskeyAuthentication.Command authenticate = await BuildAuthenticateCommand(authenticator);
    CompleteEntraBootstrapExisting.Command complete = new()
    {
      CredentialId = authenticate.CredentialId,
      ClientDataJson = authenticate.ClientDataJson,
      AuthenticatorData = authenticate.AuthenticatorData,
      Signature = authenticate.Signature
    };

    using HttpClient isolated = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolated.DefaultRequestHeaders.Add(
      "Cookie",
      $"{TimeWarp.Architecture.Features.Identity.Application.EntraChoiceCookie.CookieName}={parkId}");
    string body = JsonSerializer.Serialize(complete, ContractSerializationDefaults.Options);
    HttpResponseMessage httpResponse = await isolated.PostAsync(
      "/api/identity/entra/choice/existing",
      new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
    httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    IPrincipalStore store = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Credential? entra = await store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(tenantId, objectId));
    entra.ShouldNotBeNull();
    entra!.PrincipalId.ShouldBe(principalId);
  }

  public static async Task Start_Anonymous_Should_401()
  {
    using HttpClient anonymous = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await anonymous.PostAsync(
      StartAddExistingPasskey.Command.RouteTemplate,
      new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  private static async Task<PrincipalId> RegisterPasskey(IntegrationSoftwareAuthenticator authenticator)
  {
    (PrincipalId principalId, string? _) = await RegisterPasskeyWithCookie(authenticator);
    return principalId;
  }

  private static async Task<(PrincipalId PrincipalId, string? Cookie)> RegisterPasskeyWithCookie(
    IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    start.IsT0.ShouldBeTrue();

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: true);
    byte[] attestationObject = IntegrationSoftwareAuthenticator.BuildAttestationObject(authenticatorData);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.create", challenge, origin);

    var registerCommand = new CompletePasskeyRegistration.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AttestationObject = Base64Url.EncodeToString(attestationObject)
    };

    HttpResponseMessage httpResponse = await TestApiService.GetHttpResponseMessage(registerCommand, CancellationToken.None);
    httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string json = await httpResponse.Content.ReadAsStringAsync();
    CompletePasskeyRegistration.Response? parsed =
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerializationDefaults.Options);
    parsed.ShouldNotBeNull();
    httpResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues);
    return (parsed.PrincipalId, CookieFrom(setCookieValues));
  }

  private static async Task<CompleteAddExistingPasskey.Command> BuildMergeCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartAddExistingPasskey.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartAddExistingPasskey.Response>(new StartAddExistingPasskey.Command(), CancellationToken.None);
    start.IsT0.ShouldBeTrue($"Start merge should succeed, got {Format(start)}");

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: false);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, origin);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    return new CompleteAddExistingPasskey.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AuthenticatorData = Base64Url.EncodeToString(authenticatorData),
      Signature = Base64Url.EncodeToString(signature)
    };
  }

  private static async Task<CompletePasskeyAuthentication.Command> BuildAuthenticateCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyAuthentication.Response>(new StartPasskeyAuthentication.Command(), CancellationToken.None);
    start.IsT0.ShouldBeTrue();

    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = Web.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);
    byte[] authenticatorData = authenticator.BuildAuthenticatorData(RpId, includeAttestedCredentialData: false);
    byte[] clientDataJson = IntegrationSoftwareAuthenticator.BuildClientDataJson("webauthn.get", challenge, origin);
    byte[] signature = authenticator.Sign(authenticatorData, clientDataJson);

    return new CompletePasskeyAuthentication.Command
    {
      CredentialId = Base64Url.EncodeToString(authenticator.CredentialId),
      ClientDataJson = Base64Url.EncodeToString(clientDataJson),
      AuthenticatorData = Base64Url.EncodeToString(authenticatorData),
      Signature = Base64Url.EncodeToString(signature)
    };
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }

  private static string? CookieFrom(IEnumerable<string>? setCookieValues)
  {
    if (setCookieValues is null)
    {
      return null;
    }

    string? sessionCookie = setCookieValues.FirstOrDefault(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    return sessionCookie?.Split(';')[0];
  }

  private static string Format<T>(OneOf<T, FileResponse, SharedProblemDetails> result)
    where T : class =>
    result.IsT2 ? $"{result.AsT2.Title}: {result.AsT2.Detail}" : result.ToString();
}
