namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

using BlazorState;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

/// <summary>
/// Base Class for Client tests.
/// </summary>
/// <remarks>
/// Based on Jimmy's SliceFixture
/// https://github.com/jbogard/ContosoUniversityDotNetCore-Pages/blob/master/ContosoUniversity.IntegrationTests/SliceFixture.cs
/// </remarks>
public abstract class BaseTest
{
  private readonly IServiceScopeFactory ServiceScopeFactory;
  private readonly IServiceScope ServiceScope;
  private readonly ISender Sender;
  protected readonly IStore Store;

  /// <summary>
  /// Base Class for Spa tests.
  /// </summary>
  /// <param name="aSpaTestApplication"></param>
  /// <remarks>The response to Spa Actions is always 'Unit' because the handler updates the state.</remarks>
  public BaseTest(ISpaTestApplication aSpaTestApplication)
  {
    ServiceScopeFactory = aSpaTestApplication.ServiceProvider.GetService<IServiceScopeFactory>();
    ServiceScope = ServiceScopeFactory.CreateScope();
    Sender = ServiceScope.ServiceProvider.GetService<ISender>();
    Store = ServiceScope.ServiceProvider.GetService<IStore>();
  }

  protected Task<TResponse> Send<TResponse>(IRequest<TResponse> aRequest) => Send(aRequest);

  protected async Task Send(IRequest aRequest) => await Sender.Send(aRequest);

}


