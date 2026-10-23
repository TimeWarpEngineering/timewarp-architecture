#region Purpose
// Which edge a Drawer sits on: the end edge (default) or the start edge.
#endregion

#region Design
// End is the default and adds twe-drawer--end. Start adds twe-drawer--start.
// The stylesheet pins the panel to that edge. A page does not pass a CSS side.
#endregion

namespace TimeWarp.Architecture.Components;

public enum DrawerSide
{
  End,
  Start,
}
