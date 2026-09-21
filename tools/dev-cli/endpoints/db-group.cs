#region Purpose
// Nuru group base: all `dev db *` subcommands inherit this prefix.
#endregion

#region Design
// Pattern from TimeWarp.Nuru samples / crunchit ccc group: [NuruRouteGroup] on abstract base,
// single-literal [NuruRoute] on each command class. Flat alias `db-update` stays outside the group.
// Live AppHost verbs: update / drop / reset / status. Scaffolding: add-migration (design-time
// `dotnet ef`, no running AppHost).
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev db …</c> commands.</summary>
[NuruRouteGroup("db", Description = "Database: scaffold migrations, apply/drop/reset/status via the running AppHost")]
public abstract class DbGroup;
