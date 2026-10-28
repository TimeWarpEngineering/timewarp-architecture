#region Purpose
// Full identity banner or compact pull-status row for an IntegrationCard.
#endregion

#region Design
// One component, two variants. Full is the default and adds no modifier class.
// Compact adds twe-integration-card--compact. That row replaces a separate sync bar.
// IntegrationCardTone paints the compact row; it does not add a variant.
// Neither variant wires an event or owns a sync action. A page does not restyle the
// card with ::deep to get the compact form.
#endregion

namespace TimeWarp.Architecture.Components;

public enum IntegrationCardVariant
{
  Full,
  Compact,
}
