namespace TimeWarp.Architecture.Features.Counters.Spa;

internal partial class CounterState : State<CounterState>
{
  public override CounterState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    var counterState = new CounterState()
    {
      Count = Convert.ToInt32(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Count))].ToString(), CultureInfo.InvariantCulture),
      Guid = new System.Guid(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()),
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
