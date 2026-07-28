#region Purpose
// Minimal demo state slice — the template's reference example of a TimeWarp.State store.
#endregion

#region Design
// Count has a private setter: mutation happens only in the action handlers in the
// sibling partial files, demonstrating one-way data flow.
// Initialize seeds a nonzero value so an initialized state is observably different
// from default(int) in tests and demos.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

[StateAccess]
public sealed partial class CounterState : State<CounterState>
{
  public int Count { get; private set; }

  public CounterState() { }

  /// <summary>
  /// Set the Initial State
  /// </summary>
  public override void Initialize() => Count = 3;
}
