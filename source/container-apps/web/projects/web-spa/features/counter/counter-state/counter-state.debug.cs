#region Purpose
// CounterState test-only initialization.
#endregion

#region Design
// Initialize(int) is guarded by State's ThrowIfNotTestAssembly so production code
// cannot bypass action-based mutation (open only after the test host calls
// StateTestOptions.Enable(), 12.0.0-beta.11).
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  public void Initialize(int count)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Count = count;
  }
}
