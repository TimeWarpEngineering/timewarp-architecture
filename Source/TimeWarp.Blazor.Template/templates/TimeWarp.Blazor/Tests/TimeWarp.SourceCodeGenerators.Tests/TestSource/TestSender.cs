namespace TimeWarp.SourceCodeGenerators.Tests.TestSource
{
  using MediatR;
  using System.Threading;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public class TestSender : ISender
  {
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
      throw new System.NotImplementedException();

    public Task<object> Send(object request, CancellationToken cancellationToken = default) =>
      throw new System.NotImplementedException();
  }
}
