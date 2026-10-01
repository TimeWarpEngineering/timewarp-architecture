#region Purpose
// Postgres ISiteSettingsTableProbe: to_regclass catalog lookup for the EF-mapped site-settings table.
#endregion

#region Design
// Task 270: to_regclass returns NULL for a missing table or schema instead of raising 42P01, so the
// probe never produces the EF Error entries it exists to avoid. The table name comes from the EF
// model (schema + table of SiteSettings), not a hard-coded string, so a mapping change cannot
// leave the probe checking a table nobody reads. Both parts are double-quoted so to_regclass
// matches them exactly as the migration created them. Scoped: PostgresDbContext is scoped.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Infrastructure;

using Microsoft.EntityFrameworkCore.Metadata;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

public sealed class EfSiteSettingsTableProbe : ISiteSettingsTableProbe
{
  private readonly PostgresDbContext Db;

  public EfSiteSettingsTableProbe(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
  {
    IEntityType entityType = Db.Model.FindEntityType(typeof(SiteSettings))
      ?? throw new InvalidOperationException("SiteSettings is not mapped by PostgresDbContext.");
    string tableName = entityType.GetTableName()
      ?? throw new InvalidOperationException("SiteSettings has no table mapping.");
    string? schema = entityType.GetSchema();
    string qualifiedName = schema is null ? Quote(tableName) : $"{Quote(schema)}.{Quote(tableName)}";

    List<bool> exists = await Db.Database
      .SqlQuery<bool>($"SELECT to_regclass({qualifiedName}) IS NOT NULL AS \"Value\"")
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    return exists.Single();
  }

  private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
