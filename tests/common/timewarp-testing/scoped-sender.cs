#region Purpose
// ISender that wraps each Send in a fresh IServiceScope so tests isolate handler lifetimes.
#endregion

namespace TimeWarp.Architecture.Testing;

/// <summary>
/// This is an implementation of Mediator's ISender Interface
/// that wraps calls to Send in a <see cref="IServiceScope"/>.
/// </summary>
public class ScopedSender : ISender
{
  private readonly IServiceScopeFactory ServiceScopeFactory;

  public ScopedSender(IServiceProvider serviceProvider)
  {
    ServiceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
  }

  public ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
  {
    return ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request, cancellationToken);
      }
    );
  }

  public ValueTask<object?> Send(object request, CancellationToken cancellationToken = default)
  {
    return ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request, cancellationToken);
      }
    );
  }

  public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
  {
    return ExecuteInScope
    (
      serviceProvider =>
      {
        ISender sender = serviceProvider.GetRequiredService<ISender>();

        return sender.Send(request, cancellationToken);
      }
    );
  }

  internal async ValueTask<T> ExecuteInScope<T>(Func<IServiceProvider, ValueTask<T>> action)
  {
    using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
    return await action(serviceScope.ServiceProvider).ConfigureAwait(false);
  }

  internal async ValueTask ExecuteInScope(Func<IServiceProvider, ValueTask> action)
  {
    using IServiceScope serviceScope = ServiceScopeFactory.CreateScope();
    await action(serviceScope.ServiceProvider).ConfigureAwait(false);
  }
}
