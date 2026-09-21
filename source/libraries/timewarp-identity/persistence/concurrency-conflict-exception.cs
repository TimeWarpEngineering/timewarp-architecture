#region Purpose
// Thrown by IPrincipalStore's Update* members when the caller's Version does not match the stored
// row's Version — the optimistic-concurrency conflict signal (task 104-028, supersedes 104-002 D6
// last-write-wins).
#endregion

#region Design
// The three standard exception constructors (CA1032, not suppressed for source/libraries) exist for
// framework/test scaffolding; the aggregate-aware constructor is the one the store actually uses.
// ExpectedVersion is the CALLER's (the in-hand instance's) Version; ActualVersion is the STORE's
// current Version for that id — naming mirrors "expected vs actual" test-assertion convention, not
// "who is right." Store state is untouched when this throws (see IPrincipalStore's Design region for
// the full port contract): the caller must re-Get to obtain a fresh snapshot before retrying.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Optimistic-concurrency conflict: the caller's entity version does not match the stored row.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
  /// <summary>Initializes a new instance of the exception.</summary>
  public ConcurrencyConflictException()
  {
  }

  /// <summary>Initializes a new instance of the exception.</summary>
  public ConcurrencyConflictException(string message) : base(message)
  {
  }

  /// <summary>Initializes a new instance of the exception.</summary>
  public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
  {
  }

  /// <summary>
  /// Builds the store conflict signal with entity identity and expected-vs-actual versions.
  /// </summary>
  public ConcurrencyConflictException(Type entityType, string entityId, long expectedVersion, long actualVersion)
    : base(BuildMessage(entityType, entityId, expectedVersion, actualVersion))
  {
    EntityType = entityType;
    EntityId = entityId;
    ExpectedVersion = expectedVersion;
    ActualVersion = actualVersion;
  }

  /// <summary>CLR type of the conflicting aggregate, when raised by a store.</summary>
  public Type? EntityType { get; }

  /// <summary>String form of the conflicting entity id, when raised by a store.</summary>
  public string? EntityId { get; }

  /// <summary>Version on the caller's in-hand snapshot.</summary>
  public long ExpectedVersion { get; }

  /// <summary>Version on the stored row for that id.</summary>
  public long ActualVersion { get; }

  private static string BuildMessage(Type entityType, string entityId, long expectedVersion, long actualVersion) =>
    $"Concurrency conflict updating {entityType.Name} '{entityId}': expected version {expectedVersion} but the store has version {actualVersion}. Reload and retry.";
}
