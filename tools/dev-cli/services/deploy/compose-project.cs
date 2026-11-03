#region Purpose
// A running Compose project as `compose ls --format json` reports it.
#endregion

#region Design
// Parsed by DeployCompose.ParseComposeProjects.
#endregion

namespace DevCli.Services;

/// <summary>A running Compose project as <c>compose ls --format json</c> reports it.</summary>
internal sealed record ComposeProject(string Name, string Status, IReadOnlyList<string> ConfigFiles);
