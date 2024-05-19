namespace TimeWarp.Architecture.Features.Counters;

internal partial class CounterState : State<CounterState>
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
  /// <param name="aCount"></param>
  public void Initialize(int aCount)
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Count = aCount;
  }
}
