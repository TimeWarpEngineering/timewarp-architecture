#region Purpose
// The Compose project to exec into, or why there is none.
#endregion

#region Design
// Returned by DeployCompose.ChooseComposeProject; exactly one of the two is set.
#endregion

namespace DevCli.Services;

/// <summary>The Compose project to exec into, or why there is none.</summary>
internal sealed record ComposeProjectChoice(ComposeProject? Project, string? Refusal);
