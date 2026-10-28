#region Purpose
// Which end of a PeriodStepper the page should move: previous or next.
#endregion

#region Design
// The stepper does not know years, days, or ranges. The page owns the label and
// decides what the next label is when ValueChanged reports one of these ends.
#endregion

namespace TimeWarp.Architecture.Components;

public enum PeriodStep
{
  Previous,
  Next,
}
