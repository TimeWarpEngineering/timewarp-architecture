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
    public PrincipalId PrincipalId { get; }
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
