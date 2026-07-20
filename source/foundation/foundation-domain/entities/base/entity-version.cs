#region Purpose
// Pure increment logic for Entity{TId}.Version, extracted out of the EF-side SaveChanges hook so
// the arithmetic has a unit-testable seam that does not require a live DbContext.
#endregion

#region Design
// PostgresDbContext is deliberately entity-free (no DbSet, no OnConfiguring target), so there is no
// way to prove the increment via a real EF SaveChanges round-trip without pulling in a new test
// package (EF InMemory/Sqlite) — out of scope for this task. This static method is the entire
// "logic" the hook depends on; foundation-domain-tests/entity-version-tests.cs exercises it
// directly, and the hook itself (postgres-db-context.cs) is thin glue that reads
// PropertyEntry.OriginalValue, calls this, and writes PropertyEntry.CurrentValue — reviewed by
// inspection rather than by an automated integration test.
#endregion

namespace TimeWarp.Foundation.Entities;

public static class EntityVersion
{
  public static long Next(long originalVersion) => originalVersion + 1;
}
