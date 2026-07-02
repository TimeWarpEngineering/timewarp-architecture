#region Purpose
// Injectable clock abstraction, plus a source of guaranteed-unique UTC timestamps.
#endregion

#region Design
// Behind IDateTimeService so tests can freeze or script time instead of racing the wall clock.
// NextUtcNow exists for consumers that use timestamps as ordering keys: DateTime.UtcNow can
// return the same tick twice under load, so it hands out strictly increasing ticks via
// Interlocked (lock-free), drifting at most a few ticks ahead of real time.
// Uniqueness is per-instance — register as a singleton or the guarantee evaporates.
#endregion

namespace TimeWarp.Foundation.Services;

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
