namespace TimeWarp.Blazor.Integration.Tests.Features.Application
{
  using AnyClone;
  using Microsoft.AspNetCore.Blazor.Hosting;
  using Shouldly;
  using TimeWarp.Blazor.ApplicationFeature;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure;

  internal class ApplicationStateCloneTests : BaseTest
  {
    private ApplicationState ApplicationState { get; set; }

    public ApplicationStateCloneTests(WebAssemblyHost aWebAssemblyHost) : base(aWebAssemblyHost)
    {
      ApplicationState = Store.GetState<ApplicationState>();
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
