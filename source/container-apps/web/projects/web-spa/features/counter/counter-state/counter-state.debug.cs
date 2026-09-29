#region Purpose
// CounterState support for Redux DevTools rehydration and test-only initialization.
#endregion

#region Design
// Hydrate rebuilds state from the camelCased key/value bag Redux DevTools sends
// during time-travel debugging, so member names must round-trip through
// JsonNamingPolicy.CamelCase.ConvertName (TimeWarp.State 12.0.0-beta.5 dropped its
// hand-copied CamelCase helper).
// Initialize(int) is guarded by State's ThrowIfNotTestAssembly so production code
// cannot bypass action-based mutation (case-insensitive since 12.0.0-beta.5, so the
// kebab web-spa-integration-tests assembly passes — timewarp-state#607).
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public override CounterState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    var counterState = new CounterState()
    {
      Guid = new Guid(keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(Guid))].ToString() ?? throw new InvalidOperationException()),
      Count = Convert.ToInt32(keyValuePairs[JsonNamingPolicy.CamelCase.ConvertName(nameof(Count))].ToString(), CultureInfo.InvariantCulture),
    };

    return counterState;
  }

  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  public void Initialize(int count)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Count = count;
  }
}
