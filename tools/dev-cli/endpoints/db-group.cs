#region Purpose
// Nuru group base: all `dev db *` subcommands inherit this prefix.
#endregion

#region Design
// Pattern from TimeWarp.Nuru samples / crunchit ccc group: [NuruRouteGroup] on abstract base,
// single-literal [NuruRoute] on each command class. Flat alias `db-update` stays outside the group.
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev db …</c> commands.</summary>
[NuruRouteGroup("db", Description = "Database: migrations and greenfield wipe against the running AppHost")]
public abstract class DbGroup;
