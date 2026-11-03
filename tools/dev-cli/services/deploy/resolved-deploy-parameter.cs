#region Purpose
// A deploy parameter value and where it came from.
#endregion

#region Design
// Source is the label the plan prints ("from the …"). Forwarded parameters are never secret.
#endregion

namespace DevCli.Services;

/// <summary>A deploy parameter value and where it came from (environment variable, AppHost user secret or appsettings file).</summary>
internal sealed record ResolvedDeployParameter(string Name, string Value, string Source);
