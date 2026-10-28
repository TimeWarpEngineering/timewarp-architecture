#region Purpose
// Design-system state. The confirm sample is the only action.
#endregion

#region Design
// No stored fields. The sample action asks and, on yes, publishes a shell note. The page
// does not keep a local flag for the dialog.
#endregion

namespace TimeWarp.Architecture.Features.DesignSystem;

/// <summary>Design-system demos that go through an action.</summary>
[StateAccess]
public sealed partial class DesignSystemState : State<DesignSystemState>
{
  /// <inheritdoc />
  public override void Initialize()
  {
  }
}
