namespace ApplicationState
{
  using AnyClone;
  using FluentAssertions;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Features.Applications;

  public class Clone_Should : BaseTest
  {
    private ApplicationState ApplicationState => Store.GetState<ApplicationState>();

    public Clone_Should(TestClientApplication aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      //Arrange
      ApplicationState.Initialize(aName: "TestName", aLogo: "SomeUrl", aIsMenuExpanded: false);

      //Act
      ApplicationState clone = ApplicationState.Clone();

      //Assert
      ApplicationState.Should().NotBeSameAs(clone);
      ApplicationState.Name.Should().Be(clone.Name);
      ApplicationState.Logo.Should().Be(clone.Logo);
      ApplicationState.IsMenuExpanded.Should().Be(clone.IsMenuExpanded);
      ApplicationState.Guid.Should().NotBe(clone.Guid);
    }
  }
}
