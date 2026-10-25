#region Purpose
// Normalize, parse, and JSON-encode the site-settings accepted Entra tenant list.
#endregion

#region Design
// One list shape for the aggregate, the settings validator, and the jsonb column: entries of
// tenant id plus an optional domain. Empty Guid is refused. Duplicates collapse by tenant id.
// Two different non-null domains for the same id are refused. A null domain loses to a
// non-null one. Domains are trimmed and lowercased. Order is sorted by tenant id so snapshots
// compare. Whitespace domain becomes null. A domain must be at most 253 characters, at least
// two DNS labels, each label 1-63 characters of [a-z0-9-] with no leading or trailing hyphen.
// Punycode (xn--) is a normal label. Colon, slash, at-sign, and space are refused.
// JSON is a camelCase array of { tenantId, domain } via JsonSerializerDefaults.Web, including
// a null domain. There is no reader for the old GUID array. FromJson of that shape, or of
// unreadable JSON, is an empty list. The column is migrated to entries before this reader
// runs. Migration ConvertEntraAcceptedTenants rewrites a stored GUID array into null-domain
// entries. That rewrite is pinned by ef-entra-accepted-tenants-migration-tests.cs. Consumers
// is the Microsoft Account personal tenant. Policy does not
// special-case it. TryParse still returns GUIDs for a hand-entered list; each id is stored
// with a null domain.
#endregion

namespace TimeWarp.Identity;

/// <summary>Accepted Entra tenants for <see cref="SiteSettings"/>.</summary>
public static class EntraTenantIds
{
  /// <summary>Upper bound stored on one site-settings row.</summary>
  public const int MaxAcceptedCount = 32;

  /// <summary>Longest accepted domain, in characters, after trim.</summary>
  public const int MaxDomainLength = 253;

  /// <summary>
  /// Microsoft Account personal-tenant id (<c>consumers</c>).
  /// </summary>
  public static readonly Guid Consumers = Guid.Parse("9188040d-6c67-4c5b-b112-36a304b66dad");

  private static readonly JsonSerializerOptions EntryJsonOptions = new(JsonSerializerDefaults.Web);

  /// <summary>
  /// Collapses duplicates by tenant id, rejects <see cref="Guid.Empty"/>, normalizes domains, and sorts.
  /// A null sequence is an empty list.
  /// </summary>
  public static bool TryNormalize(
    IEnumerable<EntraAcceptedTenant>? tenants,
    out IReadOnlyList<EntraAcceptedTenant> normalized,
    out string? error)
  {
    normalized = [];
    error = null;
    if (tenants is null)
    {
      return true;
    }

    Dictionary<Guid, EntraAcceptedTenant> byId = [];
    foreach (EntraAcceptedTenant? tenant in tenants)
    {
      if (tenant is null || tenant.TenantId == Guid.Empty)
      {
        error = "Accepted tenant ids must be non-empty GUIDs.";
        normalized = [];
        return false;
      }

      if (!TryNormalizeDomain(tenant.Domain, out string? domain))
      {
        error = $"'{tenant.Domain?.Trim()}' is not a domain name.";
        normalized = [];
        return false;
      }

      if (byId.TryGetValue(tenant.TenantId, out EntraAcceptedTenant? existing))
      {
        if (existing.Domain is not null
          && domain is not null
          && !string.Equals(existing.Domain, domain, StringComparison.Ordinal))
        {
          error = $"Tenant {tenant.TenantId:D} is listed with more than one domain.";
          normalized = [];
          return false;
        }

        if (existing.Domain is null && domain is not null)
        {
          byId[tenant.TenantId] = new EntraAcceptedTenant(tenant.TenantId, domain);
        }
      }
      else
      {
        byId[tenant.TenantId] = new EntraAcceptedTenant(tenant.TenantId, domain);
      }
    }

    if (byId.Count > MaxAcceptedCount)
    {
      error = $"At most {MaxAcceptedCount} accepted tenant ids.";
      normalized = [];
      return false;
    }

    List<EntraAcceptedTenant> sorted = [.. byId.Values];
    sorted.Sort(static (left, right) => left.TenantId.CompareTo(right.TenantId));
    normalized = sorted;
    return true;
  }

  /// <summary>
  /// Trims and lowercases a domain. Whitespace is a null domain and succeeds.
  /// A non-null result is a domain name.
  /// </summary>
  public static bool TryNormalizeDomain(string? domain, out string? normalized)
  {
    normalized = null;
    if (string.IsNullOrWhiteSpace(domain))
    {
      return true;
    }

    string trimmed = domain.Trim().ToLowerInvariant();
    if (trimmed.Length > MaxDomainLength || !IsDomainName(trimmed))
    {
      return false;
    }

    normalized = trimmed;
    return true;
  }

  /// <summary>
  /// Parses a textarea or comma-separated list of tenant ids.
  /// Blank text is an empty list. Domains are not parsed here.
  /// </summary>
  public static bool TryParse(string? text, out IReadOnlyList<Guid> tenantIds, out string? error)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      tenantIds = [];
      error = null;
      return true;
    }

    List<EntraAcceptedTenant> parsed = [];
    string[] tokens = text.Split(
      [',', ';', '\n', '\r', '\t', ' '],
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (string token in tokens)
    {
      if (!Guid.TryParse(token, out Guid tenantId))
      {
        tenantIds = [];
        error = $"'{token}' is not a tenant id.";
        return false;
      }

      parsed.Add(EntraAcceptedTenant.IdOnly(tenantId));
    }

    if (!TryNormalize(parsed, out IReadOnlyList<EntraAcceptedTenant> normalized, out error))
    {
      tenantIds = [];
      return false;
    }

    tenantIds = normalized.Select(static entry => entry.TenantId).ToArray();
    return true;
  }

  /// <summary>JSON array of accepted tenants for the jsonb column.</summary>
  public static string ToJson(IReadOnlyList<EntraAcceptedTenant>? tenants)
  {
    IReadOnlyList<EntraAcceptedTenant> entries = tenants ?? [];
    return JsonSerializer.Serialize(entries, EntryJsonOptions);
  }

  /// <summary>
  /// Reads a jsonb accepted-tenant array. The old GUID-array shape and unreadable JSON
  /// become an empty list.
  /// </summary>
  public static IReadOnlyList<EntraAcceptedTenant> FromJson(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return [];
    }

    try
    {
      List<EntraAcceptedTenant>? parsed = JsonSerializer.Deserialize<List<EntraAcceptedTenant>>(json, EntryJsonOptions);
      if (parsed is null || !TryNormalize(parsed, out IReadOnlyList<EntraAcceptedTenant> normalized, out _))
      {
        return [];
      }

      return normalized;
    }
    catch (JsonException)
    {
      return [];
    }
  }

  private static bool IsDomainName(string domain)
  {
    foreach (char character in domain)
    {
      if (character is ':' or '/' or '@' or ' ')
      {
        return false;
      }
    }

    string[] labels = domain.Split('.');
    if (labels.Length < 2)
    {
      return false;
    }

    foreach (string label in labels)
    {
      if (label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-')
      {
        return false;
      }

      foreach (char character in label)
      {
        bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
        if (!allowed)
        {
          return false;
        }
      }
    }

    return true;
  }
}
