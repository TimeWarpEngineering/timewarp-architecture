#region Purpose
// End-to-end tests for POST api/Roles: real host, real mediator pipeline, real backend validation.
#endregion

#region Design
// CreateRole carries [EndpointAuthorize(Policy=CanViewRolesPage)] (task 147-004) — Administrator,
// not any authenticated principal (task 110's identity-session-only posture is retired). An
// anonymous real-HTTP call 401s before FluentValidation ever runs; a Member-only passkey session
// 403s. ValidationError_Given_Empty_Name/UserId use ConfirmEndpointValidationError, which IS real
// HTTP (unlike Ok_With_RoleId_Given_Valid_Command's .Send(), which bypasses the ASP.NET Core auth
// pipeline entirely — see Roles_Authorization_Tests.cs's Design region for why .Send() could never
// have caught the pre-110 fail-open bug). Both validation tests mint a real passkey session via
// EnsureAuthenticatedAsync() and grant Administrator through IPrincipalRoleStore so the policy
// admits them into the FluentValidation path. Ceremony runs through WebTestServerApplication
// GetResponse (the SAME underlying HttpClient ConfirmEndpointValidationError uses), so the
// resulting Set-Cookie lands in that HttpClient's ambient cookie jar automatically — no manual
// cookie extraction/attachment needed here; this class never asserts on session state, only on
// validation behavior once authorized.
#endregion

namespace CreateRoleEndpoint_;

using System.Buffers.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

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

  private static Command CreateValidCommand() => new()
  {
    UserId = Guid.NewGuid(),
    Name = "Auditor",
    Description = "Read-only access to financial modules."
  };

  public static async Task Ok_With_RoleId_Given_Valid_Command()
  {

    Command command = CreateValidCommand();

    OneOf<Response, SharedProblemDetails> result = await Web.Send(command);

    result.Switch
    (
      response => response.RoleId.ShouldNotBe(Guid.Empty),
      problemDetails => problemDetails.ShouldBeNull("CreateRole returned SharedProblemDetails for a valid command.")
    );
  }

  public static async Task ValidationError_Given_Empty_Name()
  {

    Command command = CreateValidCommand();

    // Backend validation: FluentValidationBehavior runs the contract's Validator
    // (shared RoleDetailsValidator) server-side — the same rules the Blazor form enforced.
    await EnsureAuthenticatedAsync();
    command.Name = "";

    await Web.ConfirmEndpointValidationError<Response>(command, nameof(command.Name));
  }

  public static async Task ValidationError_Given_Empty_UserId()
  {

    Command command = CreateValidCommand();

    // AuthApiRequestValidator composes into the same server-side validation pass.
    await EnsureAuthenticatedAsync();
    command.UserId = Guid.Empty;

    await Web.ConfirmEndpointValidationError<Response>(command, nameof(command.UserId));
  }

  private static async Task EnsureAuthenticatedAsync()
  {
    var authenticator = new IntegrationSoftwareAuthenticator();

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

    OneOf<CompletePasskeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompletePasskeyRegistration.Response>(registerCommand, CancellationToken.None);
    result.IsT0.ShouldBeTrue("Passkey registration setup for an authenticated Roles test should succeed.");

    // Task 147-004: CanViewRolesPage requires Administrator. Fresh passkey principals are Member-only
    // (roles-authorization-tests proves Member → 403). Roles resolve at request time from the store
    // (not baked into the cookie), so grant after registration and the ambient-jar session is enough.
    PrincipalId principalId = result.AsT0.PrincipalId;
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, [RoleIds.Member, RoleIds.Administrator]);
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }

}
