#region Purpose
// Canonical UTF-8 join key for EntraAccount credentials: "{tid}:{oid}" lowercase GUID D-format.
#endregion

#region Design
// Join key is tid:oid, never email, UPN, or OIDC sub (RFC 219 D2). Encode always emits lowercase
// Guid "D" form so unique (Type, Handle) cannot split the same account across casings.
// TryDecode accepts only that canonical encoding (uppercase / other Guid formats fail).
// Library stays Graph-free — no Graph types, no UPN lookup.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Canonical UTF-8 EntraAccount join key <c>{tid}:{oid}</c> (lowercase GUID D-format) — never email, UPN, or sub.
/// </summary>
public static class EntraAccountHandle
{
  private const int GuidDLength = 36;
  private const int CanonicalLength = GuidDLength + 1 + GuidDLength;

  /// <summary>
  /// Encodes tenant id and object id as UTF-8 <c>{tid}:{oid}</c> with canonical lowercase GUID "D" form.
  /// </summary>
  public static byte[] Encode(Guid tenantId, Guid objectId)
  {
    string text = $"{tenantId:D}:{objectId:D}";
    return Encoding.UTF8.GetBytes(text);
  }

  /// <summary>
  /// Decodes a canonical UTF-8 <c>{tid}:{oid}</c> handle. Returns false for non-canonical encodings.
  /// </summary>
  public static bool TryDecode(ReadOnlySpan<byte> handle, out Guid tenantId, out Guid objectId)
  {
    tenantId = default;
    objectId = default;

    if (handle.Length != CanonicalLength || handle[GuidDLength] != (byte)':')
    {
      return false;
    }

    if (!Ascii.IsValid(handle))
    {
      return false;
    }

    string text = Encoding.UTF8.GetString(handle);
    if (!Guid.TryParseExact(text.AsSpan(0, GuidDLength), "D", out tenantId)
        || !Guid.TryParseExact(text.AsSpan(GuidDLength + 1), "D", out objectId))
    {
      tenantId = default;
      objectId = default;
      return false;
    }

    if (!text.Equals($"{tenantId:D}:{objectId:D}", StringComparison.Ordinal))
    {
      tenantId = default;
      objectId = default;
      return false;
    }

    return true;
  }
}
