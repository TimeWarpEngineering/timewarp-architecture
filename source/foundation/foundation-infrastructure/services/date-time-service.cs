namespace TimeWarp.Architecture.Services;

public class DateTimeService : IDateTimeService
{
  // A private field to store the last value used
  private long LastValueUsed = DateTime.UtcNow.Ticks;

  public DateTime UtcNow => DateTime.UtcNow;

  /// <summary>
  /// Get the next unique DateTime closest to now
  /// </summary>
  /// <remarks>
  /// This will move forward in time (barely) until if finds an unused tick
  /// </remarks>
  public DateTime NextUtcNow()
  {
    long result;
    long ticksNow = DateTime.UtcNow.Ticks;

    // Do this loop until result >= ticksNow
    do
    {
      result = Interlocked.Increment(ref LastValueUsed);

      if (result >= ticksNow)
        return new DateTime(ticks: result);

      ticksNow = LastValueUsed;
    } while (Interlocked.CompareExchange(ref LastValueUsed, ticksNow, result) != result);

    return new DateTime(ticks: result);
  }
}
