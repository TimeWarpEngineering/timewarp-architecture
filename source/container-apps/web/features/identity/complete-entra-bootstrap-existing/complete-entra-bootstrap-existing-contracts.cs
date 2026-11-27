#region Purpose
// Completes Microsoft 365 bootstrap by asserting an existing passkey and attaching parked Entra claims.
#endregion

#region Design
// Anonymous: this is the sign-in that also attaches Entra. UserHandle unused (same as
// CompletePasskeyAuthentication). Park id is the HttpOnly cookie, not the body.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Completes parked Entra bootstrap by proving an existing passkey; no session exists until this succeeds.")]
public static partial class CompleteEntraBootstrapExisting
{
  [ApiRoute("api/identity/entra/choice/existing", HttpVerb.Post)]
  [Operation("entra-bootstrap.complete-existing", Description = "Prove an existing passkey and link the parked Microsoft 365 sign-in to it.", Group = "identity")]
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
    /// <summary>The id of the principal the Microsoft 365 sign-in was linked to.</summary>
    public PrincipalId PrincipalId { get; }
    /// <summary>The path the browser should navigate to next.</summary>
    public string Destination { get; }

    public Response(PrincipalId principalId, string destination)
    {
      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      PrincipalId = principalId;
      Destination = Guard.Against.Null(destination);
    }
  }
}
