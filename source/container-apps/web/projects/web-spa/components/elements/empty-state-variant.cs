#region Purpose
// Full paper card or compact inline form for an EmptyState.
#endregion

#region Design
// One component, two variants. Full is the default and adds no modifier class.
// Inline adds twe-empty-state--inline, which drops the paper, shadow, and padding.
// A page does not restyle the card with ::deep to get the compact form.
#endregion

namespace TimeWarp.Architecture.Components;

public enum EmptyStateVariant
{
  Full,
  Inline,
}
