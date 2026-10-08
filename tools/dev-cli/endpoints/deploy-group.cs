#region Purpose
// Nuru group base: `dev deploy *` subcommands (today `dev deploy migrate`) inherit this prefix.
#endregion

#region Design
// Same group pattern as publish-group.cs / db-group.cs. `dev deploy` itself stays the DeployCommand
// route; the group only carries the operator follow-up steps that act on a deployment (task 287).
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev deploy …</c> subcommands.</summary>
[NuruRouteGroup("deploy", Description = "Act on a deployment `dev deploy` made (operator-run, never CI)")]
public abstract class DeployGroup;
