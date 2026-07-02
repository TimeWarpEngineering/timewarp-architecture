#region Purpose
// MVC-controller bridge from HTTP to the mediator pipeline for contract request/response pairs.
#endregion

#region Design
// Controllers are thin adapters: all behavior lives in mediator handlers, so an endpoint is just
// generics + Send. ISender is resolved from HttpContext.RequestServices rather than the
// constructor so derived (including source-generated) endpoints need no ctor plumbing.
// OneOf<TResponse, SharedProblemDetails> replaces exceptions for expected failures; the problem
// branch maps to its own Status code (default 400), keeping handler results transport-agnostic.
// FastEndpoints counterpart: base-fast-endpoint.cs — keep their semantics aligned.
#endregion

// TODO: Review this code.  Why not inject ISender?
namespace TimeWarp.Foundation.Features;

[ApiController]
[Produces("application/json")]
public class BaseEndpoint<TRequest, TResponse> : ControllerBase
  where TRequest : IRequest<OneOf<TResponse, SharedProblemDetails>>
  where TResponse : BaseResponse
{
  private ISender Sender => HttpContext?.RequestServices.GetRequiredService<ISender>()
    ?? throw new InvalidOperationException("ISender is not available.");

  protected virtual async Task<IActionResult> Send(TRequest request)
  {
    OneOf<TResponse, SharedProblemDetails> response = await Sender.Send(request).ConfigureAwait(false);

    return response.Match<ActionResult>
    (
      Ok,
      problemDetails => StatusCode(problemDetails.Status ?? 400, problemDetails)
    );
  }
}
