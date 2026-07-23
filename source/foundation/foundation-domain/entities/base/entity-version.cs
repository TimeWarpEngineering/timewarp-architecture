#region Purpose
// Pure increment logic for Entity{TId}.Version, extracted out of the EF-side SaveChanges hook so
// the arithmetic has a unit-testable seam that does not require a live DbContext.
#endregion

#region Design
// Pure arithmetic only — no EF dependency. foundation-domain-tests/entity-version-tests.cs
// exercises Next directly; the EF SaveChanges hook that calls it lives in GoldenDbContext
// (foundation-infrastructure) and is covered by foundation-infrastructure-tests/golden-db-context-tests.cs
// (InMemory harness: root modify, child-only mutate, fail-closed missing Version).
#endregion

namespace TimeWarp.Foundation.Entities;

public static class EntityVersion
{
  public static long Next(long originalVersion) => originalVersion + 1;
}
