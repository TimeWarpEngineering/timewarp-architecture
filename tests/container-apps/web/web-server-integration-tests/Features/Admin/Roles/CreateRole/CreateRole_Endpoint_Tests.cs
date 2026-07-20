#region Purpose
// End-to-end tests for POST api/Roles: real host, real mediator pipeline, real backend validation.
#endregion

#region Design
// Task 110: CreateRole's generated endpoint now carries [EndpointAuthorize(Policy=
// "identity-session-authenticated")] — an anonymous real-HTTP call 401s before FluentValidation
// ever runs. ValidationError_Given_Empty_Name/UserId use ConfirmEndpointValidationError, which IS
// real HTTP (unlike Ok_With_RoleId_Given_Valid_Command's .Send(), which bypasses the ASP.NET Core
// auth pipeline entirely — see Roles_Authorization_Tests.cs's Design region for why .Send() could
// never have caught the pre-110 fail-open bug). Both validation tests now mint a real passkey
// session first via EnsureAuthenticatedAsync(): it runs the ceremony through
// WebTestServerApplication.GetResponse (the SAME underlying HttpClient ConfirmEndpointValidationError
// uses), so the resulting Set-Cookie lands in that HttpClient's own ambient cookie jar automatically
// — no manual cookie extraction/attachment needed here, unlike the isolated-client pattern used
// where cross-test cookie leakage would be a problem (this class's own tests never assert on
// session state, only on validation behavior once authenticated, so ambient-jar reliance is fine).
#endregion

namespace CreateRoleEndpoint_;

using System.Buffers.Text;
using System.Text.Json;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using static TimeWarp.Architecture.Features.Admin.Roles.CreateRole;

public class Returns_
{
  private readonly Command Command;
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_
  (
    WebTestServerApplication webTestServerApplication
  )
  {
    Command = new Command
    {
      UserId = Guid.NewGuid(),
      Name = "Auditor",
      Description = "Read-only access to financial modules."
    };
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_With_RoleId_Given_Valid_Command()
  {
    OneOf<Response, SharedProblemDetails> result = await WebTestServerApplication.Send(Command);

    result.Switch
    (
      response => response.RoleId.ShouldNotBe(Guid.Empty),
      problemDetails => problemDetails.ShouldBeNull("CreateRole returned SharedProblemDetails for a valid command.")
    );
  }

  public async Task ValidationError_Given_Empty_Name()
  {
    // Backend validation: FluentValidationBehavior runs the contract's Validator
    // (shared RoleDetailsValidator) server-side — the same rules the Blazor form enforced.
    await EnsureAuthenticatedAsync();
    Command.Name = "";

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>(Command, nameof(Command.Name));
  }

  public async Task ValidationError_Given_Empty_UserId()
  {
    // AuthApiRequestValidator composes into the same server-side validation pass.
    await EnsureAuthenticatedAsync();
    Command.UserId = Guid.Empty;

    await WebTestServerApplication.ConfirmEndpointValidationError<Response>(Command, nameof(Command.UserId));
  }

  private async Task EnsureAuthenticatedAsync()
  {
    var authenticator = new IntegrationSoftwareAuthenticator();

    OneOf<StartPasskeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartPasskeyRegistration.Response>(new StartPasskeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = ReadChallenge(start.AsT0.OptionsJson);
    string origin = WebTestServerApplication.HttpClient.BaseAddress!.GetLeftPart(UriPartial.Authority);

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
      await WebTestServerApplication.GetResponse<CompletePasskeyRegistration.Response>(registerCommand, CancellationToken.None);
    result.IsT0.ShouldBeTrue("Passkey registration setup for an authenticated Roles test should succeed.");
  }

  private static byte[] ReadChallenge(string optionsJson)
  {
    using JsonDocument document = JsonDocument.Parse(optionsJson);
    string challengeBase64Url = document.RootElement.GetProperty("challenge").GetString()!;
    return Base64Url.DecodeFromChars(challengeBase64Url);
  }
}
