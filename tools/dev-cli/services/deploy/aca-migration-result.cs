#region Purpose
// Outcome of the aca bundle run.
#endregion

#region Design
// Carries the exit code to report and the firewall-rule delete's own exit code.
#endregion

namespace DevCli.Services;

/// <summary>Outcome of the aca bundle run: the exit code to report, and the firewall-rule delete's own exit code.</summary>
internal sealed record AcaMigrationResult(int ExitCode, int CleanupExitCode, string? CleanupFailure);
