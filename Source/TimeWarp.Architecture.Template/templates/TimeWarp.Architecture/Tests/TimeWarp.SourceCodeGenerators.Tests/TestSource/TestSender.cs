namespace TimeWarp.SourceCodeGenerators.Tests.TestSource;

using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TimeWarp.Fixie;

[NotTest]
public class TestSender : ISender
{
  public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
    throw new NotImplementedException();

  public Task<object> Send(object request, CancellationToken cancellationToken = default) =>
    throw new NotImplementedException();

  public IAsyncEnumerable<TResponse> CreateStream<TResponse>
  (
    IStreamRequest<TResponse> aStreamRequest,
    CancellationToken aCancellationToken = default
  ) => throw new NotImplementedException();

  public IAsyncEnumerable<object> CreateStream
  (
    object aRequest,
    CancellationToken aCancellationToken = default
  ) => throw new NotImplementedException();
}
