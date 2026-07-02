#region Purpose
// Binds the PostgreSQL connection string from configuration for PostgresDbModule and the Postgres environment check.
#endregion

namespace TimeWarp.Architecture.Configuration;

public sealed partial class PostgresDbOptions
{
  public string? ConnectionString { get; set; }
}
