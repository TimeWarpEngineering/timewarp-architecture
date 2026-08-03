#region Purpose
// Overlay timewarp-software-style TIP_* environment variables onto TipOptions after config bind.
#endregion

#region Design
// tip.js reads TIP_ENABLED === "true" (strict string — "1" / "false" / missing must not enable).
// ASP.NET options bind TipOptions:* / TipOptions__* from appsettings and env; this helper also
// accepts the software var names so local run docs can show TIP_ENABLED / TIP_PAY_TO without
// forcing operators onto the options section prefix. Applied via PostConfigure so appsettings
// remain defaults and env wins when set.
// CDP keys (TIP_FACILITATOR_URL vs CDP_API_KEY_*) stay out of this type — HasFacilitatorAuth is
// set when CDP_API_KEY_ID and CDP_API_KEY_SECRET are both present; mainnet RequiresFacilitatorAuth
// is left to config (operators set Network + RequiresFacilitatorAuth for eip155:8453).
#endregion

namespace TimeWarp.Architecture.Features.Tip.Application;

using TimeWarp.X402;

/// <summary>Maps TIP_* / CDP_* process environment variables onto <see cref="TipOptions"/>.</summary>
public static class TipEnvironment
{
  public const string Enabled = "TIP_ENABLED";
  public const string PayTo = "TIP_PAY_TO";
  public const string Network = "TIP_NETWORK";
  public const string Price = "TIP_PRICE";
  public const string FacilitatorUrl = "TIP_FACILITATOR_URL";
  public const string Asset = "TIP_ASSET";
  public const string CdpApiKeyId = "CDP_API_KEY_ID";
  public const string CdpApiKeySecret = "CDP_API_KEY_SECRET";

  /// <summary>
  /// Applies non-null environment values. <see cref="Enabled"/> is true only when the env
  /// value is exactly <c>true</c> (case-sensitive, tip-jar hard lesson).
  /// </summary>
  public static void ApplyFromEnvironment(TipOptions options, Func<string, string?>? getEnv = null)
  {
    ArgumentNullException.ThrowIfNull(options);
    getEnv ??= static name => Environment.GetEnvironmentVariable(name);

    string? enabled = getEnv(Enabled);
    if (enabled is not null)
    {
      // Strict: only the string "true" enables — matches tip.js isTipEnabled.
      options.Enabled = enabled == "true";
    }

    string? payTo = getEnv(PayTo);
    if (!string.IsNullOrWhiteSpace(payTo))
    {
      options.PayTo = payTo.Trim();
    }

    string? network = getEnv(Network);
    if (!string.IsNullOrWhiteSpace(network))
    {
      options.Network = network.Trim();
    }

    string? price = getEnv(Price);
    if (!string.IsNullOrWhiteSpace(price))
    {
      options.Price = price.Trim();
    }

    string? facilitator = getEnv(FacilitatorUrl);
    if (!string.IsNullOrWhiteSpace(facilitator))
    {
      options.FacilitatorBase = facilitator.Trim();
    }

    string? asset = getEnv(Asset);
    if (!string.IsNullOrWhiteSpace(asset))
    {
      options.Asset = asset.Trim();
    }

    string? cdpId = getEnv(CdpApiKeyId)?.Trim();
    string? cdpSecret = getEnv(CdpApiKeySecret)?.Trim();
    bool hasCdp = !string.IsNullOrEmpty(cdpId) && !string.IsNullOrEmpty(cdpSecret);
    options.HasFacilitatorAuth = hasCdp;

    // When CDP keys present and no explicit facilitator URL, prefer CDP platform (tip.js parity).
    if (hasCdp && string.IsNullOrWhiteSpace(getEnv(FacilitatorUrl)))
    {
      options.FacilitatorBase = FacilitatorUrls.CdpPlatform;
    }

    // Mainnet Base requires authenticated facilitator (tip.js mainnet gate).
    if (string.Equals(options.Network, "eip155:8453", StringComparison.Ordinal))
    {
      options.RequiresFacilitatorAuth = true;
    }
  }
}
