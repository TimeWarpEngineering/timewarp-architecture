namespace TimeWarp.Architecture.Components;

#region Purpose
// Status values for the StatusBadge component, mapped to .twe-badge--* modifiers.
#endregion

#region Design
// Error is the red lost/error status. One value covers both words. The badge label
// carries the word the page wants ("Error", "Lost", "Churned"). The other four
// values stay the record statuses they already were.
#endregion

public enum BadgeStatus
{
  Active,
  Inactive,
  Warning,
  Info,
  Error,
}
