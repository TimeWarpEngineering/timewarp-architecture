#region Purpose
// Authentication material bound to a principal: passkey or agent key with handle, public material, and revoke lifecycle.
#endregion

#region Design
// Multi-credential by model: many Credential rows per PrincipalId (list/revoke APIs come later). Handle is the lookup key
// (credential id / key id); PublicMaterial is verification material (COSE/public key). Create copies inputs; getters return
// fresh copies so callers cannot mutate stored material. Empty PrincipalId is rejected at Create. Revoke is one-shot.
// Timestamps use DateTimeOffset for unambiguous UTC.
#endregion

namespace TimeWarp.Identity;

public sealed class Credential
{
  private readonly byte[] HandleField;
  private readonly byte[] PublicMaterialField;

  private Credential(
    Guid id,
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    DateTimeOffset createdAt,
    string? label)
  {
    Id = id;
    PrincipalId = principalId;
    Type = type;
    HandleField = handle;
    PublicMaterialField = publicMaterial;
    CreatedAt = createdAt;
    Label = label;
  }

  public Guid Id { get; }
  public PrincipalId PrincipalId { get; }
  public CredentialType Type { get; }

#pragma warning disable CA1819 // Binary material is intentionally exposed as byte[] copies
  public byte[] Handle => HandleField.ToArray();
  public byte[] PublicMaterial => PublicMaterialField.ToArray();
#pragma warning restore CA1819

  public DateTimeOffset CreatedAt { get; }
  public DateTimeOffset? RevokedAt { get; private set; }
  public string? Label { get; }
  public bool IsRevoked => RevokedAt is not null;

  public static Credential Create(
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    string? label = null)
  {
    ArgumentNullException.ThrowIfNull(handle);
    ArgumentNullException.ThrowIfNull(publicMaterial);

    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
    }

    if (!Enum.IsDefined(type))
    {
      throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CredentialType value.");
    }

    if (handle.Length == 0)
    {
      throw new ArgumentException("Handle must be non-empty.", nameof(handle));
    }

    if (publicMaterial.Length == 0)
    {
      throw new ArgumentException("PublicMaterial must be non-empty.", nameof(publicMaterial));
    }

    string? normalizedLabel = NormalizeLabel(label);

    return new Credential(
      Guid.CreateVersion7(),
      principalId,
      type,
      handle.ToArray(),
      publicMaterial.ToArray(),
      DateTimeOffset.UtcNow,
      normalizedLabel);
  }

  public void Revoke()
  {
    if (RevokedAt is not null)
    {
      throw new InvalidOperationException("Credential is already revoked.");
    }

    RevokedAt = DateTimeOffset.UtcNow;
  }

  private static string? NormalizeLabel(string? label)
  {
    if (label is null)
    {
      return null;
    }

    string trimmed = label.Trim();
    return trimmed.Length == 0 ? null : trimmed;
  }
}
