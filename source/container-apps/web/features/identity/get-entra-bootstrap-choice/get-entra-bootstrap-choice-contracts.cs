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
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public bool Valid { get; }
    public string Destination { get; }

    public Response(bool valid, string destination)
    {
      Valid = valid;
      Destination = Guard.Against.Null(destination);
    }
  }
}
