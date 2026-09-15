#region Purpose
// Nuru group base: all `dev entra *` subcommands inherit this prefix.
#endregion

#region Design
// Same shape as DbGroup: [NuruRouteGroup] on an abstract base, single-literal [NuruRoute] on
// each command. Local Entra setup is a named-scheme companion to 219-002, not DefaultScheme.
#endregion

namespace DevCli.Commands;

using TimeWarp.Nuru;

/// <summary>Base for <c>dev entra …</c> commands.</summary>
[NuruRouteGroup("entra", Description = "Entra: local app registration and Web.Server user secrets")]
public abstract class EntraGroup;
