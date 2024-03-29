namespace ApplicationState_;



public class Clone_Should : BaseTest
{
  private ApplicationState ApplicationState => Store.GetState<ApplicationState>();

  public Clone_Should
  (
    SpaTestApplication<YarpTestServerApplication, TimeWarp.Architecture.Yarp.Server.Program> aSpaTestApplication
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
