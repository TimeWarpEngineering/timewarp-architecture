#region Purpose
// Shared EF Core scaffolding paths and argument builder for `dev db add-migration`.
#endregion

#region Design
// Design-time `dotnet ef` (not Aspire live `web-migrations`). Paths match tw-aggregate-pattern
// Schema evolution. --output-dir is relative to the web-infrastructure project directory.
// Dummy connection comes from PostgresDbContextDesignTimeFactory — no live database required
// for scaffolding. Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include.
#endregion

namespace DevCli.Services;

/// <summary>Constants + argument builder for EF <c>migrations add</c>.</summary>
internal static class DbEf
{
  internal const string InfrastructureProject =
    "source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj";

  internal const string StartupProject =
    "source/container-apps/web/projects/web-server/web-server.csproj";

  internal const string ContextName = "PostgresDbContext";

  /// <summary>Relative to the web-infrastructure project directory.</summary>
  internal const string MigrationsOutputDir = "../../platform/postgres/migrations";

  internal const string MigrationsNamespace = "TimeWarp.Architecture.Persistence.Migrations";

  internal static bool IsValidMigrationName(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return false;
    }

    char first = name[0];
    if (!char.IsAsciiLetter(first) && first != '_')
    {
      return false;
    }

    for (int index = 1; index < name.Length; index++)
    {
      char character = name[index];
      if (!char.IsAsciiLetterOrDigit(character) && character != '_')
      {
        return false;
      }
    }

    return true;
  }

  internal static string[] BuildAddMigrationArguments(string migrationName) =>
  [
    "ef",
    "migrations",
    "add",
    migrationName,
    "--project",
    InfrastructureProject,
    "--startup-project",
    StartupProject,
    "--context",
    ContextName,
    "--output-dir",
    MigrationsOutputDir,
    "--namespace",
    MigrationsNamespace
  ];
}
