namespace EventStreamState_;

public class Clone_Should : BaseTest
{
  private EventStreamState EventStreamState => Store.GetState<EventStreamState>();

  public Clone_Should
  (
    ISpaTestApplication aSpaTestApplication
  ) : base(aSpaTestApplication) { }

  public void Clone()
  {
    //Arrange
    var events = new List<string> { "Event 1", "Event 2", "Event 3" };
    EventStreamState.Initialize(events);

    //Act
    EventStreamState clone = EventStreamState.Clone();

    //Assert
    EventStreamState.Events.Count.ShouldBe(clone.Events.Count);
    EventStreamState.Guid.ShouldNotBe(clone.Guid);
    EventStreamState.Events[0].ShouldBe(clone.Events[0]);
  }
}
