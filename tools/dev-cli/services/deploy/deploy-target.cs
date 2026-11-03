#region Purpose
// One deploy target (the AppHost's Publish:Target value).
#endregion

#region Design
// A record, so targets compare by Name; AspireDeploy holds the four known instances.
#endregion

namespace DevCli.Services;

/// <summary>One deploy target: the AppHost's Publish:Target value.</summary>
internal sealed record DeployTarget(string Name);
