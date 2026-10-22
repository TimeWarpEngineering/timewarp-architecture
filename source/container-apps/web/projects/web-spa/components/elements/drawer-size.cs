#region Purpose
// Width of a Drawer: medium (default) or wide.
#endregion

#region Design
// Medium is the default and adds no size class. Wide adds twe-drawer--wide.
// Widths are the --twe-drawer-width and --twe-drawer-width-wide tokens.
// A page does not pass a pixel width.
#endregion

namespace TimeWarp.Architecture.Components;

public enum DrawerSize
{
  Medium,
  Wide,
}
