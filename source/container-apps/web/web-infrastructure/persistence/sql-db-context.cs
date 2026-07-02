#region Purpose
// Placeholder EF Core context marking the seam for a SQL Server storage option alongside Postgres.
#endregion

#region Design
// Unregistered dead code kept as reference: its only mention is a commented-out
// AddDbContextCheck line in Web.Server's Program, showing where SQL Server wiring would go.
#endregion

namespace TimeWarp.Architecture.Data;

public class SqlDbContext : DbContext { }
