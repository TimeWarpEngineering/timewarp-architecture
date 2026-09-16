#region Purpose
// Authenticated start of the add-existing-passkey merge ceremony: WebAuthn assertion options.
#endregion

#region Design
// Same empty-body options as StartPasskeyAuthentication (discoverable, empty allowCredentials).
// Challenge is minted as WebAuthnCeremonyType.Merge so a login challenge cannot complete merge.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class StartAddExistingPasskey
{
  [ApiRoute("api/identity/credentials/passkey/existing/options", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    public string OptionsJson { get; }

    public Response(string optionsJson)
    {
      OptionsJson = Guard.Against.NullOrEmpty(optionsJson);
    }
  }
}
