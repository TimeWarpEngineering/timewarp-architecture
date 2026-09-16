#region Purpose
// Completes add-existing-passkey: verify assertion, merge the other principal into the caller.
#endregion

#region Design
// Authenticated identity-session. UserHandle unused (credential-handle lookup). Response includes
// CredentialsMoved so Settings can show "Merged account: N credential(s) moved".
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class CompleteAddExistingPasskey
{
  [ApiRoute("api/identity/credentials/passkey/existing", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string CredentialId { get; set; } = null!;
    public string ClientDataJson { get; set; } = null!;
    public string AuthenticatorData { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public string? UserHandle { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty().MaximumLength(2 * 1024);
      RuleFor(x => x.ClientDataJson).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.AuthenticatorData).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.Signature).NotEmpty().MaximumLength(64 * 1024);
      RuleFor(x => x.UserHandle).MaximumLength(2 * 1024);
    }
  }

  public sealed class Response
  {
    public int CredentialsMoved { get; }
    public PrincipalId SourcePrincipalId { get; }

    public Response(int credentialsMoved, PrincipalId sourcePrincipalId)
    {
      ArgumentOutOfRangeException.ThrowIfNegative(credentialsMoved);

      if (sourcePrincipalId.IsEmpty)
      {
        throw new ArgumentException("SourcePrincipalId cannot be empty.", nameof(sourcePrincipalId));
      }

      CredentialsMoved = credentialsMoved;
      SourcePrincipalId = sourcePrincipalId;
    }
  }
}
