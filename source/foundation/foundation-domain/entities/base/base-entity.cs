#region Purpose
// Common base for domain entities so every entity carries a Guid identity regardless of database provider.
#endregion

namespace TimeWarp.Foundation.Entities;
public abstract class BaseEntity
{
  public Guid Guid { get; set; }
}
