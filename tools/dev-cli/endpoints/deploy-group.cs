#region Purpose
// Nuru group base: `dev deploy` itself (DeployCommand, the empty route) and `dev deploy migrate` inherit this prefix.
#endregion

#region Design
// Same group pattern as publish-group.cs / db-group.cs (task 287). DeployCommand is the group's empty
// route `[NuruRoute("")]` rather than a separate top-level `deploy` route: Nuru answers
// `dev deploy --help` with the group's table, which would then hide the deploy verb's own options.
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev deploy</c> and its subcommands.</summary>
[NuruRouteGroup("deploy", Description = "Deploy with `aspire deploy`, and act on the deployment it made (operator-run, never CI)")]
public abstract class DeployGroup;
