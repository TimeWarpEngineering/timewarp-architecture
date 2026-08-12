#region Purpose
// Rewrites bare /api to the voluntary tip jar so commerce scanners probing conventional API roots see x402.
#endregion

#region Design
// timewarp-software tip.js pattern: /api and /api/ are discovery aliases that normalize to the
// canonical resource /api/tip. Free routes never enter this rewrite. Challenge Resource stays
// /api/tip (TipOptions.Resource) — only the request path is rewritten so FastEndpoints hits
// SubmitTip. Exact path only: /api/health, /api/identity/…, /api/demo/… are unchanged.
// Runs before UseRouting (same slot as markdown negotiation) so endpoint matching sees /api/tip.
// Ingress (YARP/AppHost) must also pin exact /api → Web.Server or bare /api falls to api-server's
// /api/{**catch-all} and never reaches this rewrite (task 104-020).
#endregion

namespace TimeWarp.Architecture.Features.Tip;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Path rewrite: bare <c>/api</c> → <c>/api/tip</c> for x402 commerce-scanner discovery.
/// </summary>
public static class TipDiscoveryAlias
{
  /// <summary>Canonical tip action path advertised in PAYMENT-REQUIRED challenges.</summary>
  public static readonly PathString CanonicalTipPath = new("/api/tip");

  /// <summary>Whether <paramref name="path"/> is the bare API-root discovery alias.</summary>
  public static bool IsAliasPath(PathString path)
  {
    string value = path.Value ?? string.Empty;
    return value.Equals("/api", StringComparison.OrdinalIgnoreCase)
      || value.Equals("/api/", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Rewrites bare <c>/api</c> to <see cref="CanonicalTipPath"/> before endpoint routing.
  /// </summary>
  public static IApplicationBuilder UseTipDiscoveryAlias(this IApplicationBuilder app)
  {
    ArgumentNullException.ThrowIfNull(app);
    return app.Use(static async (context, next) =>
    {
      if (IsAliasPath(context.Request.Path))
      {
        context.Request.Path = CanonicalTipPath;
      }

      await next().ConfigureAwait(false);
    });
  }
}
