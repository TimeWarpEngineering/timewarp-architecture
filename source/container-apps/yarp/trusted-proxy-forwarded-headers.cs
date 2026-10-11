#region Purpose
// Applies X-Forwarded-Proto and X-Forwarded-Host on the YARP ingress only when the peer is a configured trusted proxy.
#endregion

#region Design
// "X-Forwarded": "Set" copies Request.Scheme and Request.Host downstream. The Caddy hop is plain
// http, so without this step Set replaces Caddy's https with http and link previews and mail
// permalinks leave as http. This middleware runs before MapReverseProxy, so the Set transform
// forwards the client-facing scheme and host. ForwardLimit is 1 (the closest hop only).
// KnownProxies and KnownIPNetworks come from the TrustedProxy section. When neither key is declared
// the ingress trusts loopback only (127.0.0.1 and ::1): Caddy on this machine connects to the pinned
// http endpoint. Declaring either key replaces that default, so a cluster can name its proxy CIDR
// without also trusting loopback. A declared key counts even when its array is empty: JSON stores []
// as a null-valued key that Exists() reports as absent, so ReadList checks the key itself. Both
// lists empty skips the middleware. The framework treats two empty
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
  /// <summary>Configuration section. When neither list is declared, loopback only is trusted.</summary>
  public const string SectionName = "TrustedProxy";

  /// <summary>
  /// Honors <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c> from the peers named by
  /// <see cref="SectionName"/>. A peer outside that list cannot change scheme or host.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// A configured address or CIDR does not parse, or a list is a single value instead of an array.
  /// </exception>
  public static void UseTrustedProxyForwardedHeaders(this WebApplication webApplication)
  {
    IConfiguration section = webApplication.Configuration.GetSection(SectionName);
    string[]? configuredProxies = ReadList(section, "KnownProxies");
    string[]? configuredNetworks = ReadList(section, "KnownNetworks");

    List<IPAddress> proxies = [];
    List<System.Net.IPNetwork> networks = [];

    if (configuredProxies is null && configuredNetworks is null)
    {
      proxies.Add(IPAddress.Loopback);
      proxies.Add(IPAddress.IPv6Loopback);
    }
    else
    {
      foreach (string value in configuredProxies ?? [])
      {
        proxies.Add(ParseProxy(value));
      }

      foreach (string value in configuredNetworks ?? [])
      {
        networks.Add(ParseNetwork(value));
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

  // A declared key is configured even when empty: JSON stores [] as a key with a null value, which
  // IConfigurationSection.Exists() reports as absent. Returns null only when the key is not declared.
  private static string[]? ReadList(IConfiguration section, string key)
  {
    IConfigurationSection? list = section.GetChildren()
      .FirstOrDefault(child => string.Equals(child.Key, key, StringComparison.OrdinalIgnoreCase));
    if (list is null)
    {
      return null;
    }

    if (!string.IsNullOrEmpty(list.Value))
    {
      throw new InvalidOperationException($"TrustedProxy:{key} must be an array, not a single value.");
    }

    return [.. list.GetChildren().Select(child => child.Value ?? string.Empty)];
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
