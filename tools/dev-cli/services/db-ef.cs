#region Purpose
// Shared EF Core scaffolding paths and argument builder for `dev db add-migration`.
#endregion

#region Design
// Design-time `dotnet ef` (not Aspire live `web-migrations`). Paths match tw-aggregate-pattern
// Schema evolution. --output-dir is relative to the web-infrastructure project directory.
// Dummy connection comes from PostgresDbContextDesignTimeFactory — no live database required
// for scaffolding. Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include.
//
// Snapshot placement: the tracked migrations live outside the web-infrastructure project
// directory (compile-linked by feature-membership.targets). --output-dir steers the migration and
// designer files there, but EF's MigrationsScaffolder locates the existing snapshot only by
// searching the project directory; it never finds the linked one and writes the updated snapshot
// to <ProjectDir>/<namespace path>/ instead. EF has no option for the snapshot directory, so
// ReconcileSnapshot runs after a successful scaffold: it moves the single misplaced snapshot
// (outside bin/obj) over the tracked one, deletes the directories left empty up to the project
// directory, and reports NotUpdated when neither copy changed (the caller fails loudly).
// The misplaced snapshot is found by file name, not a namespace-derived path, so no namespace
// string beyond MigrationsNamespace is needed (dotnet-new sourceName rewrites that one).
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

  /// <summary>Relative to the repository root; the resolved form of <see cref="MigrationsOutputDir"/>.</summary>
  internal const string MigrationsDirectory = "source/container-apps/web/platform/postgres/migrations";

  /// <summary>EF names the snapshot class <c>&lt;Context&gt;ModelSnapshot</c>.</summary>
  internal const string SnapshotFileName = ContextName + "ModelSnapshot.cs";

  /// <summary>The local tool command `dotnet tool run` resolves (the dotnet-tools.json manifest name).</summary>
  internal const string EfToolCommand = "dotnet-ef";

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

  /// <summary>Arguments after the tool name: <c>dotnet tool run dotnet-ef</c> + these.</summary>
  internal static string[] BuildAddMigrationArguments(string migrationName) =>
  [
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

  /// <summary>
  /// Moves a snapshot EF wrote under <paramref name="infrastructureProjectDirectory"/> over the
  /// tracked one in <paramref name="migrationsDirectory"/> and removes the directories it leaves
  /// empty. <paramref name="trackedSnapshotBefore"/> is the tracked snapshot text read before the
  /// scaffold (null when there was none).
  /// </summary>
  internal static SnapshotReconciliation ReconcileSnapshot(
    string infrastructureProjectDirectory,
    string migrationsDirectory,
    string? trackedSnapshotBefore)
  {
    string projectDirectory = Path.GetFullPath(infrastructureProjectDirectory);
    string trackedSnapshot = Path.Combine(Path.GetFullPath(migrationsDirectory), SnapshotFileName);

    List<string> misplaced = FindMisplacedSnapshots(projectDirectory, trackedSnapshot);
    if (misplaced.Count > 1)
    {
      return new SnapshotReconciliation(SnapshotOutcome.Ambiguous, misplaced, []);
    }

    if (misplaced.Count == 1)
    {
      string source = misplaced[0];
      Directory.CreateDirectory(Path.GetDirectoryName(trackedSnapshot)!);
      File.Move(source, trackedSnapshot, overwrite: true);
      List<string> removed = RemoveEmptyDirectories(Path.GetDirectoryName(source)!, projectDirectory);
      return new SnapshotReconciliation(SnapshotOutcome.Relocated, misplaced, removed);
    }

    string? trackedSnapshotAfter = File.Exists(trackedSnapshot) ? File.ReadAllText(trackedSnapshot) : null;
    SnapshotOutcome outcome = trackedSnapshotAfter is not null && trackedSnapshotAfter != trackedSnapshotBefore
      ? SnapshotOutcome.AlreadyInPlace
      : SnapshotOutcome.NotUpdated;
    return new SnapshotReconciliation(outcome, [], []);
  }

  private static List<string> FindMisplacedSnapshots(string projectDirectory, string trackedSnapshot)
  {
    List<string> found = [];
    if (!Directory.Exists(projectDirectory))
    {
      return found;
    }

    EnumerationOptions options = new() { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseSensitive };
    foreach (string path in Directory.EnumerateFiles(projectDirectory, SnapshotFileName, options))
    {
      string full = Path.GetFullPath(path);
      if (string.Equals(full, trackedSnapshot, StringComparison.Ordinal) || IsBuildOutput(projectDirectory, full))
      {
        continue;
      }

      found.Add(full);
    }

    found.Sort(StringComparer.Ordinal);
    return found;
  }

  private static bool IsBuildOutput(string projectDirectory, string path)
  {
    string first = Path.GetRelativePath(projectDirectory, path)
      .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    return first is "bin" or "obj";
  }

  /// <summary>Deletes <paramref name="start"/> and its parents while empty, stopping below <paramref name="stopAt"/>.</summary>
  private static List<string> RemoveEmptyDirectories(string start, string stopAt)
  {
    List<string> removed = [];
    string stop = Path.TrimEndingDirectorySeparator(stopAt);
    string? current = Path.TrimEndingDirectorySeparator(Path.GetFullPath(start));
    while (current is not null
      && !string.Equals(current, stop, StringComparison.Ordinal)
      && current.StartsWith(stop + Path.DirectorySeparatorChar, StringComparison.Ordinal)
      && !Directory.EnumerateFileSystemEntries(current).Any())
    {
      Directory.Delete(current);
      removed.Add(current);
      current = Path.GetDirectoryName(current);
    }

    return removed;
  }
}

/// <summary>What <see cref="DbEf.ReconcileSnapshot"/> found after <c>migrations add</c>.</summary>
internal enum SnapshotOutcome
{
  /// <summary>A misplaced snapshot was moved over the tracked one.</summary>
  Relocated,

  /// <summary>EF updated the tracked snapshot itself; nothing moved.</summary>
  AlreadyInPlace,

  /// <summary>No misplaced snapshot and the tracked one is unchanged or missing.</summary>
  NotUpdated,

  /// <summary>More than one misplaced snapshot; nothing moved.</summary>
  Ambiguous
}

/// <param name="Outcome">What happened.</param>
/// <param name="MisplacedSnapshots">Full paths of snapshots found under the project directory.</param>
/// <param name="RemovedDirectories">Full paths of emptied directories that were deleted, deepest first.</param>
internal sealed record SnapshotReconciliation(
  SnapshotOutcome Outcome,
  IReadOnlyList<string> MisplacedSnapshots,
  IReadOnlyList<string> RemovedDirectories);
