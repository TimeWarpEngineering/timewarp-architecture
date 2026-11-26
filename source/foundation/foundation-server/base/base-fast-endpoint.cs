#region Purpose
// FastEndpoints bridge from HTTP to the mediator pipeline; base for source-generated endpoints.
#endregion

#region Design
// The FastEndpoint generator emits subclasses of this, so it must stay ctor-free:
// ISender comes from HttpContext.RequestServices instead of constructor injection.
// Writes the response manually (not SendAsync) because the endpoint's declared response type is
// the OneOf union — success serializes as bare TResponse, failure as application/problem+json
// with the problem's own Status (default 400), so clients never see the union wrapper.
// SharedProblemDetailsHttp writes the problem; it passes the media type to WriteAsJsonAsync,
// which otherwise replaces the Content-Type with application/json.
// MVC BaseEndpoint was removed (task 131 F-002): the template's only HTTP ingress for contracts
// is generated FastEndpoints; reintroduce MVC only via a deliberate future decision.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// FastEndpoints bridge that dispatches <typeparamref name="TRequest"/> through the mediator OneOf pipeline.
/// </summary>
public abstract class BaseFastEndpoint<TRequest, TResponse> : Endpoint<TRequest, OneOf<TResponse, SharedProblemDetails>>
  where TRequest : IRequest<OneOf<TResponse, SharedProblemDetails>>
  where TResponse : class
{
  private ISender Sender => HttpContext?.RequestServices.GetRequiredService<ISender>()
    ?? throw new InvalidOperationException("ISender is not available.");

  /// <summary>
  /// Sends the request via mediator and writes JSON success or problem+json failure.
  /// </summary>
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
      problem => SharedProblemDetailsHttp.WriteAsync(HttpContext.Response, problem, cancellationToken)
    );
  }
}
