#region Purpose
// One lookup result: outcome, tenant id when found, and the status or issuer failure when not.
#endregion

#region Design
// Factories keep impossible combinations out of call sites. TenantId is empty unless Found.
// Domain is the normalized domain for every outcome except InvalidDomain, which keeps the
// trimmed input so the UI can quote it. StatusCode is set only for UnexpectedStatus.
#endregion

namespace TimeWarp.Identity;

/// <summary>Typed result of <see cref="EntraTenantResolver.ResolveAsync"/>.</summary>
public sealed record EntraTenantLookupResult
{
  private EntraTenantLookupResult(
    EntraTenantLookupOutcome outcome,
    Guid tenantId,
    string? domain,
    int? statusCode,
    EntraIssuerParseFailure issuerFailure)
  {
    Outcome = outcome;
    TenantId = tenantId;
    Domain = domain;
    StatusCode = statusCode;
    IssuerFailure = issuerFailure;
  }

  /// <summary>What the lookup decided.</summary>
  public EntraTenantLookupOutcome Outcome { get; }

  /// <summary>The tenant id when <see cref="Outcome"/> is <see cref="EntraTenantLookupOutcome.Found"/>.</summary>
  public Guid TenantId { get; }

  /// <summary>Normalized domain, or the trimmed input when the domain itself was invalid.</summary>
  public string? Domain { get; }

  /// <summary>HTTP status when <see cref="Outcome"/> is <see cref="EntraTenantLookupOutcome.UnexpectedStatus"/>.</summary>
  public int? StatusCode { get; }

  /// <summary>Issuer parse failure when <see cref="Outcome"/> is <see cref="EntraTenantLookupOutcome.UnexpectedIssuer"/>.</summary>
  public EntraIssuerParseFailure IssuerFailure { get; }

  /// <summary>A tenant id parsed from the discovery issuer.</summary>
  public static EntraTenantLookupResult Found(Guid tenantId, string domain) =>
    new(EntraTenantLookupOutcome.Found, tenantId, domain, null, EntraIssuerParseFailure.None);

  /// <summary>Microsoft does not publish a tenant for this domain.</summary>
  public static EntraTenantLookupResult NotFound(string domain) =>
    new(EntraTenantLookupOutcome.NotFound, Guid.Empty, domain, null, EntraIssuerParseFailure.None);

  /// <summary>The domain was rejected before HTTP.</summary>
  public static EntraTenantLookupResult InvalidDomain(string? domain) =>
    new(EntraTenantLookupOutcome.InvalidDomain, Guid.Empty, domain, null, EntraIssuerParseFailure.None);

  /// <summary>The request did not complete.</summary>
  public static EntraTenantLookupResult NetworkFailure(string domain) =>
    new(EntraTenantLookupOutcome.NetworkFailure, Guid.Empty, domain, null, EntraIssuerParseFailure.None);

  /// <summary>The request exceeded the lookup timeout.</summary>
  public static EntraTenantLookupResult TimedOut(string domain) =>
    new(EntraTenantLookupOutcome.Timeout, Guid.Empty, domain, null, EntraIssuerParseFailure.None);

  /// <summary>A non-success status that is not an unknown tenant.</summary>
  public static EntraTenantLookupResult UnexpectedStatus(string domain, int statusCode) =>
    new(EntraTenantLookupOutcome.UnexpectedStatus, Guid.Empty, domain, statusCode, EntraIssuerParseFailure.None);

  /// <summary>The issuer was missing or not a v2 tenant issuer.</summary>
  public static EntraTenantLookupResult UnexpectedIssuer(string domain, EntraIssuerParseFailure failure) =>
    new(EntraTenantLookupOutcome.UnexpectedIssuer, Guid.Empty, domain, null, failure);
}
