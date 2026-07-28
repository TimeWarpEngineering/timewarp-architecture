#region Purpose
// CounterState support for Redux DevTools rehydration and test-only initialization.
#endregion

#region Design
// Hydrate rebuilds state from the camelCased key/value bag Redux DevTools sends
// during time-travel debugging, so member names must round-trip through
// CamelCase.MemberNameToCamelCase.
// Initialize(int) is guarded by ThrowIfNotTestAssembly so production code cannot
// bypass action-based mutation.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public override CounterState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    var counterState = new CounterState()
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      Count = Convert.ToInt32(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Count))].ToString(), CultureInfo.InvariantCulture),
    };

    return counterState;
  }

  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  /// <param name="count"></param>
  public void Initialize(int count)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Count = count;
  }
}
