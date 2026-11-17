#region Purpose
// One process the aca migration runs.
#endregion

#region Design
// The three steps (create rule, bundle, delete rule) are handed to DeployAca.RunAcaBundleAsync.
#endregion

namespace DevCli.Services;

/// <summary>One process the aca migration runs: executable, arguments, and whether its output is captured for the summary.</summary>
internal sealed record ProcessStep(string Executable, IReadOnlyList<string> Arguments, bool Capture = false);
