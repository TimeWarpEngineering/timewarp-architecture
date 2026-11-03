#region Purpose
// The subscription the Azure CLI is logged in to.
#endregion

#region Design
// Parsed from `az account show` by AspireDeployAzure.ParseAzureAccount.
#endregion

namespace DevCli.Services;

/// <summary>The subscription the Azure CLI is logged in to (<c>az account show</c>).</summary>
internal sealed record AzureAccount(string Id, string Name);
