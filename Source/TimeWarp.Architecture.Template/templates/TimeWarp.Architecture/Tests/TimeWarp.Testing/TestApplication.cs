namespace TimeWarp.Architecture.Testing
{
  using MediatR;
  using System;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp;

  /// <summary>
  /// An abstract class that adds test functionality for sending Requests in a scope.
  /// </summary>
  /// <example><see cref="TestServerApplication"/></example>
  /// <example><see cref="TimeWarpBlazorClientApplication"/></example>
  [NotTest]
  public abstract partial class TestApplication
  {
    [Delegate]
    private readonly ISender ScopedSender;

    public IServiceProvider ServiceProvider { get; }

    public TestApplication(IServiceProvider aServiceProvider)
    {
      ServiceProvider = aServiceProvider;
      ScopedSender = new ScopedSender(aServiceProvider);
    }

    //public Task<TResponse> Send<TResponse>
    //(
    //  IRequest<TResponse> aRequest,
    //  CancellationToken aCancellationToken = default
    //) =>
    //  ScopedSender.Send(aRequest, aCancellationToken);

    //public Task<object> Send(object aRequest, CancellationToken aCancellationToken = default) =>
    //  ScopedSender.Send(aRequest, aCancellationToken);
  }
}
