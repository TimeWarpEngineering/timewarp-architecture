#region Purpose
// Tests for WebAuthnOrigin.IsAllowed: the empty-Origins fallback (https on the RP host, http only on
// a loopback RP host) and the exact allow-list when Origins is non-empty.
#endregion

// ReSharper disable InconsistentNaming
namespace WebAuthnOrigin_;

public class IsAllowed
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsAllowed>();

  private static WebAuthnRelyingParty Fallback(string rpId) => new(rpId, "Test RP", []);

  public static Task Fallback_accepts_https_localhost_with_port()
  {
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "https://localhost:63611").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Fallback_accepts_http_localhost_with_port()
  {
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "http://localhost:36371").ShouldBeTrue();
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "http://LocalHost:36371").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Fallback_accepts_http_ipv4_loopback_when_rp_id_matches()
  {
    WebAuthnOrigin.IsAllowed(Fallback("127.0.0.1"), "http://127.0.0.1:8080").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Fallback_accepts_http_ipv6_loopback_when_rp_id_matches()
  {
    WebAuthnOrigin.IsAllowed(Fallback("[::1]"), "http://[::1]:8080").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Fallback_rejects_http_loopback_when_rp_id_differs()
  {
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "http://127.0.0.1:8080").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Fallback_rejects_http_on_non_loopback_host()
  {
    WebAuthnOrigin.IsAllowed(Fallback("arch.timewarp.work"), "http://arch.timewarp.work").ShouldBeFalse();
    WebAuthnOrigin.IsAllowed(Fallback("arch.timewarp.work"), "https://arch.timewarp.work").ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Fallback_rejects_https_on_foreign_host()
  {
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "https://evil.example").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Fallback_rejects_non_web_scheme_on_loopback()
  {
    WebAuthnOrigin.IsAllowed(Fallback("localhost"), "ftp://localhost").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Explicit_list_rejects_unlisted_http_localhost()
  {
    WebAuthnRelyingParty rp = new("localhost", "Test RP", ["https://localhost:63611"]);

    WebAuthnOrigin.IsAllowed(rp, "https://localhost:63611").ShouldBeTrue();
    WebAuthnOrigin.IsAllowed(rp, "http://localhost:36371").ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Explicit_list_accepts_listed_http_localhost_exactly()
  {
    WebAuthnRelyingParty rp = new("localhost", "Test RP", ["http://localhost:36371"]);

    WebAuthnOrigin.IsAllowed(rp, "http://localhost:36371").ShouldBeTrue();
    WebAuthnOrigin.IsAllowed(rp, "http://localhost:36372").ShouldBeFalse();
    return Task.CompletedTask;
  }

}
