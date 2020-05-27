// #test_ApplicationState
namespace ApplicationState
{
  using AnyClone;
  using Shouldly;
  using TimeWarp.Blazor.Features.Applications.Client;
  using TimeWarp.Blazor.Integration.Tests.Infrastructure.Client;

  public class Clone_Should : BaseTest
  {
    private ApplicationState ApplicationState { get; set; }

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost)
    {
      ApplicationState = Store.GetState<ApplicationState>();
    }

    public void Clone()
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
