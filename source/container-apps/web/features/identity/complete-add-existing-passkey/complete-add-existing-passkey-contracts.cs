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
  [Operation("passkeys.complete-add-existing", Description = "Verify an existing passkey and merge its account into the caller's.", Group = "identity")]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    /// <summary>The credential id, base64url, as returned by the browser.</summary>
    public string CredentialId { get; set; } = null!;
    /// <summary>The base64url clientDataJSON from the browser's WebAuthn response.</summary>
    public string ClientDataJson { get; set; } = null!;
    /// <summary>The base64url authenticatorData from the browser's WebAuthn assertion.</summary>
    public string AuthenticatorData { get; set; } = null!;
    /// <summary>The base64url assertion signature from the browser.</summary>
    public string Signature { get; set; } = null!;
    /// <summary>The base64url user handle from the assertion, or null; not used for lookup.</summary>
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
    /// <summary>The number of credentials moved into the caller's principal.</summary>
    public int CredentialsMoved { get; }
    /// <summary>The id of the principal that was merged into the caller's.</summary>
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
