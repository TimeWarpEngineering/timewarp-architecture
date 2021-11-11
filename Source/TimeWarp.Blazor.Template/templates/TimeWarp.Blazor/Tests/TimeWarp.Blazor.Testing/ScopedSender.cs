namespace TimeWarp.Blazor.Testing
{
  using MediatR;
  using Microsoft.Extensions.DependencyInjection;
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  /// <summary>
  /// This is an implementation of MediatR's ISender Interface
  /// that wraps calls to Send in a <see cref="IServiceScope"/>.
  /// </summary>
  [NotTest]
  public class ScopedSender: ISender
  {
    private readonly IServiceScopeFactory ServiceScopeFactory;

    public ScopedSender(IServiceProvider aServiceProvider)
    {
      ServiceScopeFactory = aServiceProvider.GetService<IServiceScopeFactory>();
    }

    public Task<object> Send(object aRequest, CancellationToken aCancellationToken = default)
    {
      return ExecuteInScope
      (
        aServiceProvider =>
        {
          IMediator mediator = aServiceProvider.GetService<IMediator>();

          return mediator.Send(aRequest);
        }
      );
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> aRequest, CancellationToken aCancellationToken = default)
    {
      return ExecuteInScope
      (
        aServiceProvider =>
        {
          IMediator mediator = aServiceProvider.GetService<IMediator>();

          return mediator.Send(aRequest);
        }
      );
    }

    internal async Task<T> ExecuteInScope<T>(Func<IServiceProvider, Task<T>> aAction)
    {
      using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
      return await aAction(serviceScope.ServiceProvider).ConfigureAwait(false);
    }
  }
}
