#region Purpose
// The target's required parameters: those with a value, and the names nobody set.
#endregion

#region Design
// Returned by AspireDeployParameters.ResolveParameters.
#endregion

namespace DevCli.Services;

/// <summary>The target's required parameters: those with a value, and the names nobody set.</summary>
internal sealed record DeployParameterResolution(IReadOnlyList<ResolvedDeployParameter> Resolved, IReadOnlyList<string> Missing);
