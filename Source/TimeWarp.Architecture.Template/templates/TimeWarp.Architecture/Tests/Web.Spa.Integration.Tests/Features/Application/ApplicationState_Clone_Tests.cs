namespace ApplicationState;

using AnyClone;
using FluentAssertions;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Testing;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;

public class Clone_Should : BaseTest
{
  private ApplicationState ApplicationState => Store.GetState<ApplicationState>();

  public Clone_Should
  (
    SpaTestApplication<YarpServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
  ) : base(aSpaTestApplication) { }

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
