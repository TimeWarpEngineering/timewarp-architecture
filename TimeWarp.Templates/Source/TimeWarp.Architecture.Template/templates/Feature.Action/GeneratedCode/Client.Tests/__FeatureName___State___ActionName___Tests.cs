namespace __FeatureName__State
{
  using Shouldly;
  using System.Threading.Tasks;
  using HobbyAnime.Client.Integration.Tests.Infrastructure;
  using HobbyAnime.Features.__FeatureName__s;
  using static HobbyAnime.Features.__FeatureName__s.__FeatureName__State;

  public class __ActionName__Action_Should : BaseTest
  {
    private __FeatureName__State __FeatureName__State => Store.GetState<__FeatureName__State>();

    public __ActionName__Action_Should(ClientHost aWebAssemblyHost) : base(aWebAssemblyHost) { }

    public async Task Decrement_PageIndex_Given_NegativeAmount()
    {
      //Arrange 
      CounterState.Initialize(aPageIndex: 15);

      var incrementPageIndexRequest = new __ActionName__Action
      {
        PageIndex = -2
      };

      //Act
      await Send(incrementPageIndexRequest);

      //Assert
      __FeatureName__State.PageIndex.ShouldBe(13);
    }

    public async Task Increment_PageIndex()
    {
      //Arrange
      __FeatureName__State.Initialize(aPageIndex: 22);

      var incrementPageIndexRequest = new __ActionName__Action
      {
        PageIndex = 5
      };

      //Act
      await Send(incrementPageIndexRequest);

      //Assert
      CounterState.PageIndex.ShouldBe(27);
    }
  }
}
