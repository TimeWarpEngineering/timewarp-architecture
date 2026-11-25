#region Purpose
// The version of the running app, as the server reports it, for records that must say which build took them.
#endregion

#region Design
// Task 521: a feedback filing stores the version that took it, so a bug report triaged later can
// be checked against later releases. The value is the web-server assembly's informational version
// (version plus source revision when the SDK stamps it), the same string the boot log prints. It
// is read on the server and never taken from a client. Null when the assembly carries no
// informational version. A port so handler tests can pin a value.
#endregion

namespace TimeWarp.Architecture.Versioning;

public interface IAppVersionAccessor
{
  /// <summary>The running app's informational version, or null when the build carries none.</summary>
  string? GetVersion();
}
