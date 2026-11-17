#region Purpose
// An AppHost appsettings file and its JSON text.
#endregion

#region Design
// The process half reads the file; AspireDeployParameters parses it, so parsing stays testable.
#endregion

namespace DevCli.Services;

/// <summary>An AppHost appsettings file (<see cref="FileName"/> beside the project) and its JSON text.</summary>
internal sealed record AppSettingsFile(string FileName, string Json);
