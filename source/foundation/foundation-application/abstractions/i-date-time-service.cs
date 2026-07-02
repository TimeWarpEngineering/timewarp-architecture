#region Purpose
// Injectable clock abstraction so consumers can be tested with frozen or scripted time.
#endregion

#region Design
// NextUtcNow contracts to hand out strictly increasing timestamps for consumers that use
// time as an ordering key; see DateTimeService in foundation-infrastructure for the mechanics.
#endregion

namespace TimeWarp.Foundation.Abstractions;

public interface IDateTimeService
{
  DateTime UtcNow { get; }
  DateTime NextUtcNow();
}
