#region Purpose
// HTTP cookie PUT coverage for UpdateProfile: identity-session authorizes and persists;
// anonymous PUT stays 401. Isolated cookie jar — do not reuse the shared HttpClient jar.
#endregion

#region Design
// Task 205-003: GET GetProfile is AllowAnonymous (dual-mode demo) so handler-only and GET
// session tests never hit FastEndpoints auth on PUT. This suite sends PUT
// api/Users/Current/Profile on an isolated client. Register+authenticate+isolated-cookie
// matches get-profile-session-tests.cs. Persistence is proven by a follow-up GET with the
// same cookie (store-backed fields, not the anonymous mock). Fixture: C-create per class.
#endregion

namespace UpdateProfileSession_;

using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Text.Json;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles;
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

  private const string RpId = "localhost";
  private static TestApiService TestApiService => new(Web.HttpClient, ContractSerializationDefaults.Options);

  public static async Task Persists_Given_Authorized_Session()
  {
    IntegrationSoftwareAuthenticator authenticator = new();
    await RegisterPasskey(authenticator);

    CompletePasskeyAuthentication.Command authenticateCommand = await BuildValidAuthenticateCommand(authenticator);
    HttpResponseMessage authResponse = await TestApiService.GetHttpResponseMessage(authenticateCommand, CancellationToken.None);

    authResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    authResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();

    using HttpClient isolatedClient = CreateIsolatedClient(setCookieValues!);
    UpdateProfile.Command command = ValidCommand();
    HttpResponseMessage putResponse = await PutProfile(isolatedClient, command);

    putResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    string putJson = await putResponse.Content.ReadAsStringAsync();
    UpdateProfile.Response? putBody =
      JsonSerializer.Deserialize<UpdateProfile.Response>(putJson, ContractSerializationDefaults.Options);
    putBody.ShouldNotBeNull();
    putBody.Alias.ShouldBe(command.Alias);
    putBody.Email.ShouldBe(command.Email);
    putBody.Language.ShouldBe(command.Language);
    putBody.Region.ShouldBe(command.Region);
    putBody.Theme.ShouldBe(command.Theme);
    putBody.Notifications.ShouldBe(command.Notifications);

    HttpResponseMessage getResponse = await isolatedClient.GetAsync(GetProfile.Query.RouteTemplate);
    string getJson = await getResponse.Content.ReadAsStringAsync();
    GetProfile.Response profileResponse =
      JsonSerializer.Deserialize<GetProfile.Response>(getJson, ContractSerializationDefaults.Options)
      ?? throw new InvalidOperationException("GetProfile response deserialized to null.");
    profileResponse.Alias.ShouldBe(command.Alias);
    profileResponse.Email.ShouldBe(command.Email);
    profileResponse.Language.ShouldBe(command.Language);
    profileResponse.Region.ShouldBe(command.Region);
    profileResponse.Theme.ShouldBe(command.Theme);
    profileResponse.Notifications.ShouldBe(command.Notifications);
    profileResponse.Alias.ShouldNotBe("alias", "Authorized PUT must persist store-backed fields, not the anonymous GET mock.");
  }

  public static async Task Unauthorized_Given_No_Session()
  {
    using HttpClient anonymousClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    HttpResponseMessage response = await PutProfile(anonymousClient, ValidCommand());
    string body = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    body.ShouldBeEmpty();
  }

  private static UpdateProfile.Command ValidCommand() => new()
  {
    Alias = "Ada Lovelace",
    Email = "ada@example.com",
    Language = "en-US",
    Region = "US",
    Theme = "dark",
    Notifications = true
  };

  private static async Task<PrincipalId> RegisterPasskey(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);

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

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(registerCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Registration setup for a profile-session test should succeed.");
    return result.AsT0.PrincipalId;
  }

  private static async Task<CompletePasskeyAuthentication.Command> BuildValidAuthenticateCommand(IntegrationSoftwareAuthenticator authenticator)
  {
    OneOf<StartPasskeyAuthentication.Response, FileResponse, SharedProblemDetails> start =
      await Web.GetResponse<StartPasskeyAuthentication.Response>(new StartPasskeyAuthentication.Command(), CancellationToken.None);

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

  private static HttpClient CreateIsolatedClient(IEnumerable<string> setCookieValues)
  {
    string? sessionCookie = setCookieValues.FirstOrDefault(
      value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    sessionCookie.ShouldNotBeNull("Expected a Set-Cookie header carrying the identity-session cookie.");

    HttpClient isolatedClient = new() { BaseAddress = Web.HttpClient.BaseAddress };
    isolatedClient.DefaultRequestHeaders.Add("Cookie", sessionCookie.Split(';')[0]);
    return isolatedClient;
  }

  private static async Task<HttpResponseMessage> PutProfile(HttpClient client, UpdateProfile.Command command)
  {
    string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
    using StringContent content = new(json, Encoding.UTF8, "application/json");
    return await client.PutAsync(UpdateProfile.Command.RouteTemplate, content);
  }

}
