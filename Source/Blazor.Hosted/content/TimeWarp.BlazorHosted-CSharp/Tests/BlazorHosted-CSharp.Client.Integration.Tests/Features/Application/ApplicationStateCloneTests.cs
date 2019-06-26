namespace BlazorHosted_CSharp.Client.Integration.Tests.Features.Application
{
  using AnyClone;
  using BlazorState;
  using Microsoft.Extensions.DependencyInjection;
  using Shouldly;
  using System;
  using BlazorHosted_CSharp.Client.Features.Application;
  using BlazorHosted_CSharp.Client.Integration.Tests.Infrastructure;

  internal class ApplicationStateCloneTests
  {
    public ApplicationStateCloneTests(TestFixture aTestFixture)
    {
      IServiceProvider serviceProvider = aTestFixture.ServiceProvider;
      IStore store = serviceProvider.GetService<IStore>();
      ApplicationState = store.GetState<ApplicationState>();
    }

    private ApplicationState ApplicationState { get; set; }

    public void ShouldClone()
    {
      //Arrange
      ApplicationState.Initialize(aName:"TestName", aLogo:"SomeUrl", aIsMenuExpanded:false);

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
