#region Purpose
// Placeholder Mediator pipeline behavior marking where cross-cutting concerns hook in.
#endregion

#region Design
// Intentionally does nothing beyond console writes: it exists so generated apps ship a wired,
// working example of pipeline registration (it runs before FluentValidationBehavior because
// behaviors execute in registration order). Replace with real concerns — logging, metrics,
// transactions — rather than layering additional demo behaviors.
#endregion

namespace TimeWarp.Architecture.Api.Server;
public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine("Handling request");
        TResponse response = await next();
        Console.WriteLine("-- Finished Request");

        return response;
    }
}
