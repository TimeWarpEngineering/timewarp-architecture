#region Purpose
// Resolves the RegisteredWith context for a new credential from what the registering request exposes:
// WebAuthn authenticator attachment / transports and the request User-Agent (parsed into browser and
// OS FAMILY names — never stored raw).
#endregion

#region Design
// Task 248-001. Attachment precedence: PublicKeyCredential.authenticatorAttachment ("platform" /
// "cross-platform") is authoritative when the browser reports it; otherwise the attestation
// response's transports decide — "internal" means a platform authenticator (a synced passkey may
// report ["internal","hybrid"], still platform on THIS device), any roaming transport (usb / nfc /
// ble / hybrid / smart-card / cable) without internal means cross-platform; nothing → Unknown.
// Both inputs are client-asserted hints, not security signals — they only ever feed a display row.
// User-Agent parsing is a deliberately small, ordered substring classifier (no package): the
// product needs "Chrome on Windows", not version fidelity. Edge/Opera/Samsung are checked BEFORE
// Chrome because they embed "Chrome/"; Chrome before Safari because it embeds "Safari/"; iOS
// browsers (CriOS / FxiOS / EdgiOS) are recognized by their iOS tokens. Unknown families return
// null rather than a guess. Input is capped (MaxUserAgentLength) before any scan so a hostile
// header cannot make this quadratic. Agent keys go through the same resolver with no WebAuthn
// hints, so an agent CLI that sends a User-Agent gets Browser/Os and Unknown attachment; one that
// sends none gets RegisteredWith.Unknown — the "else null" the task asked for.
// Lives in the identity feature (application layer) because both passkey and agent-key handlers
// call it; the User-Agent itself is read through IRequestUserAgentAccessor (platform port), keeping
// web-application free of ASP.NET Core exactly like IRequestHostAccessor.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public static class RegistrationContext
{
  /// <summary>Longest User-Agent the classifier will scan; longer values are truncated first.</summary>
  public const int MaxUserAgentLength = 1024;

  private static readonly string[] RoamingTransports = ["usb", "nfc", "ble", "hybrid", "smart-card", "cable"];

  /// <summary>Builds the stored context from WebAuthn hints plus the request User-Agent.</summary>
  public static RegisteredWith Resolve(string? authenticatorAttachment, IReadOnlyList<string>? transports, string? userAgent)
  {
    AuthenticatorAttachment attachment = ResolveAttachment(authenticatorAttachment, transports);
    (string? browser, string? os) = ParseUserAgent(userAgent);
    return new RegisteredWith(attachment, browser, os);
  }

  /// <summary>Agent-key form: no WebAuthn hints; only the request User-Agent (may be null).</summary>
  public static RegisteredWith ResolveForAgentKey(string? userAgent) => Resolve(authenticatorAttachment: null, transports: null, userAgent);

  public static AuthenticatorAttachment ResolveAttachment(string? authenticatorAttachment, IReadOnlyList<string>? transports)
  {
    if (string.Equals(authenticatorAttachment?.Trim(), "platform", StringComparison.OrdinalIgnoreCase))
    {
      return AuthenticatorAttachment.Platform;
    }

    if (string.Equals(authenticatorAttachment?.Trim(), "cross-platform", StringComparison.OrdinalIgnoreCase))
    {
      return AuthenticatorAttachment.CrossPlatform;
    }

    if (transports is null || transports.Count == 0)
    {
      return AuthenticatorAttachment.Unknown;
    }

    if (transports.Any(transport => string.Equals(transport?.Trim(), "internal", StringComparison.OrdinalIgnoreCase)))
    {
      return AuthenticatorAttachment.Platform;
    }

    if (transports.Any(transport => RoamingTransports.Contains(transport?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)))
    {
      return AuthenticatorAttachment.CrossPlatform;
    }

    return AuthenticatorAttachment.Unknown;
  }

  /// <summary>Browser and OS family from a User-Agent; either is null when unrecognized.</summary>
  public static (string? Browser, string? Os) ParseUserAgent(string? userAgent)
  {
    if (string.IsNullOrWhiteSpace(userAgent))
    {
      return (null, null);
    }

    string ua = userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
    return (ParseBrowser(ua), ParseOs(ua));
  }

  private static string? ParseBrowser(string ua)
  {
    if (Has(ua, "Edg/") || Has(ua, "EdgA/") || Has(ua, "EdgiOS/") || Has(ua, "Edge/"))
    {
      return "Edge";
    }

    if (Has(ua, "OPR/") || Has(ua, "Opera"))
    {
      return "Opera";
    }

    if (Has(ua, "SamsungBrowser/"))
    {
      return "Samsung Internet";
    }

    if (Has(ua, "Firefox/") || Has(ua, "FxiOS/"))
    {
      return "Firefox";
    }

    if (Has(ua, "Chrome/") || Has(ua, "CriOS/") || Has(ua, "Chromium/"))
    {
      return "Chrome";
    }

    if (Has(ua, "Safari/") && (Has(ua, "Version/") || Has(ua, "iPhone") || Has(ua, "iPad") || Has(ua, "Macintosh")))
    {
      return "Safari";
    }

    return null;
  }

  private static string? ParseOs(string ua)
  {
    if (Has(ua, "Windows"))
    {
      return "Windows";
    }

    if (Has(ua, "iPhone") || Has(ua, "iPad") || Has(ua, "iPod"))
    {
      return "iOS";
    }

    if (Has(ua, "Android"))
    {
      return "Android";
    }

    if (Has(ua, "CrOS"))
    {
      return "ChromeOS";
    }

    if (Has(ua, "Macintosh") || Has(ua, "Mac OS X"))
    {
      return "macOS";
    }

    if (Has(ua, "Linux") || Has(ua, "X11"))
    {
      return "Linux";
    }

    return null;
  }

  private static bool Has(string ua, string token) => ua.Contains(token, StringComparison.Ordinal);
}
