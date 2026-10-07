#region Purpose
// Nuru group base: all `dev publish *` subcommands inherit this prefix.
#endregion

#region Design
// One subcommand per Aspire publish target the AppHost declares (compose, task 070-003; kubernetes /
// Helm, task 070-004; aca / Azure Container Apps Bicep, task 070-007), sharing services/aspire-publish.cs. Same group pattern as db-group.cs.
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev publish …</c> commands.</summary>
[NuruRouteGroup("publish", Description = "Generate deployment artifacts with `aspire publish` and check them for production safety")]
public abstract class PublishGroup;
