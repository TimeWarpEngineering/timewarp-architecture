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
