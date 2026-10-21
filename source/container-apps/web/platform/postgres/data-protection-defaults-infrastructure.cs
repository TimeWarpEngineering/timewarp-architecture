#region Purpose
// Stable application name for the ASP.NET Data Protection key ring.
#endregion

#region Design
// The literal is the template source name. dotnet new rewrites it to the generated app's name,
// so every replica of that app shares keys and two generated apps do not.
#endregion

namespace TimeWarp.Architecture.Persistence;

/// <summary>Discriminator shared by every replica of one app.</summary>
public static class DataProtectionDefaults
{
  /// <summary>Passed to <c>SetApplicationName</c>. The template source name.</summary>
  public const string ApplicationName = "TimeWarp.Architecture";
}
