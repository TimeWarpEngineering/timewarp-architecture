#region Purpose
// Shared enum so form components can switch read-only, edit, and create behavior from one state value.
#endregion

namespace TimeWarp.Architecture.Components;

public enum EditMode { View, Edit, New };
