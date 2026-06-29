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
