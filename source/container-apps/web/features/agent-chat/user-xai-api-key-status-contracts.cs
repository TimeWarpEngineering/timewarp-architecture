#region Purpose
// Write-only status of the signed-in user's xAI API key. No property holds the key.
#endregion

#region Design
// Visible is false when the site mode is Off, and then Suffix is null so a hidden section does
// not describe a stored key. SupersededByHost is true when XAI:ApiKey is set: the user's row may
// still exist, and Ask will not use it until the host key is cleared. CanChange is false when
// the mode is Off or this host cannot store a secret.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using TimeWarp.Identity;

/// <summary>What Profile may show about the current user's xAI API key.</summary>
public interface IUserXaiApiKeyStatus
{
  /// <summary>False when the site mode is Off. Profile renders no section.</summary>
  bool Visible { get; }

  /// <summary>The site mode that produced this status.</summary>
  XaiByokMode Mode { get; }

  /// <summary>True when this user's key is stored and can be read.</summary>
  bool Configured { get; }

  /// <summary>True when a row is stored and cannot be read.</summary>
  bool NeedsReplacement { get; }

  /// <summary>True when this user may set, replace, or clear a key.</summary>
  bool CanChange { get; }

  /// <summary>True when this host can store a secret that survives a restart.</summary>
  bool StorageAvailable { get; }

  /// <summary>True when the host key is set, so Ask will not use this user's key.</summary>
  bool SupersededByHost { get; }

  /// <summary>Last four characters of this user's key. Null when Off, or when no row is stored.</summary>
  string? Suffix { get; }
}

/// <summary>Ctor-backed <see cref="IUserXaiApiKeyStatus"/>.</summary>
public class UserXaiApiKeyStatus : IUserXaiApiKeyStatus
{
  /// <summary>Creates a status. The key is not a parameter.</summary>
  public UserXaiApiKeyStatus(
    bool visible,
    XaiByokMode mode,
    bool configured,
    bool needsReplacement,
    bool canChange,
    bool storageAvailable,
    bool supersededByHost,
    string? suffix)
  {
    Visible = visible;
    Mode = mode;
    Configured = configured;
    NeedsReplacement = needsReplacement;
    CanChange = canChange;
    StorageAvailable = storageAvailable;
    SupersededByHost = supersededByHost;
    Suffix = suffix;
  }

  /// <inheritdoc />
  public bool Visible { get; }

  /// <inheritdoc />
  public XaiByokMode Mode { get; }

  /// <inheritdoc />
  public bool Configured { get; }

  /// <inheritdoc />
  public bool NeedsReplacement { get; }

  /// <inheritdoc />
  public bool CanChange { get; }

  /// <inheritdoc />
  public bool StorageAvailable { get; }

  /// <inheritdoc />
  public bool SupersededByHost { get; }

  /// <inheritdoc />
  public string? Suffix { get; }
}
