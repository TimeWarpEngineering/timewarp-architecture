#region Purpose
// Normalize, parse, and JSON-encode the site-settings accepted Entra tenant id list.
#endregion

#region Design
// One list shape for the aggregate, the settings validator, the admin textarea, and the jsonb
// column. Empty Guid is refused. Duplicates collapse. Order is sorted so snapshots compare.
// Consumers is the Microsoft Account personal tenant. Policy does not special-case it: Any
// accepts it when a token arrives, and Restricted accepts it only when an administrator lists it.
// Tokens for that tenant arrive only from a common or consumers authority.
#endregion

namespace TimeWarp.Identity;

/// <summary>Accepted Entra tenant ids for <see cref="SiteSettings"/>.</summary>
public static class EntraTenantIds
{
  /// <summary>Upper bound stored on one site-settings row.</summary>
  public const int MaxAcceptedCount = 32;

  /// <summary>
  /// Microsoft Account personal-tenant id (<c>consumers</c>).
  /// </summary>
  public static readonly Guid Consumers = Guid.Parse("9188040d-6c67-4c5b-b112-36a304b66dad");

  /// <summary>
  /// Collapses duplicates, rejects <see cref="Guid.Empty"/>, and sorts.
  /// A null sequence is an empty list.
  /// </summary>
  public static bool TryNormalize(
    IEnumerable<Guid>? tenantIds,
    out IReadOnlyList<Guid> normalized,
    out string? error)
  {
    normalized = [];
    error = null;
    if (tenantIds is null)
    {
      return true;
    }

    List<Guid> distinct = [];
    foreach (Guid tenantId in tenantIds)
    {
      if (tenantId == Guid.Empty)
      {
        error = "Accepted tenant ids must be non-empty GUIDs.";
        normalized = [];
        return false;
      }

      if (!distinct.Contains(tenantId))
      {
        distinct.Add(tenantId);
      }
    }

    if (distinct.Count > MaxAcceptedCount)
    {
      error = $"At most {MaxAcceptedCount} accepted tenant ids.";
      normalized = [];
      return false;
    }

    distinct.Sort();
    normalized = distinct;
    return true;
  }

  /// <summary>
  /// Parses a textarea or comma-separated list of tenant ids.
  /// Blank text is an empty list.
  /// </summary>
  public static bool TryParse(string? text, out IReadOnlyList<Guid> tenantIds, out string? error)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      tenantIds = [];
      error = null;
      return true;
    }

    List<Guid> parsed = [];
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

      parsed.Add(tenantId);
    }

    return TryNormalize(parsed, out tenantIds, out error);
  }

  /// <summary>JSON array of tenant ids for the jsonb column.</summary>
  public static string ToJson(IReadOnlyList<Guid>? tenantIds)
  {
    IReadOnlyList<Guid> ids = tenantIds ?? [];
    return JsonSerializer.Serialize(ids);
  }

  /// <summary>Reads a jsonb tenant-id array. Unreadable JSON becomes an empty list.</summary>
  public static IReadOnlyList<Guid> FromJson(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return [];
    }

    try
    {
      List<Guid>? parsed = JsonSerializer.Deserialize<List<Guid>>(json);
      if (parsed is null || !TryNormalize(parsed, out IReadOnlyList<Guid> normalized, out _))
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
}
