namespace TimeWarp.Architecture.Testing;

using MediatR;
using System;
using TimeWarp;

/// <summary>
/// An abstract class that adds test functionality for sending Requests in a scope.
/// </summary>
/// <example><see cref="TestServerApplication"/></example>
[NotTest]
public abstract partial class TestApplication
{
  [TimeWarp.Delegate]
  private readonly ISender ScopedSender;

  public IServiceProvider ServiceProvider { get; }

  public TestApplication(IServiceProvider aServiceProvider)
  {
    ServiceProvider = aServiceProvider;
    ScopedSender = new ScopedSender(aServiceProvider);
  }
}
