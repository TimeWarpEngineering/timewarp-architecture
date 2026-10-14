#region Purpose
// Postgres IPendingMigrationsProbe: EF's own pending-migrations comparison for PostgresDbContext.
#endregion

#region Design
// Task 363: Database.GetPendingMigrationsAsync() compares the migrations compiled into this
// assembly with the rows in __EFMigrationsHistory. Chosen over "is the latest migration id in the
// history table" because it also catches an older migration that was never applied (a branch
// merged with an earlier timestamp), and because EF owns the history table's name and schema, so
// a MigrationsHistoryTable change cannot leave the probe reading the wrong table.
// Logging: on EF Core 11 / Npgsql, GetPendingMigrationsAsync reads __EFMigrationsHistory without
// checking it exists; on a fresh database that read fails with 42P01, EF logs "Failed executing
// DbCommand" at Error, and only then treats the history as empty. So the probe first asks
// to_regclass (NULL for a missing table, never 42P01 — the task 270 technique) and reports every
// compiled migration while the history table is missing. The table name and schema come from the
// context's relational options (EF's defaults when unset), so a MigrationsHistoryTable change
// moves both checks together; an unset schema stays unqualified so to_regclass and EF resolve it
// through the same search_path. ef-pending-migrations-probe-tests pins "no Error" with the EF
// logger. Cost per call is at most three small queries, logged under
// Microsoft.EntityFrameworkCore.Database.Command, which is Warning in committed appsettings.
// Scoped: PostgresDbContext is scoped.
#endregion

namespace TimeWarp.Architecture.Persistence;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

public sealed class EfPendingMigrationsProbe : IPendingMigrationsProbe
{
  private readonly PostgresDbContext Db;

  public EfPendingMigrationsProbe(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
  {
    if (!await HistoryTableExistsAsync(cancellationToken).ConfigureAwait(false))
    {
      return [.. Db.Database.GetMigrations()];
    }

    IEnumerable<string> pending = await Db.Database
      .GetPendingMigrationsAsync(cancellationToken)
      .ConfigureAwait(false);
    return [.. pending];
  }

  private async Task<bool> HistoryTableExistsAsync(CancellationToken cancellationToken)
  {
    var relationalOptions = RelationalOptionsExtension.Extract(Db.GetService<IDbContextOptions>());
    string tableName = relationalOptions.MigrationsHistoryTableName ?? HistoryRepository.DefaultTableName;
    string? schema = relationalOptions.MigrationsHistoryTableSchema;
    string qualifiedName = schema is null ? Quote(tableName) : $"{Quote(schema)}.{Quote(tableName)}";

    List<bool> exists = await Db.Database
      .SqlQuery<bool>($"SELECT to_regclass({qualifiedName}) IS NOT NULL AS \"Value\"")
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    return exists.Single();
  }

  private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
