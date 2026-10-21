#region Purpose
// One protected-secret row: name, purpose, ciphertext, display suffix, and who set it.
#endregion

#region Design
// Not an aggregate. The EF store is the only writer. Ciphertext is Data Protection output for the
// row's purpose; this type never sees the plaintext. A private constructor plus Replace keeps the
// setters off the public surface. Task 398 is the site xAI key. Task 399 adds rows, not columns.
#endregion

namespace TimeWarp.Architecture.Secrets;

/// <summary>One named secret protected at rest. The plaintext is not a property.</summary>
public sealed class ProtectedSecret
{
  private ProtectedSecret()
  {
    Name = "";
    Purpose = "";
    Ciphertext = "";
    Suffix = "";
  }

  /// <summary>Stable name. One row per name.</summary>
  public string Name { get; private set; }

  /// <summary>Data Protection purpose this ciphertext was protected with.</summary>
  public string Purpose { get; private set; }

  /// <summary>Protected payload. Not the plaintext.</summary>
  public string Ciphertext { get; private set; }

  /// <summary>Last characters of the plaintext, for a write-only status line.</summary>
  public string Suffix { get; private set; }

  /// <summary>When the current ciphertext was written.</summary>
  public DateTimeOffset SetAt { get; private set; }

  /// <summary>Principal that wrote the current ciphertext.</summary>
  public Guid SetByPrincipalId { get; private set; }

  /// <summary>A new row. <paramref name="plaintext"/> is not stored.</summary>
  public static ProtectedSecret Create(
    string name,
    string purpose,
    string ciphertext,
    string suffix,
    DateTimeOffset setAt,
    Guid setByPrincipalId)
  {
    ProtectedSecret row = new();
    row.Name = Require(name, nameof(name));
    row.Replace(purpose, ciphertext, suffix, setAt, setByPrincipalId);
    return row;
  }

  /// <summary>Replaces the protected payload. <paramref name="ciphertext"/> is not the plaintext.</summary>
  public void Replace(
    string purpose,
    string ciphertext,
    string suffix,
    DateTimeOffset setAt,
    Guid setByPrincipalId)
  {
    Purpose = Require(purpose, nameof(purpose));
    Ciphertext = Require(ciphertext, nameof(ciphertext));
    Suffix = Require(suffix, nameof(suffix));
    SetAt = setAt;
    SetByPrincipalId = setByPrincipalId;
  }

  private static string Require(string value, string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
    return value;
  }
}
