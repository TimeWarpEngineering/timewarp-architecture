#region Purpose
// Injectable clock abstraction so consumers can be tested with frozen or scripted time.
#endregion

#region Design
// NextUtcNow contracts to hand out strictly increasing timestamps for consumers that use
// time as an ordering key; see DateTimeService in foundation-infrastructure for the mechanics.
#endregion

namespace TimeWarp.Foundation.Abstractions;

/// <summary>
/// Injectable clock so handlers and tests can freeze or script UTC time.
/// </summary>
public interface IDateTimeService
{
  /// <summary>
  /// Current UTC wall-clock time.
  /// </summary>
  DateTime UtcNow { get; }
  /// <summary>
  /// Next strictly increasing UTC timestamp suitable as an ordering key.
  /// </summary>
  DateTime NextUtcNow();
}
