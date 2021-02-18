namespace __FeatureName__State
{
  using AnyClone;
  using Shouldly;
  using HobbyAnime.Client.Integration.Tests.Infrastructure;
  using HobbyAnime.Features.__FeatureName__s;

  public class Clone_Should : BaseTest
  {
    private __FeatureName__State __FeatureName__State => Store.GetState<__FeatureName__State>();

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      //Arrange
      __FeatureName__State.Initialize(aPageIndex: 15);

      //Act
      var clone = __FeatureName__State.Clone() as __FeatureName__State;

      //Assert
      __FeatureName__State.ShouldNotBeSameAs(clone);
      __FeatureName__State.Count.ShouldBe(clone.PageIndex);
    }
  }
}
