#region Purpose
// The psql commands, `__EFMigrationsHistory` parsing and the migration reports of
// `dev deploy migrate`.
#endregion

#region Design
// psql: PGPASSWORD comes from the postgres container's own environment (the image enforces password
// auth even on the in-container socket, and a piped session cannot prompt), so the password never
// passes through the dev CLI. The script is piped on stdin (`exec -T` / `exec -i`), so no TTY.
//
// Report (task 339): the idempotent script's transcript looks the same whether ten migrations applied or
// none, so the verb reports from EF's documented state instead — `__EFMigrationsHistory` read with psql
// before and after the run through the same exec path (a missing table, SQLSTATE 42P01, is a fresh
// database: none applied). The script itself runs quietly (-q, client_min_messages=warning) so only errors
// and WARNINGs print; --verbose keeps psql's full transcript. psql's DO lines are never parsed. ACA has no
// psql on the operator's machine; the bundle's own `Applying migration '…'` lines (EF's log output) give
// its summary.
#endregion

namespace DevCli.Services;

/// <summary>The psql commands, <c>__EFMigrationsHistory</c> parsing and migration reports of <c>dev deploy migrate</c>.</summary>
internal static partial class DeployMigrationHistory
{
  /// <summary>psql inside the postgres container; the password comes from the container's environment.</summary>
  private const string Psql = "PGPASSWORD=\"$POSTGRES_PASSWORD\" psql -U postgres -d " + DeployOperate.PostgresDatabaseName;

  /// <summary>
  /// The script run with psql's full transcript (<c>--verbose</c>): every statement tag and NOTICE, as psql prints them.
  /// </summary>
  internal const string PsqlTranscriptCommand = Psql + " -v ON_ERROR_STOP=1";

  /// <summary>
  /// The script run quietly (the default): <c>-q</c> drops the statement tags and <c>client_min_messages=warning</c> drops the
  /// NOTICEs (the idempotent script's "__EFMigrationsHistory already exists, skipping"); errors and WARNINGs still print.
  /// </summary>
  internal const string PsqlQuietCommand =
    "PGPASSWORD=\"$POSTGRES_PASSWORD\" PGOPTIONS='-c client_min_messages=warning' psql -U postgres -d " + DeployOperate.PostgresDatabaseName
    + " -q -v ON_ERROR_STOP=1";

  internal const string MigrationHistoryTable = "__EFMigrationsHistory";

  /// <summary>
  /// Reads EF's applied migration ids, one per line, oldest first. <c>VERBOSITY=sqlstate</c> makes a missing table (fresh
  /// database) print <see cref="UndefinedTableSqlState"/> instead of localized text.
  /// </summary>
  internal const string PsqlHistoryCommand =
    Psql + " -At -v VERBOSITY=sqlstate -c 'SELECT \"MigrationId\" FROM \"" + MigrationHistoryTable + "\" ORDER BY \"MigrationId\"'";

  /// <summary>PostgreSQL's SQLSTATE for undefined_table.</summary>
  internal const string UndefinedTableSqlState = "42P01";

  /// <summary>The psql command that runs the script: quiet unless <paramref name="verbose"/>.</summary>
  internal static string PsqlScriptCommand(bool verbose) => verbose ? PsqlTranscriptCommand : PsqlQuietCommand;

  /// <summary>
  /// The applied ids from <see cref="PsqlHistoryCommand"/>: its lines on success, empty when the table does not exist yet
  /// (<see cref="UndefinedTableSqlState"/>, a fresh database), null when the history could not be read.
  /// </summary>
  internal static string[]? ParseMigrationHistory(int exitCode, string stdout, string stderr) =>
    exitCode == 0 ? stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : stderr.Contains(UndefinedTableSqlState, StringComparison.Ordinal) ? []
    : null;

  /// <summary>The ids in <paramref name="after"/> that are not in <paramref name="before"/>, in <paramref name="after"/>'s order.</summary>
  internal static string[] NewlyAppliedMigrations(IReadOnlyList<string> before, IReadOnlyList<string> after)
  {
    HashSet<string> already = new(before, StringComparer.Ordinal);
    return [.. after.Where(id => !already.Contains(id))];
  }

  /// <summary>
  /// What the run changed, from <c>__EFMigrationsHistory</c> read before and after it (null = unreadable): the ids applied,
  /// the count already applied and the latest id; or "Up to date"; or, on failure, the last id applied before it failed.
  /// </summary>
  internal static string[] BuildMigrationReport(IReadOnlyList<string>? before, IReadOnlyList<string>? after, int exitCode)
  {
    if (exitCode != 0)
    {
      string[] applied = before is not null && after is not null ? NewlyAppliedMigrations(before, after) : [];
      string failed = after is null
        ? $"Failed while applying migrations ({MigrationHistoryTable} could not be read after the failure)"
        : after.Count == 0 ? "Failed before any migration was applied"
        : $"Failed while applying migrations after {after[^1]}";
      return applied.Length == 0 ? [failed] : [.. AppliedLines(applied), failed];
    }

    if (before is null || after is null)
    {
      return [$"Migrations ran, but {MigrationHistoryTable} could not be read {(before is null ? "before" : "after")} the run, so which ones applied is unknown"];
    }

    string[] newlyApplied = NewlyAppliedMigrations(before, after);
    string latest = after.Count == 0 ? "" : after[^1];
    if (newlyApplied.Length == 0)
    {
      return [after.Count == 0 ? "Up to date: no pending migrations (0 applied)" : $"Up to date: no pending migrations ({after.Count} applied, latest {latest})"];
    }

    return [.. AppliedLines(newlyApplied), $"Already applied: {after.Count - newlyApplied.Length} · Database now at {latest}"];

    static IEnumerable<string> AppliedLines(string[] ids) => [$"Applied {ids.Length} migration(s):", .. ids.Select(id => $"  {id}")];
  }

  /// <summary>
  /// The ACA summary from the EF bundle's own output: the ids of its <c>Applying migration '…'</c> lines, or "Up to date"
  /// when there are none; on failure, the last id it started.
  /// </summary>
  internal static string[] BuildBundleReport(string bundleOutput, int exitCode)
  {
    string[] applying =
    [
      .. bundleOutput.Split('\n')
        .Select(line => BundleApplyingPattern().Match(line))
        .Where(match => match.Success)
        .Select(match => match.Groups[1].Value),
    ];
    if (exitCode != 0)
    {
      // The last "Applying migration" is the one that failed (each runs in its own transaction); the ones before it applied.
      if (applying.Length == 0) return ["Failed before any migration was applied"];
      string[] appliedBeforeFailure = applying[..^1];
      return appliedBeforeFailure.Length == 0
        ? [$"Failed while applying {applying[^1]}"]
        : [$"Applied {appliedBeforeFailure.Length} migration(s):", .. appliedBeforeFailure.Select(id => $"  {id}"), $"Failed while applying {applying[^1]}"];
    }

    return applying.Length == 0
      ? ["Up to date: no pending migrations"]
      : [$"Applied {applying.Length} migration(s):", .. applying.Select(id => $"  {id}"), $"Database now at {applying[^1]}"];
  }

  [System.Text.RegularExpressions.GeneratedRegex(@"Applying migration '([^']+)'")]
  private static partial System.Text.RegularExpressions.Regex BundleApplyingPattern();
}
