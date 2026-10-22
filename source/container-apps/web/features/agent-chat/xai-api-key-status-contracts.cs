#region Purpose
// Shared write-only status for the site xAI API key. No property holds the key.
#endregion

#region Design
// Get, set, and clear each have their own Response, and each Response is this shape. The SPA
// binds the interface. Suffix is the last four characters when an admin key is the one Ask will
// use, or when a stored row cannot be read. It is null when the host key wins, so the host
// secret's tail is not sent to the browser. SetupHint is how to set the host value. It does not
// tell the reader to open Admin → AI.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>What Admin → AI may show. The key itself is not a member.</summary>
public interface IXaiApiKeyStatus
{
  /// <summary>True when <c>XAI:ApiKey</c> is set on the host. The page is read-only.</summary>
  bool ManagedByHost { get; }

  /// <summary>True when Ask has a key, from the host or from a readable admin secret.</summary>
  bool Configured { get; }

  /// <summary>True when this host can store an admin key that survives a restart.</summary>
  bool StorageAvailable { get; }

  /// <summary>True when an admin may set, replace, or clear the stored key.</summary>
  bool CanChange { get; }

  /// <summary>True when a row is stored but Ask cannot read it. Paste a new key.</summary>
  bool NeedsReplacement { get; }

  /// <summary>Last four characters of the stored admin key. Null when the host key wins or no row is stored.</summary>
  string? Suffix { get; }

  /// <summary>When the active admin key was written.</summary>
  DateTimeOffset? SetAt { get; }

  /// <summary>Principal that wrote the active admin key.</summary>
  Guid? SetByPrincipalId { get; }

  /// <summary>How to set the host value. Does not mention Admin → AI.</summary>
  string SetupHint { get; }
}

/// <summary>Ctor-backed <see cref="IXaiApiKeyStatus"/>.</summary>
public class XaiApiKeyStatus : IXaiApiKeyStatus
{
  /// <summary>Creates a status. <paramref name="setupHint"/> is required. The key is not a parameter.</summary>
  public XaiApiKeyStatus(
    bool managedByHost,
    bool configured,
    bool storageAvailable,
    bool canChange,
    bool needsReplacement,
    string? suffix,
    DateTimeOffset? setAt,
    Guid? setByPrincipalId,
    string setupHint)
  {
    ManagedByHost = managedByHost;
    Configured = configured;
    StorageAvailable = storageAvailable;
    CanChange = canChange;
    NeedsReplacement = needsReplacement;
    Suffix = suffix;
    SetAt = setAt;
    SetByPrincipalId = setByPrincipalId;
    SetupHint = Guard.Against.NullOrWhiteSpace(setupHint);
  }

  /// <inheritdoc />
  public bool ManagedByHost { get; }

  /// <inheritdoc />
  public bool Configured { get; }

  /// <inheritdoc />
  public bool StorageAvailable { get; }

  /// <inheritdoc />
  public bool CanChange { get; }

  /// <inheritdoc />
  public bool NeedsReplacement { get; }

  /// <inheritdoc />
  public string? Suffix { get; }

  /// <inheritdoc />
  public DateTimeOffset? SetAt { get; }

  /// <inheritdoc />
  public Guid? SetByPrincipalId { get; }

  /// <inheritdoc />
  public string SetupHint { get; }
}
