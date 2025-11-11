namespace ApplicationState_;



public class Clone_Should : BaseTest
{
  private ApplicationState ApplicationState => Store.GetState<ApplicationState>();

  public Clone_Should
  (
    ISpaTestApplication spaTestApplication
  ) : base(spaTestApplication) { }

  public void Clone()
  {
    //Arrange
    ApplicationState.Initialize(name: "TestName", logo: "SomeUrl", isMenuExpanded: false);

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
