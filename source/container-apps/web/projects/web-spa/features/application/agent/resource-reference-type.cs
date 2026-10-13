#region Purpose
// One @ resource type: its prefix, label, id type, policy, and how to search its list query.
#endregion

#region Design
// Task 345. Instances come only from GeneratedResourceReferenceTypes (ResourceReferenceRegistryGenerator
// over [ReferenceableResource] contracts); this type is the generator's host marker. Create wraps
// the contract's query in one search call: the API service sends it as the signed-in user, so the
// server's [EndpointAuthorize] decides what comes back. The search term is trimmed and blank is
// null (list everything the query allows). At most ResultCap items are kept and HasMore says the
// list was cut, so the picker asks for a narrower term instead of paging through a long list. A
// blank label falls back to the id. A problem answer becomes a failed search with its title; so
// does a transport (HttpRequestException) or JSON failure (JsonException, or the
// InvalidOperationException a literal null body raises), so a keystroke never throws through the
// action pipeline and the picker's searching flag always clears. Null items read as empty.
// Store state never holds this type (it carries delegates); it holds ResourceReferenceTypeRow.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A resource type the Ask composer can reference as <c>@prefix:id</c>.</summary>
public sealed class ResourceReferenceType
{
  public const int ResultCap = 20;

  private readonly Func<IApiService, string?, CancellationToken, Task<ResourceReferenceSearch>> Search;

  private ResourceReferenceType
  (
    string prefix,
    string displayName,
    string idTypeName,
    string? policy,
    Func<IApiService, string?, CancellationToken, Task<ResourceReferenceSearch>> search
  )
  {
    Prefix = prefix;
    DisplayName = displayName;
    IdTypeName = idTypeName;
    Policy = policy;
    Search = search;
  }

  /// <summary>Token prefix, e.g. <c>feedback</c>.</summary>
  public string Prefix { get; }

  /// <summary>Plural name for the type list.</summary>
  public string DisplayName { get; }

  /// <summary>CLR name of the id type ([TypedId] struct or Guid) the token's id parses as.</summary>
  public string IdTypeName { get; }

  /// <summary>The list query's [EndpointAuthorize] policy; null means any signed-in user.</summary>
  public string? Policy { get; }

  public ResourceReferenceTypeRow ToRow() => new(Prefix, DisplayName, IdTypeName);

  /// <summary>Runs the list query as the signed-in user.</summary>
  public Task<ResourceReferenceSearch> SearchAsync(IApiService apiService, string? term, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(apiService);
    string? normalized = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
    return Search(apiService, normalized, cancellationToken);
  }

  /// <summary>Called by generated code for each [ReferenceableResource] contract.</summary>
  public static ResourceReferenceType Create<TQuery, TResponse, TItem>
  (
    string prefix,
    string displayName,
    string idTypeName,
    string? policy,
    Func<string?, TQuery> query,
    Func<TResponse, IEnumerable<TItem>> items,
    Func<TItem, string> id,
    Func<TItem, string?> label
  )
    where TQuery : IApiRequest
    where TResponse : class
  {
    ArgumentNullException.ThrowIfNull(query);
    ArgumentNullException.ThrowIfNull(items);
    ArgumentNullException.ThrowIfNull(id);
    ArgumentNullException.ThrowIfNull(label);
    return new ResourceReferenceType
    (
      prefix,
      displayName,
      idTypeName,
      policy,
      async (apiService, term, cancellationToken) =>
      {
        OneOf<TResponse, FileResponse, SharedProblemDetails> result;
        try
        {
          result = await apiService.GetResponse<TResponse>(query(term), cancellationToken);
        }
        catch (HttpRequestException)
        {
          return ResourceReferenceSearch.Failed("The list could not be reached.");
        }
        catch (System.Text.Json.JsonException)
        {
          return ResourceReferenceSearch.Failed("The list answer could not be read.");
        }
        catch (InvalidOperationException)
        {
          return ResourceReferenceSearch.Failed("The list answer could not be read.");
        }

        return result.Match
        (
          response => Found(prefix, idTypeName, items(response) ?? [], id, label),
          _ => ResourceReferenceSearch.Failed("The list returned a file."),
          problem => ResourceReferenceSearch.Failed(problem.Title ?? "The list could not be loaded.")
        );
      }
    );
  }

  private static ResourceReferenceSearch Found<TItem>
  (
    string prefix,
    string idTypeName,
    IEnumerable<TItem> items,
    Func<TItem, string> id,
    Func<TItem, string?> label
  )
  {
    List<ResourceReferenceItem> found = [];
    bool hasMore = false;
    foreach (TItem item in items)
    {
      if (found.Count == ResultCap)
      {
        hasMore = true;
        break;
      }

      string value = id(item);
      string? text = label(item);
      found.Add(new ResourceReferenceItem(prefix, value, string.IsNullOrWhiteSpace(text) ? value : text.Trim(), idTypeName));
    }

    return new ResourceReferenceSearch(found, hasMore, Problem: null);
  }
}
