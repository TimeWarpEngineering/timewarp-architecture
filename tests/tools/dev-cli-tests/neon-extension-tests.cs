#region Purpose
// Records that the committed Postgres migrations do not create an extension Neon would have to provide.
#endregion

#region Design
// Task 317: Neon Free supports the core Postgres types these migrations use. A migration that adds
// CREATE EXTENSION or HasPostgresExtension has to be checked against
// https://neon.com/docs/extensions/pg-extensions before it can ship on aca-neon. pg_trgm is only a
// comment in the profile entity configuration today, and Neon lists it for Postgres 14–18.
#endregion

namespace NeonExtensions_;

public class Migrations_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Migrations_Given_>();

  public static Task CommittedMigrations_Should_CreateNoExtension()
  {
    string migrations = Path.Combine(RepoRoot(), "source", "container-apps", "web", "platform", "postgres", "migrations");
    Directory.Exists(migrations).ShouldBeTrue(migrations);

    foreach (string file in Directory.EnumerateFiles(migrations, "*.cs", SearchOption.AllDirectories))
    {
      string text = File.ReadAllText(file);
      text.Contains("CREATE EXTENSION", StringComparison.OrdinalIgnoreCase)
        .ShouldBeFalse($"{file} creates an extension; confirm Neon offers it before shipping aca-neon");
      text.Contains("HasPostgresExtension", StringComparison.Ordinal)
        .ShouldBeFalse($"{file} calls HasPostgresExtension; confirm Neon offers it before shipping aca-neon");
    }

    return Task.CompletedTask;
  }

  private static string RepoRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !Path.Exists(Path.Combine(directory.FullName, ".git")))
    {
      directory = directory.Parent;
    }

    return directory.ShouldNotBeNull().FullName;
  }
}
