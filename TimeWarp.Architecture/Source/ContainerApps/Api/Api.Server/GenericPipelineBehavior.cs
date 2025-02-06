namespace TimeWarp.Architecture.Api.Server;

using MediatR;

public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine("Handling request");
        var response = await next();
        Console.WriteLine("-- Finished Request");

        return response;
    }
}
