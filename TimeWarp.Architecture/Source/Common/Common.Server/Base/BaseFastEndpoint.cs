// TODO: Review this code.  Why not inject ISender?
namespace TimeWarp.Architecture.Features;

public abstract class BaseFastEndpoint<TRequest, TResponse> : Endpoint<TRequest, OneOf<TResponse, SharedProblemDetails>>
  where TRequest : IRequest<OneOf<TResponse, SharedProblemDetails>>
  where TResponse : BaseResponse
{
  private ISender Sender => HttpContext?.RequestServices.GetRequiredService<ISender>()
    ?? throw new InvalidOperationException("ISender is not available.");

  public override async Task HandleAsync(TRequest request, CancellationToken cancellationToken)
  {
    OneOf<TResponse, SharedProblemDetails> oneOfResponse = await Sender.Send(request, cancellationToken).ConfigureAwait(false);

    await oneOfResponse.Match<Task>
    (
      async success =>
      {
        HttpContext.Response.StatusCode = 200;
        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.WriteAsJsonAsync(success, cancellationToken);
      },
      async problem =>
      {
        HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";
        HttpContext.Response.StatusCode = problem.Status ?? 400;
        await HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
      }
    );
  }
}
