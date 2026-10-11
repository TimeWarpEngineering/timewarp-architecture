#region Purpose
// Applies X-Forwarded-Proto and X-Forwarded-Host on the YARP ingress only when the peer is a configured trusted proxy.
#endregion

#region Design
// "X-Forwarded": "Set" copies Request.Scheme and Request.Host downstream. The Caddy hop is plain
// http, so without this step Set replaces Caddy's https with http and link previews and mail
// permalinks leave as http. This middleware runs before MapReverseProxy, so the Set transform
// forwards the client-facing scheme and host. ForwardLimit is 1 (the closest hop only).
// KnownProxies and KnownIPNetworks come from the TrustedProxy section. An absent section trusts
// loopback only (127.0.0.1 and ::1): Caddy on this machine connects to the pinned http endpoint.
// An explicit section replaces that default, so a cluster can name its proxy CIDR without also
// trusting loopback. Both lists empty skips the middleware. The framework treats two empty
// allow-lists as trust-everyone; this ingress must not. An unparseable address or CIDR fails
// startup. X-Forwarded-For is not applied: Web.Server rate limiting reads Connection.RemoteIpAddress
// and does not consume that header. ASPNETCORE_FORWARDEDHEADERS_ENABLED is not used: it clears the
// allow-lists and does not honor X-Forwarded-Host.
#endregion

namespace TimeWarp.Architecture.Yarp.Server;

/// <summary>
/// Trust boundary for the proxy headers the ingress copies onto the request it forwards.
/// </summary>
public static class TrustedProxyForwardedHeaders
{
  /// <summary>Configuration section. An absent section trusts loopback only.</summary>
  public const string SectionName = "TrustedProxy";

  /// <summary>
  /// Honors <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c> from the peers named by
  /// <see cref="SectionName"/>. A peer outside that list cannot change scheme or host.
  /// </summary>
  /// <exception cref="InvalidOperationException">A configured address or CIDR does not parse.</exception>
  public static void UseTrustedProxyForwardedHeaders(this WebApplication webApplication)
  {
    IConfiguration section = webApplication.Configuration.GetSection(SectionName);
    IConfigurationSection proxiesSection = section.GetSection("KnownProxies");
    IConfigurationSection networksSection = section.GetSection("KnownNetworks");
    bool proxiesConfigured = proxiesSection.Exists();
    bool networksConfigured = networksSection.Exists();

    List<IPAddress> proxies = [];
    List<System.Net.IPNetwork> networks = [];

    if (!proxiesConfigured && !networksConfigured)
    {
      proxies.Add(IPAddress.Loopback);
      proxies.Add(IPAddress.IPv6Loopback);
    }
    else
    {
      if (proxiesConfigured)
      {
        string[]? configuredProxies = proxiesSection.Get<string[]>();
        if (configuredProxies is null)
        {
          throw new InvalidOperationException(
            "TrustedProxy:KnownProxies must be an array of IP addresses.");
        }

        foreach (string value in configuredProxies)
        {
          proxies.Add(ParseProxy(value));
        }
      }

      if (networksConfigured)
      {
        string[]? configuredNetworks = networksSection.Get<string[]>();
        if (configuredNetworks is null)
        {
          throw new InvalidOperationException(
            "TrustedProxy:KnownNetworks must be an array of CIDR networks.");
        }

        foreach (string value in configuredNetworks)
        {
          networks.Add(ParseNetwork(value));
        }
      }
    }

    // Empty allow-lists make ForwardedHeadersMiddleware trust every peer. Skip it instead.
    if (proxies.Count == 0 && networks.Count == 0)
    {
      return;
    }

    ForwardedHeadersOptions options = new()
    {
      ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
      ForwardLimit = 1,
    };
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    foreach (IPAddress proxy in proxies)
    {
      options.KnownProxies.Add(proxy);
    }

    foreach (System.Net.IPNetwork network in networks)
    {
      options.KnownIPNetworks.Add(network);
    }

    webApplication.UseForwardedHeaders(options);
  }

  private static IPAddress ParseProxy(string value)
  {
    if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value.Trim(), out IPAddress? address))
    {
      throw new InvalidOperationException(
        $"TrustedProxy:KnownProxies value '{value}' is not an IP address.");
    }

    return address;
  }

  private static System.Net.IPNetwork ParseNetwork(string value)
  {
    if (string.IsNullOrWhiteSpace(value) || !System.Net.IPNetwork.TryParse(value.Trim(), out System.Net.IPNetwork network))
    {
      throw new InvalidOperationException(
        $"TrustedProxy:KnownNetworks value '{value}' is not a CIDR network.");
    }

    return network;
  }
}
