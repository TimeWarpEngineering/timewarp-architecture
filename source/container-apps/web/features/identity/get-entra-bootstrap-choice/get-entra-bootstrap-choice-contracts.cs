#region Purpose
// Public read of whether parked Microsoft 365 bootstrap claims are still valid for the choose page.
#endregion

#region Design
// Anonymous: the visitor is not signed in yet. Payload is Valid + Destination only — no tid/oid.
// Cookie is HttpOnly; this query only peeks the server-side park keyed by that cookie.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAllowAnonymous("Choose page needs to know whether parked Entra claims are still valid; the visitor has no session yet.")]
public static partial class GetEntraBootstrapChoice
{
  [ApiRoute("api/identity/entra/choice", HttpVerb.Get)]
  [Operation("entra-bootstrap.get-choice", Description = "Check whether the parked Microsoft 365 sign-in is still valid.", Group = "identity")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    /// <summary>True when the parked Microsoft 365 claims are still valid.</summary>
    public bool Valid { get; }
    /// <summary>The path the browser should navigate to next.</summary>
    public string Destination { get; }

    public Response(bool valid, string destination)
    {
      Valid = valid;
      Destination = Guard.Against.Null(destination);
    }
  }
}
