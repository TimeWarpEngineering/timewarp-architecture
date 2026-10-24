#region Purpose
// Passkey ceremonies return one 403 when Passkey is not listed, and an existing passkey still
// resumes after it is listed again.
#endregion

#region Design
// One host. Registration runs while the checked-in list includes Passkey, which proves the
// ceremony still succeeds with Passkey listed. The live SignInOptions list is then set to both
// methods (start still succeeds), then to Microsoft365 only. Each of the six passkey ceremony
// routes answers 403 with IdentityProblems.SignInMethodDisabledType. The credential row stays.
// Listing Passkey again makes start succeed. The gate reads IOptions, so this mutates the same
// instance the pipeline holds. Agent-key routes are not in the set.
#endregion

namespace PasskeySignInMethodGate_;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;

public class Returns_
{
  private const string Dummy = "YQ";

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

  public static async Task Refuse_The_Six_Passkey_Routes_When_Passkey_Is_Not_Listed()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    SetMethods(SignInMethod.Passkey, SignInMethod.Microsoft365);
    HttpResponseMessage bothListed = await PostAsync(StartPasskeyAuthentication.Command.RouteTemplate, new StartPasskeyAuthentication.Command());
    bothListed.StatusCode.ShouldBe(HttpStatusCode.OK);

    SetMethods(SignInMethod.Microsoft365);
    await ShouldRefuseAsync(StartPasskeyAuthentication.Command.RouteTemplate, new StartPasskeyAuthentication.Command());
    await ShouldRefuseAsync(CompletePasskeyAuthentication.Command.RouteTemplate, new CompletePasskeyAuthentication.Command
    {
      CredentialId = Dummy,
      ClientDataJson = Dummy,
      AuthenticatorData = Dummy,
      Signature = Dummy
    });
    await ShouldRefuseAsync(StartPasskeyRegistration.Command.RouteTemplate, new StartPasskeyRegistration.Command());
    await ShouldRefuseAsync(CompletePasskeyRegistration.Command.RouteTemplate, new CompletePasskeyRegistration.Command
    {
      CredentialId = Dummy,
      ClientDataJson = Dummy,
      AttestationObject = Dummy
    });
    await ShouldRefuseAsync(
      StartAddExistingPasskey.Command.RouteTemplate,
      new StartAddExistingPasskey.Command(),
      sessionCookie);
    await ShouldRefuseAsync(
      CompleteAddExistingPasskey.Command.RouteTemplate,
      new CompleteAddExistingPasskey.Command
      {
        CredentialId = Dummy,
        ClientDataJson = Dummy,
        AuthenticatorData = Dummy,
        Signature = Dummy
      },
      sessionCookie);

    using HttpClient listed = Client(sessionCookie);
    HttpResponseMessage credentials = await listed.GetAsync(new GetCredentials.Query { UserId = Guid.NewGuid() }.GetRouteWithQueryString());
    credentials.StatusCode.ShouldBe(HttpStatusCode.OK);
    string credentialsJson = await credentials.Content.ReadAsStringAsync();
    credentialsJson.ShouldContain("\"type\":\"Passkey\"");
    credentialsJson.ShouldContain("\"isActive\":true");

    SetMethods(SignInMethod.Passkey, SignInMethod.Microsoft365);
    HttpResponseMessage resumed = await PostAsync(StartPasskeyAuthentication.Command.RouteTemplate, new StartPasskeyAuthentication.Command());
    resumed.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  private static void SetMethods(params SignInMethod[] methods)
  {
    using IServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateScope();
    SignInOptions signIn = scope.ServiceProvider.GetRequiredService<IOptions<SignInOptions>>().Value;
    signIn.Methods = [.. methods];
  }

  private static async Task ShouldRefuseAsync<TCommand>(string route, TCommand command, string? sessionCookie = null)
  {
    HttpResponseMessage response = await PostAsync(route, command, sessionCookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    string body = await response.Content.ReadAsStringAsync();
    body.ShouldContain(IdentityProblems.SignInMethodDisabledType);
    body.ShouldNotContain("\"optionsJson\"");
  }

  private static async Task<HttpResponseMessage> PostAsync<TCommand>(string route, TCommand command, string? sessionCookie = null)
  {
    using HttpClient client = Client(sessionCookie);
    string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
    return await client.PostAsync(route, new StringContent(json, Encoding.UTF8, "application/json"));
  }

  private static HttpClient Client(string? sessionCookie)
  {
    HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    if (sessionCookie is not null)
    {
      client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    }

    return client;
  }
}
