#region Purpose
// Configurable app-level rate-limit windows for principal registration and payment-challenge paths.
#endregion

#region Design
// Section name "AbuseRateLimitOptions" (matches type name — AddFluentValidatedOptions binds by type name).
// Lives under platform/abuse (not a product Features.* slice): host abuse posture shared by identity
// registration and 402 challenge endpoints (task 104-015).
//
// EDGE VS APP (checklist / 104-023 later):
//   - Edge (Cloudflare WAF / rate limits) is the outer volumetric ring — DDoS, crude IP floods,
//     bot classes. Documented separately (task 104-023); not a substitute for app Identity/402 law.
//   - App (this options + ASP.NET RateLimiter middleware) protects origin from mass register and
//     unpaid 402 challenge floods that already passed edge or hit the origin directly (local,
//     private path, misconfigured edge). Cheap rejection (structured 429) before ceremony work or
//     PaymentGate evaluation.
//   - Partition is per remote IP (Connection.RemoteIpAddress). True client IP behind shared ingress
//     requires PROXY protocol / trusted forwarded headers (task 112 notes) — until then all clients
//     behind one hop share a partition. That is accepted for v1: still bounds origin melt; edge
//     handles multi-IP volumetric abuse.
//
// Defaults are teachable production-ish sliding windows (common auth/API practice), not load-test
// ceilings. Operators raise/lower via config; tests PostConfigure tight limits to prove 429.
// Enabled=false switches both policies to no-op limiters without removing middleware wiring.
#endregion

namespace TimeWarp.Architecture.Abuse;

/// <summary>App-level abuse rate limits for registration and payment-challenge endpoints.</summary>
public sealed class AbuseRateLimitOptions
{
  public const string SectionName = "AbuseRateLimitOptions";

  /// <summary>When false, named policies admit all traffic (no-op limiters). Middleware stays registered.</summary>
  public bool Enabled { get; set; } = true;

  /// <summary>Passkey + agent-key register options/complete paths.</summary>
  public SlidingWindowLimitOptions PrincipalRegistration { get; set; } = new()
  {
    // ~10 register ceremonies / minute / IP — mass sybil minting bound.
    PermitLimit = 10,
    WindowSeconds = 60,
    SegmentsPerWindow = 6,
  };

  /// <summary>Tip + metered capability paths that can emit unpaid 402 challenges.</summary>
  public SlidingWindowLimitOptions PaymentChallenge { get; set; } = new()
  {
    // 402 bodies are cheap; still bound flood so origin cannot be melted by unpaid challenge spam.
    PermitLimit = 30,
    WindowSeconds = 60,
    SegmentsPerWindow = 6,
  };
}

/// <summary>Sliding-window parameters for one named ASP.NET rate-limit policy.</summary>
public sealed class SlidingWindowLimitOptions
{
  /// <summary>Max permits inside <see cref="WindowSeconds"/> (sliding, segmented).</summary>
  public int PermitLimit { get; set; }

  /// <summary>Window length in whole seconds.</summary>
  public int WindowSeconds { get; set; }

  /// <summary>Sliding segments within the window (ASP.NET SlidingWindowRateLimiter).</summary>
  public int SegmentsPerWindow { get; set; } = 6;
}
