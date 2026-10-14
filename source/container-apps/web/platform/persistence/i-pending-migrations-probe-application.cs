#region Purpose
// Lists the migrations this build was compiled with that the database has not applied, without a query that fails (and logs Error).
#endregion

#region Design
// Task 363 (replaces task 270's site-settings table probe): web-server can boot before
// web-migrations has finished — the AppHost has no wait edge (see its Design region). A boot-time
// read against a schema that is behind fails: 42P01 on an empty database, 42703 when a migration
// added a column to a table that already exists (the 2026-10-14 AssistantName incident). Asking
// "does this table exist" covers only the first case, so the question is "is any migration
// pending". SiteSettingsSeedHostedService asks this port and waits while the list is non-empty.
// Optional: only a store backend that can be unmigrated registers one (PostgresDbModule); with
// none registered (in-memory) boot readers run straight away.
#endregion

namespace TimeWarp.Architecture.Persistence;

public interface IPendingMigrationsProbe
{
  /// <summary>
  /// Ids of the compiled migrations the database has not applied, oldest first. Empty when the
  /// schema is current. A fresh database (no history table) reports every migration; never throws
  /// for a missing table.
  /// </summary>
  Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}
