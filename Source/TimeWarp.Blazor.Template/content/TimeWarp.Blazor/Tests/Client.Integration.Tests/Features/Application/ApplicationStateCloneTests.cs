namespace TimeWarp.Blazor.Client.Integration.Tests.Features.Application
{
  using AnyClone;
  using TimeWarp.Blazor.Client.ApplicationFeature;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;

  internal class ApplicationStateCloneTests
  {
    private ApplicationState ApplicationState { get; set; }

    public ApplicationStateCloneTests(TestFixture aTestFixture)
    {
      IServiceProvider serviceProvider = aTestFixture.ServiceProvider;
      IStore store = serviceProvider.GetService<IStore>();
      ApplicationState = store.GetState<ApplicationState>();
    }

    public void ShouldClone()
    {
      //Arrange
      ApplicationState.Initialize(aName: "TestName", aLogo: "SomeUrl", aIsMenuExpanded: false);

      //Act
      ApplicationState clone = ApplicationState.Clone();

      //Assert
      ApplicationState.ShouldNotBeSameAs(clone);
      ApplicationState.Name.ShouldBe(clone.Name);
      ApplicationState.Logo.ShouldBe(clone.Logo);
      ApplicationState.IsMenuExpanded.ShouldBe(clone.IsMenuExpanded);
      ApplicationState.Guid.ShouldNotBe(clone.Guid);
    }
  }
}