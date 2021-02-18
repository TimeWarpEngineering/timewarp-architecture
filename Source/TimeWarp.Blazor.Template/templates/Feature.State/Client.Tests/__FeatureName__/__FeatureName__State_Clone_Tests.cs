namespace __FeatureName__State
{
  using AnyClone;
  using Shouldly;
  using TimeWarp.Blazor.Client.Integration.Tests.Infrastructure;
  using TimeWarp.Blazor.Features.__FeatureName__s;

  public class Clone_Should : BaseTest
  {
    private __FeatureName__State __FeatureName__State => Store.GetState<__FeatureName__State>();

    public Clone_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public void Clone()
    {
      var objectByDictionary = new Dictionary<string, object>();
      objectByDictionary.Add("Item1", new object { aName = "Mike" });
      //Arrange
      __FeatureName__State.Initialize(aPageIndex: 1, aPageSize: 21, objectByDictionary);
      //Act
      var clone = __FeatureName__State.Clone() as __FeatureName__State;

      //Assert
      __FeatureName__State.ShouldNotBeSameAs(clone);
      __FeatureName__State.PageSize.ShouldBe(clone.PageSize);
      __FeatureName__State.PageIndex.ShouldBe(clone.PageIndex);
      __FeatureName__State.Current__FeatureName__[0].ShouldNotBe(clone.objectByDictionary[0])
      __FeatureName__State.Guid.ShouldNotBe(clone.Guid);
    }
  }
}
