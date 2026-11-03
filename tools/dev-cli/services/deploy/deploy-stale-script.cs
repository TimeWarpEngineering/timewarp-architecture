#region Purpose
// Detects a published migration script older than the source migrations, and words the warning
// and the unattended refusal.
#endregion

#region Design
// Stale script (task 341): "Up to date" is only true relative to the published script, which may come from an
// older `dev publish`. Before the plan, the verb compares the migration ids in the source (file names in the
// AppHost's sibling apps' platform/postgres/migrations folders — FindSourceMigrationFolders, not one hard-coded
// app; none means no postgres, so the check is skipped) with the ids in the script's __EFMigrationsHistory
// inserts (EF's stable idempotent-script output). Source ids the script lacks print StaleScriptWarning before
// the prompt; --yes then REFUSES unless --allow-stale, so an unattended run never silently applies an old
// script; and the report says "relative to the published script". aca: the bundle is a compiled binary whose
// migration list is not readable without EF internals, but the same publish writes web-migrations.sql beside it
// (PublishAsMigrationScript runs for every target), so the check reads that script; with no script beside the
// bundle it says the check is unavailable and skips it.
#endregion

namespace DevCli.Services;

/// <summary>Compares the migration ids in the source with those in the published script, and words the stale-script warning and refusal.</summary>
internal static partial class DeployStaleScript
{
  /// <summary>Under an app folder (a sibling of the AppHost's tree): where EF writes that app's Postgres migrations.</summary>
  internal static readonly string SourceMigrationsSubpath = Path.Combine("platform", "postgres", "migrations");

  internal const string RelativeToPublishedScript = "relative to the published script";

  /// <summary>
  /// The <c>platform/postgres/migrations</c> folders of the AppHost's sibling apps: the nearest ancestor of the AppHost
  /// (up to <paramref name="repoRoot"/>) with a child app holding one. Empty when none exists (an app without postgres).
  /// </summary>
  internal static string[] FindSourceMigrationFolders(string appHostProject, string repoRoot)
  {
    string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repoRoot));
    for (DirectoryInfo? directory = new FileInfo(Path.GetFullPath(appHostProject)).Directory;
      directory?.FullName.StartsWith(root, StringComparison.Ordinal) == true;
      directory = directory.Parent)
    {
      string[] folders =
      [
        .. directory.EnumerateDirectories()
          .Select(app => Path.Combine(app.FullName, SourceMigrationsSubpath))
          .Where(Directory.Exists)
          .Order(StringComparer.Ordinal),
      ];
      if (folders.Length > 0) return folders;
    }

    return [];
  }

  /// <summary>
  /// Migration ids from a migrations folder's file names: <c>&lt;timestamp&gt;_&lt;Name&gt;.cs</c>, without the
  /// <c>.Designer.cs</c> files and the model snapshot; ordered.
  /// </summary>
  internal static string[] ParseSourceMigrationIds(IEnumerable<string> fileNames) =>
  [
    .. fileNames
      .Select(name => SourceMigrationFilePattern().Match(Path.GetFileName(name)))
      .Where(match => match.Success)
      .Select(match => match.Groups[1].Value)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal),
  ];

  /// <summary>The ids the published script records: each migration's <c>__EFMigrationsHistory</c> insert's <c>'&lt;MigrationId&gt;'</c>.</summary>
  internal static string[] ParseScriptMigrationIds(string script) =>
  [
    .. ScriptHistoryInsertPattern().Matches(script)
      .Select(match => match.Groups[1].Value)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal),
  ];

  /// <summary>Source ids the script lacks, ordered: non-empty means the published script is older than the source.</summary>
  internal static string[] MigrationsMissingFromScript(IEnumerable<string> sourceIds, IEnumerable<string> scriptIds)
  {
    HashSet<string> published = new(scriptIds, StringComparer.Ordinal);
    return [.. sourceIds.Where(id => !published.Contains(id)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
  }

  /// <summary>The warning printed before the confirmation when the published script is stale.</summary>
  internal static string StaleScriptWarning(DeployTarget target, IReadOnlyList<string> missing) =>
    $"⚠ Published script is stale: source has {missing.Count} migration(s) the script does not\n"
    + $"  ({string.Join(", ", missing)}). Run `dev publish {target.Name}` first,\n"
    + "  then redeploy so the app and schema match.";

  /// <summary>Unattended (<c>--yes</c>) runs never apply a stale script unless <c>--allow-stale</c> says so.</summary>
  internal static bool RefuseStaleUnattended(bool stale, bool yes, bool allowStale) => stale && yes && !allowStale;

  internal static string StaleUnattendedRefusal(DeployTarget target) =>
    $"Not migrating: the published script is stale and --yes was passed. Run `dev publish {target.Name}` and redeploy, "
    + "or pass --allow-stale to apply the old script anyway.";

  /// <summary>Printed for aca when no script sits beside the bundle: the bundle's own migrations are not listed.</summary>
  internal const string BundleStaleCheckUnavailable =
    "Stale check not available for the bundle: no web-migrations.sql beside it to read its migration ids from; skipped.";

  /// <summary>
  /// A successful report qualified "relative to the published script" (after a stale warning): the "Up to date" and
  /// "Applied" lines say what they are measured against.
  /// </summary>
  internal static string[] QualifyRelativeToPublishedScript(IReadOnlyList<string> lines) =>
  [
    .. lines.Select(line =>
      line.StartsWith("Up to date:", StringComparison.Ordinal) ? $"Up to date {RelativeToPublishedScript}:{line["Up to date:".Length..]}"
      : line.StartsWith("Applied ", StringComparison.Ordinal) && line.EndsWith(" migration(s):", StringComparison.Ordinal)
        ? $"{line[..^1]} {RelativeToPublishedScript}:"
      : line),
  ];

  [System.Text.RegularExpressions.GeneratedRegex(@"^(\d+_[A-Za-z0-9_]+)\.cs$")]
  private static partial System.Text.RegularExpressions.Regex SourceMigrationFilePattern();

  [System.Text.RegularExpressions.GeneratedRegex(@"INSERT\s+INTO\s+""__EFMigrationsHistory""\s*\(\s*""MigrationId""\s*,\s*""ProductVersion""\s*\)\s*VALUES\s*\(\s*'([^']+)'")]
  private static partial System.Text.RegularExpressions.Regex ScriptHistoryInsertPattern();
}
