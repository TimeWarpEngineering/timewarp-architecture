#region Purpose
// Resolve a customer's primary domain to an Entra tenant id via OpenID discovery.
#endregion

#region Design
// GET https://login.microsoftonline.com/{domain}/.well-known/openid-configuration.
// The issuer field is https://login.microsoftonline.com/{tenant-id}/v2.0. EntraIssuer reads
// that GUID. A successful lookup proves the domain belongs to a real Entra tenant.
// Microsoft's unknown-tenant response is usually HTTP 400 with error invalid_tenant
// (AADSTS90002), not 404. Both are NotFound. Any other 400 is UnexpectedStatus so a generic
// client error stays distinct. Other non-success statuses are UnexpectedStatus.
// A 200 whose issuer is missing, not JSON, or not a v2 tenant issuer is UnexpectedIssuer.
// Invalid domains never leave the process and are not cached.
// Found and NotFound are cached in memory for CacheDuration, keyed by the normalized domain.
// Network failure, timeout, unexpected status, and unexpected issuer are logged at Warning
// and are not cached. A cancelled caller token is rethrown. The named HttpClient's Timeout
// is Infinite so this type's LookupTimeout is the only deadline. The web server hosts the
// query; this type owns the Microsoft call. Register wires the named client, the memory
// cache, and the singleton. The five-argument constructor is for tests that need a short
// timeout; DI uses the three-argument constructor.
#endregion

namespace TimeWarp.Identity;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>Domain to Entra tenant id, through the OpenID discovery issuer.</summary>
public sealed class EntraTenantResolver
{
  /// <summary>Named <see cref="HttpClient"/> whose timeout is infinite.</summary>
  public const string HttpClientName = "entra-tenant-discovery";

  /// <summary>Default per-lookup deadline.</summary>
  public static readonly TimeSpan DefaultLookupTimeout = TimeSpan.FromSeconds(10);

  /// <summary>How long a found or not-found result stays in memory.</summary>
  public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(10);

  private const int UnknownTenantErrorCode = 90002;

  private static readonly Action<ILogger, string, Exception?> LogNetworkFailure =
    LoggerMessage.Define<string>(
      LogLevel.Warning,
      new EventId(1, nameof(LogNetworkFailure)),
      "Entra tenant lookup for {Domain} failed because the network request did not complete.");

  private static readonly Action<ILogger, string, Exception?> LogTimedOut =
    LoggerMessage.Define<string>(
      LogLevel.Warning,
      new EventId(2, nameof(LogTimedOut)),
      "Entra tenant lookup for {Domain} timed out.");

  private static readonly Action<ILogger, string, int, Exception?> LogUnexpectedStatus =
    LoggerMessage.Define<string, int>(
      LogLevel.Warning,
      new EventId(3, nameof(LogUnexpectedStatus)),
      "Entra tenant lookup for {Domain} returned unexpected HTTP status {StatusCode}.");

  private static readonly Action<ILogger, string, EntraIssuerParseFailure, Exception?> LogUnexpectedIssuer =
    LoggerMessage.Define<string, EntraIssuerParseFailure>(
      LogLevel.Warning,
      new EventId(4, nameof(LogUnexpectedIssuer)),
      "Entra tenant lookup for {Domain} returned an unexpected issuer ({Failure}).");

  private readonly IHttpClientFactory HttpClientFactory;
  private readonly IMemoryCache Cache;
  private readonly ILogger<EntraTenantResolver> Logger;
  private readonly TimeSpan LookupTimeout;
  private readonly TimeSpan CacheDuration;

  /// <summary>DI constructor. Uses <see cref="DefaultLookupTimeout"/> and <see cref="DefaultCacheDuration"/>.</summary>
  public EntraTenantResolver(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<EntraTenantResolver> logger)
    : this(httpClientFactory, cache, logger, DefaultLookupTimeout, DefaultCacheDuration)
  {
  }

  /// <summary>Test constructor. <paramref name="lookupTimeout"/> is the only HTTP deadline.</summary>
  public EntraTenantResolver(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<EntraTenantResolver> logger,
    TimeSpan lookupTimeout,
    TimeSpan cacheDuration)
  {
    ArgumentNullException.ThrowIfNull(httpClientFactory);
    ArgumentNullException.ThrowIfNull(cache);
    ArgumentNullException.ThrowIfNull(logger);
    HttpClientFactory = httpClientFactory;
    Cache = cache;
    Logger = logger;
    LookupTimeout = lookupTimeout;
    CacheDuration = cacheDuration;
  }

