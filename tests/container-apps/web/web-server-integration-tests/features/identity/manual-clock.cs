#region Purpose
// TimeProvider the EntraTicketProcessor tests advance by hand to pin the last-used stamp.
#endregion

namespace EntraTicketProcessor_;

internal sealed class ManualClock : TimeProvider
{
  private DateTimeOffset Now;

  public ManualClock(DateTimeOffset now) => Now = now;

  public void Advance(TimeSpan by) => Now += by;

  public override DateTimeOffset GetUtcNow() => Now;
}
