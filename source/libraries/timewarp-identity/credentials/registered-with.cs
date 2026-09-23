#region Purpose
// Registration context captured once when a credential is created: authenticator attachment plus the browser and OS family the registering request came from.
#endregion

#region Design
// Task 248-001: three passkeys from the same provider render identically without this. Browser and
// Os are SHORT FAMILY NAMES parsed server-side ("Chrome", "Windows") — never the raw User-Agent, which
// is high-entropy and would put a tracking-grade string on every credential row and on the wire.
// Value object (record): equality by value; immutable after Create (there is no "re-register" — a
// new ceremony is a new credential). Unknown is the fail-safe for agent keys / Entra accounts / a
// request with no usable User-Agent. Family strings are trimmed and capped at MaxFamilyLength so a
// hostile UA cannot inflate a column; empty normalizes to null.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Immutable registration context: attachment, browser family, OS family.
/// </summary>
public sealed record RegisteredWith
{
  /// <summary>Upper bound for <see cref="Browser"/> and <see cref="Os"/> after trimming.</summary>
  public const int MaxFamilyLength = 32;

  /// <summary>Nothing captured — agent keys, Entra accounts, or a request that exposed no context.</summary>
  public static RegisteredWith Unknown { get; } = new(AuthenticatorAttachment.Unknown, browser: null, os: null);

  /// <summary>Creates a context; family strings are trimmed, capped, and empty normalizes to null.</summary>
  public RegisteredWith(AuthenticatorAttachment attachment, string? browser, string? os)
  {
    if (!Enum.IsDefined(attachment))
    {
      throw new ArgumentOutOfRangeException(nameof(attachment), attachment, "AuthenticatorAttachment must be a defined value.");
    }

    Attachment = attachment;
    Browser = NormalizeFamily(browser);
    Os = NormalizeFamily(os);
  }

  /// <summary>Platform / cross-platform / unknown.</summary>
  public AuthenticatorAttachment Attachment { get; }

  /// <summary>Browser family ("Chrome", "Safari") or null when not captured.</summary>
  public string? Browser { get; }

  /// <summary>OS family ("Windows", "iOS") or null when not captured.</summary>
  public string? Os { get; }

  /// <summary>True when nothing was captured.</summary>
  public bool IsUnknown => Attachment == AuthenticatorAttachment.Unknown && Browser is null && Os is null;

  private static string? NormalizeFamily(string? value)
  {
    if (value is null)
    {
      return null;
    }

    string trimmed = value.Trim();
    if (trimmed.Length == 0)
    {
      return null;
    }

    return trimmed.Length > MaxFamilyLength ? trimmed[..MaxFamilyLength] : trimmed;
  }
}
