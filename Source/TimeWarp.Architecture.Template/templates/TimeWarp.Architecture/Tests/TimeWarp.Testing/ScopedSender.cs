#nullable enable
namespace TimeWarp.Architecture.Testing;

/// <summary>
/// This is an implementation of MediatR's ISender Interface
/// that wraps calls to Send in a <see cref="IServiceScope"/>.
/// </summary>
public class ScopedSender : ISender
{
  private readonly IServiceScopeFactory ServiceScopeFactory;

  public ScopedSender(IServiceProvider serviceProvider)
  {
    ServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
  }

  // TODO: Implement this method when needed
  public IAsyncEnumerable<TResponse> CreateStream<TResponse>
  (
    IStreamRequest<TResponse> streamRequest,
    CancellationToken cancellationToken = default
  ) => throw new NotImplementedException();

  // TODO: Implement this method when needed
  public IAsyncEnumerable<object> CreateStream
  (
    object request,
    CancellationToken cancellationToken = default
  ) => throw new NotImplementedException();

  public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
  {
    return ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request);
      }
    );
  }

  public async Task<object?> Send(object request, CancellationToken aCancellationToken = default)
  {
    return await ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request);
      }
    );
  }

  public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
  {
    return await ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request);
      }
    );
  }

  internal async Task<T> ExecuteInScope<T>(Func<IServiceProvider, Task<T>> action)
  {
    using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
    return await action(serviceScope.ServiceProvider).ConfigureAwait(false);
  }

  internal async Task ExecuteInScope(Func<IServiceProvider, Task> action)
  {
    using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
    await action(serviceScope.ServiceProvider).ConfigureAwait(false);
  }
}
