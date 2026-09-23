#region Purpose
// Endpoint-centric contract for giving one of the CALLER's own credentials a user-chosen nickname —
// task 248-001's rename half of "make credentials distinguishable".
#endregion

#region Design
// POST + /rename on a route-identified resource, mirroring RevokeCredential exactly (same
// {CredentialId:guid} generated route member, same UserId client/mock-mode signal the server never
// trusts, same [EndpointAuthorize] policy + dual scheme). Nickname is the ONLY body field the server
// reads: 1–64 characters after trimming (Credential.MaxNicknameLength) — the validator rejects
// whitespace-only and oversize input before the handler runs, and the domain's Credential.Rename
// re-applies the same rule so a non-HTTP caller cannot bypass it.
// Load-bearing IDOR rule (same as RevokeCredential): the handler resolves the caller via
// ICurrentPrincipalAccessor and verifies credential.PrincipalId == caller BEFORE acting — a mismatch
// and an unknown CredentialId return the SAME 404 (never 403), so this endpoint is not an existence
// oracle for other principals' credentials.
// Renaming a REVOKED credential is allowed (the row stays visible under IncludeRevoked and a nickname
// is display-only) — no 409 branch, unlike revoke. Response is empty; the client re-fetches
// GetCredentials so the list stays the single source of truth (Settings' existing sequencing rule).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.CredentialManageSelf,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.AgentToken
)]
public static partial class RenameCredential
{
  [ApiRoute("api/identity/credentials/{CredentialId:guid}/rename", HttpVerb.Post)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Nickname { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.CredentialId).NotEmpty();
      RuleFor(x => x.Nickname)
        .NotEmpty()
        .Must(nickname => nickname is not null && nickname.Trim().Length is >= 1 and <= Credential.MaxNicknameLength)
        .WithMessage($"Nickname must be 1 to {Credential.MaxNicknameLength} characters after trimming.");
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
