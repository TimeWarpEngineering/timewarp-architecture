#region Purpose
// Completes Microsoft 365 bootstrap by creating a new principal from parked claims.
#endregion

#region Design
// Anonymous: the visitor chose "Create a new account" and still has no identity-session.
// Body is empty; the park id is the HttpOnly cookie. Handler consumes the park (single-use).
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Completes parked Entra bootstrap create; the visitor has no session until this succeeds.")]
public static partial class CompleteEntraBootstrapCreate
{
  [ApiRoute("api/identity/entra/choice/create", HttpVerb.Post)]
  [Operation("entra-bootstrap.complete-create", Description = "Create a new account from the parked Microsoft 365 sign-in.", Group = "identity")]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  public sealed class Response
  {
    /// <summary>The id of the newly created principal.</summary>
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
