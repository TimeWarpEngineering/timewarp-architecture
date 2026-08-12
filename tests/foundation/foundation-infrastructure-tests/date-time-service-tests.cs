namespace DateTimeService_;

public class NextUtcNow_Returns
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NextUtcNow_Returns>();

  public static Task No_Duplicates()
  {
    // Arrange
    var dateTimeService = new DateTimeService();

    // Act
    var trigger = new ManualResetEvent(false);
    Task<List<DateTime>>[] tasks =
      Enumerable.Range(1, 10)
      .Select(_ => Task.Run(() => GetDates(trigger)))      .ToArray();

    Thread.Sleep(1000);
    trigger.Set();

    Task.WaitAll(tasks);
    DateTime[] allDates = tasks.SelectMany(x => x.Result).ToArray();
    long ticksDifference = Math.Abs(allDates[^1].Ticks - DateTime.UtcNow.Ticks);

    IGrouping<DateTime, DateTime>[] counts =
      allDates.GroupBy(x => x)
      .Where(x => x.Count() > 1)
      .ToArray();

    // Assert
    counts.Length.ShouldBe(0);

    return Task.CompletedTask;

    // Local Functions
    List<DateTime> GetDates(ManualResetEvent trigger)
    {
      const int numberOfDates = 100_000;
      var result = new List<DateTime>(numberOfDates);
      trigger.WaitOne();
      for (int i = 0; i < numberOfDates; i++)
      {
        result.Add(dateTimeService.NextUtcNow());
      }

      return result;
    }
  }
}
