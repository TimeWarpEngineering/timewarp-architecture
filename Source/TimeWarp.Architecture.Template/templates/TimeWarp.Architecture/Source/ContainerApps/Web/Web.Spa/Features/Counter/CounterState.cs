namespace TimeWarp.Architecture.Features.Counters;

using BlazorState;

[TwBaseSpa]
internal partial class CounterState : State<CounterState>
{
  public int Count { get; private set; }

  public CounterState() { }

  /// <summary>
  /// Set the Initial State
  /// </summary>
  public override void Initialize() => Count = 3;
}
