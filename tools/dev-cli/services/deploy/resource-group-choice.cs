#region Purpose
// The resource group an aca verb targets and where it came from.
#endregion

#region Design
// Returned by DeployAca.ChooseResourceGroup; a null Name means unset.
#endregion

namespace DevCli.Services;

/// <summary>The resource group an aca verb targets and the line that says where it came from; null Name when unset.</summary>
internal sealed record ResourceGroupChoice(string? Name, string Detail);
