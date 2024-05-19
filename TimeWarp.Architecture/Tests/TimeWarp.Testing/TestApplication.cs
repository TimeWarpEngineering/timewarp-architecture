// namespace TimeWarp.Architecture.Testing;

// /// <summary>
// /// An abstract class that adds test functionality for sending Requests in a scope.
// /// </summary>
// /// <example><see cref="TestServerApplication"/></example>
// [NotTest]
// public abstract partial class TestApplication : ISender
// {
//   private readonly ISender ScopedSender;

//   public IServiceProvider ServiceProvider { get; }

//   public TestApplication(IServiceProvider aServiceProvider)
//   {
//     ServiceProvider = aServiceProvider;
//     ScopedSender = new ScopedSender(aServiceProvider);
//   }

//   #region ISender
//   public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
//     ScopedSender.Send(request, cancellationToken);

//   public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
//     where TRequest : IRequest => ScopedSender.Send(request, cancellationToken);

//   public Task<object> Send(object request, CancellationToken cancellationToken = default) => ScopedSender.Send(request, cancellationToken);

//   public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
//     ScopedSender.CreateStream(request, cancellationToken);

//   public IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default) =>
//     ScopedSender.CreateStream(request, cancellationToken);
//   #endregion
// }
