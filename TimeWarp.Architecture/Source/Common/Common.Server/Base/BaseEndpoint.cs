namespace TimeWarp.Architecture.Features;

[ApiController]
[Produces("application/json")]
public class BaseEndpoint<TRequest, TResponse> : ControllerBase
  where TRequest : IRequest<OneOf<TResponse, SharedProblemDetails>>
  where TResponse : BaseResponse
{
  private ISender? sender;

  protected ISender Sender => sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

  protected virtual async Task<IActionResult> Send(TRequest aRequest)
  {
    OneOf<TResponse, SharedProblemDetails> response = await Sender.Send(aRequest).ConfigureAwait(false);

    return response.Match<ActionResult>
    (
      Ok,
      problemDetails => StatusCode(problemDetails.Status ?? 400, problemDetails)
    );
  }
}
