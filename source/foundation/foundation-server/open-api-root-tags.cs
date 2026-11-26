#region Purpose
// Declares every operation tag at the OpenAPI document root, with the host's description for each.
#endregion

#region Design
// Operations carry tags (generator Description.WithTags, hand-written endpoints, the discovery
// operations), but the document had no root tags array, so linters reported every tag as
// undefined and Scalar had no group descriptions. The transformer runs after the host callback,
// so operations a host transformer adds are counted. It keeps any root tag already present, adds
// one per tag in use, and fills an empty description from the host's map. Root tags are written
// in ordinal name order, so the document is stable between builds.
// The document check is registered last, with the other checks. It fails when an operation tag is
// not declared at the root, and, when the host passed tag descriptions, when a root tag has no
// description. The throw makes /openapi/{doc}.json return 500 with the tag names, so a new
// feature slice without a description fails the OpenAPI tests. A description for a tag no
// operation uses is ignored here; the web-server root-tags test reports it.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;

/// <summary>
/// Root <c>tags</c> from the tags operations use.
/// </summary>
internal static class OpenApiRootTags
{
  internal static void Register(OpenApiOptions openApiOptions, IReadOnlyDictionary<string, string>? tagDescriptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer
    (
      (document, context, cancellationToken) =>
      {
        _ = context;
        _ = cancellationToken;
        DeclareTags(document, tagDescriptions);
        return Task.CompletedTask;
      }
    );
  }

  /// <summary>
  /// Registers the document check. Call after every transformer that adds or retags an operation.
  /// </summary>
  internal static void RegisterDocumentCheck(OpenApiOptions openApiOptions, IReadOnlyDictionary<string, string>? tagDescriptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer
    (
      (document, context, cancellationToken) =>
      {
        _ = context;
        _ = cancellationToken;
        CheckDocument(document, tagDescriptions is not null);
        return Task.CompletedTask;
      }
    );
  }

  /// <summary>The tag names operations use, in ordinal order.</summary>
  internal static SortedSet<string> OperationTagNames(OpenApiDocument document)
  {
    SortedSet<string> names = new(StringComparer.Ordinal);
    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations ?? [])
      {
        foreach (OpenApiTagReference tag in operation.Value.Tags ?? new HashSet<OpenApiTagReference>())
        {
          string? name = tag.Reference.Id ?? tag.Name;
          if (!string.IsNullOrEmpty(name))
          {
            names.Add(name);
          }
        }
      }
    }

    return names;
  }

  private static void DeclareTags(OpenApiDocument document, IReadOnlyDictionary<string, string>? tagDescriptions)
  {
    SortedDictionary<string, OpenApiTag> tags = new(StringComparer.Ordinal);
    foreach (OpenApiTag existing in document.Tags ?? new HashSet<OpenApiTag>())
    {
      if (!string.IsNullOrEmpty(existing.Name))
      {
        tags[existing.Name] = existing;
      }
    }

    foreach (string name in OperationTagNames(document))
    {
      if (!tags.ContainsKey(name))
      {
        tags[name] = new OpenApiTag { Name = name };
      }
    }

    foreach (OpenApiTag tag in tags.Values)
    {
      if (string.IsNullOrWhiteSpace(tag.Description)
        && tagDescriptions is not null
        && tagDescriptions.TryGetValue(tag.Name!, out string? description))
      {
        tag.Description = description;
      }
    }

    document.Tags = new HashSet<OpenApiTag>(tags.Values);
  }

  private static void CheckDocument(OpenApiDocument document, bool requireDescriptions)
  {
    HashSet<string> declared = new(StringComparer.Ordinal);
    List<string> undescribed = [];
    foreach (OpenApiTag tag in document.Tags ?? new HashSet<OpenApiTag>())
    {
      if (string.IsNullOrEmpty(tag.Name))
      {
        continue;
      }

      declared.Add(tag.Name);
      if (requireDescriptions && string.IsNullOrWhiteSpace(tag.Description))
      {
        undescribed.Add(tag.Name);
      }
    }

    var undeclared = OperationTagNames(document).Where(name => !declared.Contains(name)).ToList();
    if (undeclared.Count > 0)
    {
      throw new InvalidOperationException
      (
        "OpenApiRootTags: these operation tags are not declared in the root tags: "
        + string.Join(", ", undeclared)
        + ". Register the transformer that adds them before OpenApiRootTags in CommonServerModule.AddOpenApi."
      );
    }

    if (undescribed.Count > 0)
    {
      throw new InvalidOperationException
      (
        "OpenApiRootTags: these tags have no description: "
        + string.Join(", ", undescribed)
        + ". Add each to the tag descriptions the host passes in OpenApiDocumentInfo."
      );
    }
  }
}