  /// <summary>Adds the named client, the memory cache, and this singleton.</summary>
  public static void Register(IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);
    services.AddMemoryCache();
    services.AddHttpClient(
      HttpClientName,
      static client => client.Timeout = Timeout.InfiniteTimeSpan);
    services.AddSingleton<EntraTenantResolver>();
  }

  /// <summary>
  /// Resolves <paramref name="domain"/>. A repeat of a found or not-found domain is served from cache.
  /// </summary>
  public async Task<EntraTenantLookupResult> ResolveAsync(string? domain, CancellationToken cancellationToken)
  {
    if (!EntraTenantIds.TryNormalizeDomain(domain, out string? normalized) || normalized is null)
    {
      return EntraTenantLookupResult.InvalidDomain(domain?.Trim());
    }

    if (Cache.TryGetValue(CacheKey(normalized), out EntraTenantLookupResult? cached) && cached is not null)
    {
      return cached;
    }

    EntraTenantLookupResult result = await LookupAsync(normalized, cancellationToken).ConfigureAwait(false);
    if (result.Outcome is EntraTenantLookupOutcome.Found or EntraTenantLookupOutcome.NotFound)
    {
      Cache.Set(CacheKey(normalized), result, CacheDuration);
    }

    return result;
  }

  private async Task<EntraTenantLookupResult> LookupAsync(string domain, CancellationToken cancellationToken)
  {
    HttpClient client = HttpClientFactory.CreateClient(HttpClientName);
    client.Timeout = Timeout.InfiniteTimeSpan;
    Uri url = new($"https://login.microsoftonline.com/{domain}/.well-known/openid-configuration");
    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutSource.CancelAfter(LookupTimeout);
    try
    {
      using HttpResponseMessage response = await client
        .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
        .ConfigureAwait(false);
      string body = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
      int status = (int)response.StatusCode;
      if (response.IsSuccessStatusCode)
      {
        return ParseDiscovery(domain, body);
      }

      if (status == 404 || (status == 400 && IsUnknownTenant(body)))
      {
        return EntraTenantLookupResult.NotFound(domain);
      }

      LogUnexpectedStatus(Logger, domain, status, null);
      return EntraTenantLookupResult.UnexpectedStatus(domain, status);
    }
    catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
    {
      LogTimedOut(Logger, domain, exception);
      return EntraTenantLookupResult.TimedOut(domain);
    }
    catch (HttpRequestException exception)
    {
      LogNetworkFailure(Logger, domain, exception);
      return EntraTenantLookupResult.NetworkFailure(domain);
    }
  }

  private EntraTenantLookupResult ParseDiscovery(string domain, string body)
  {
    try
    {
      using var document = JsonDocument.Parse(body);
      if (!document.RootElement.TryGetProperty("issuer", out JsonElement issuerElement)
        || issuerElement.ValueKind != JsonValueKind.String)
      {
        LogUnexpectedIssuer(Logger, domain, EntraIssuerParseFailure.Missing, null);
        return EntraTenantLookupResult.UnexpectedIssuer(domain, EntraIssuerParseFailure.Missing);
      }

      if (!EntraIssuer.TryParse(issuerElement.GetString(), out Guid tenantId, out EntraIssuerParseFailure failure))
      {
        LogUnexpectedIssuer(Logger, domain, failure, null);
        return EntraTenantLookupResult.UnexpectedIssuer(domain, failure);
      }

      return EntraTenantLookupResult.Found(tenantId, domain);
    }
    catch (JsonException exception)
    {
      LogUnexpectedIssuer(Logger, domain, EntraIssuerParseFailure.Malformed, exception);
      return EntraTenantLookupResult.UnexpectedIssuer(domain, EntraIssuerParseFailure.Malformed);
    }
  }

  private static bool IsUnknownTenant(string body)
  {
    if (body.Contains("AADSTS90002", StringComparison.Ordinal))
    {
      return true;
    }

    try
    {
      using var document = JsonDocument.Parse(body);
      if (document.RootElement.TryGetProperty("error", out JsonElement error)
        && error.ValueKind == JsonValueKind.String
        && string.Equals(error.GetString(), "invalid_tenant", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      if (document.RootElement.TryGetProperty("error_codes", out JsonElement codes)
        && codes.ValueKind == JsonValueKind.Array)
      {
        foreach (JsonElement code in codes.EnumerateArray())
        {
          if (code.ValueKind == JsonValueKind.Number
            && code.TryGetInt32(out int value)
            && value == UnknownTenantErrorCode)
          {
            return true;
          }
        }
      }
    }
    catch (JsonException)
    {
      return false;
    }

    return false;
  }

  private static string CacheKey(string domain) => "entra-tenant:" + domain;
}
