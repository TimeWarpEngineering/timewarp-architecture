#region Purpose
// Endpoint-centric contract for revoking one of the CALLER's own credentials — task 104-005's
// destructive half of the credential-management surface.
#endregion

#region Design
// POST + /revoke on a route-identified resource (matches DeleteRole's {RoleId:guid} precedent, and
// deliberately mirrors DeleteRole's "delete-shaped verb, non-delete-shaped path" choice) — this is a
// soft-revoke (Credential.Revoke sets RevokedAt, the row stays), not a hard delete, so POST-with-verb
// reads more honestly than DELETE would; CredentialId is not hand-declared: the {CredentialId:guid}
// segment in [ApiRoute] makes the source generator emit it on the partial Command (DeleteRole
// precedent). UserId is a client/mock-mode identity signal ONLY, exactly like GetCredentials — the
// server never trusts it. POST carries a body, so UserId travels as a normal body property (no
// query-string composition needed, unlike DeleteRole's DELETE-has-no-body case).
// Load-bearing IDOR rule (task 104-005): the handler resolves the caller via
// ICurrentPrincipalAccessor and verifies credential.PrincipalId == caller BEFORE acting — a mismatch
// and an unknown CredentialId return the SAME 404 (never 403), so this endpoint is not an existence
// oracle for other principals' credentials. Full revoke semantics (concurrency retry, last-credential
// guard, already-revoked handling) live on the handler's Design region, not here — the contract only
// states shape and auth posture.
// Response is empty (matches DeleteRole/UpdateRole) — success is the status code; no payload to
// return for a revoke.
// [EndpointAuthorize] (task 110/104-005): DeleteRole posture — see GetCredentials' Design region for
// the same policy/IAuthApiRequest rationale.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize(Policy = "credential-management")] // matches CredentialManagementDefaults.Policy
public static partial class RevokeCredential
{
  [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty();
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
