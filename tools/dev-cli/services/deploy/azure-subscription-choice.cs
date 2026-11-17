#region Purpose
// The subscription an aca deploy targets, why, and whether the verb must pass it to Aspire.
#endregion

#region Design
// Returned by AspireDeployAzure.ChooseAzureSubscription; the Detail line is printed in the plan.
#endregion

namespace DevCli.Services;

/// <summary>The subscription an aca deploy targets, the line that says why, and whether the verb must pass it to Aspire.</summary>
internal sealed record AzureSubscriptionChoice(string SubscriptionId, string Detail, bool PassToAspire);
