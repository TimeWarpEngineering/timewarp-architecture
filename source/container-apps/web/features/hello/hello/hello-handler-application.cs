#region Purpose
// Server-side handler for the Hello sample query.
#endregion

#region Design
// Minimal reference implementation of the Object In, Object Out pipeline so template
// consumers can trace one feature end-to-end: contract -> handler -> generated endpoint.
// Has no failure path, so the SharedProblemDetails side of the OneOf is never produced;
// error handling belongs in real handlers, not this teaching sample.
#endregion

namespace TimeWarp.Architecture.Features.Hellos.Application;

using static TimeWarp.Architecture.Features.Hellos.Hello;

public sealed partial class Hello
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      var response = new Response()
      {
        Message = $"Hello, {query.Name}!"
      };

      return Task.FromResult((OneOf<Response, SharedProblemDetails>)response);
    }
  }
}
