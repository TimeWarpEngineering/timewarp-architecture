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

    if (oneOfResponse.IsT0)
    {
      await SendAsync(oneOfResponse.AsT0, 200, cancellationToken);
    }
    else
    {
      SharedProblemDetails problem = oneOfResponse.AsT1;

      // Set response content type with charset
      HttpContext.Response.ContentType = "application/problem+json; charset=utf-8";

      // Set status code before sending
      HttpContext.Response.StatusCode = problem.Status ?? 400;

      // Write the problem details directly
      await HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    }
  }
}
