#region Purpose
// Nuru group base: all `dev publish *` subcommands inherit this prefix.
#endregion

#region Design
// One subcommand per Aspire publish target the AppHost declares (compose today; the Kubernetes/Helm
// environment, task 070-004, adds its own). Same group pattern as db-group.cs.
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev publish …</c> commands.</summary>
[NuruRouteGroup("publish", Description = "Generate deployment artifacts with `aspire publish` and check them for production safety")]
public abstract class PublishGroup;
