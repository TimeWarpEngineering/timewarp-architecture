#region Purpose
// Diameter of an initials Avatar: small, medium, or large.
#endregion

#region Design
// The name is InitialsAvatarSize so it does not clash with FluentUI's AvatarSize.
// Medium is the default and adds no modifier class. Small and Large add
// twe-avatar--small and twe-avatar--large. The stylesheet maps those to 24, 32, and 56.
#endregion

namespace TimeWarp.Architecture.Components;

public enum InitialsAvatarSize
{
  Small,
  Medium,
  Large,
}
