#region Purpose
// Names and summarizes every OpenAPI operation from its contract's [Operation], fills member
// descriptions FastEndpoints leaves empty, and checks the result when the document is built.
#endregion

#region Design
// Task 458-005. The contracts generator copies [Operation] onto the request partial as the
// OperationName and OperationDescription constants. FastEndpoints writes the endpoint name
// (namespace plus class name) as the operationId, so the operation transformer replaces it with
// OperationName and sets summary to OperationDescription. A hand-written endpoint whose request
// is not a contract (EndpointWithoutRequest) adds OpenApiOperationName metadata instead, and that
// metadata wins over the request type. The transformer registers after OpenApiDeleteRequestBody,
// so a parameter that transformer moved to the query string gets a description too.
// Descriptions: a parameter with none gets its request property's XML <summary>, else the
// summary on the same-named property of an interface the request implements (an I*Details
// interface is the shared source of those members). A schema property FastEndpoints left
// without a description gets the interface summary the same way. The schema transformer
// registers before OpenApiOneOf, because a schema transformer after it never reaches the types
// inside a response. A $ref property keeps its description in x-ref-description metadata, the
// key FastEndpoints writes, so the reference sibling carries it.
// The document check registers last. It fails the build of the document when two operations
// share an operationId, or when a description quotes (in backticks) an operation id that no
// operation has. Prose names an operation as `roles.list`, so a renamed or removed operation
// cannot leave a stale reference behind. It does not require a summary or the
// <resource>.<action> shape (lower case, dot-separated, kebab segments) on every operation:
// test hosts publish probe endpoints with no contract, so that rule is a test on the real
// web-server document (open-api-operation-names-tests.cs), next to TWA0035 on the contracts.
#endregion

namespace TimeWarp.Foundation;

using System.Text.RegularExpressions;
using Microsoft.OpenApi;

/// <summary>Operation ids, summaries, and member descriptions from contracts.</summary>
internal static partial class OpenApiOperationNames
{
  internal const string OperationNameConstant = "OperationName";
  internal const string OperationDescriptionConstant = "OperationDescription";

  /// <summary>Registers the schema-property description transformer. Call before OpenApiOneOf.</summary>
  internal static void RegisterSchemaDescriptions(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddSchemaTransformer(DescribeSchemaAsync);
  }

  /// <summary>Registers the operation transformer. Call after OpenApiDeleteRequestBody.</summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddOperationTransformer(NameOperationAsync);
  }

  /// <summary>Registers the document check. Call after every transformer that adds or edits an operation.</summary>
  internal static void RegisterDocumentCheck(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer
    (
      (document, context, cancellationToken) =>
      {
        _ = context;
        _ = cancellationToken;
        CheckDocument(document);
        return Task.CompletedTask;
      }
    );
  }

  /// <summary>True when <paramref name="operationId"/> is <c>&lt;resource&gt;.&lt;action&gt;</c>.</summary>
  internal static bool IsOperationId(string? operationId) =>
    operationId is not null && OperationIdPattern().IsMatch(operationId);

  /// <summary>The backtick-quoted operation ids in <paramref name="text"/>.</summary>
  internal static IEnumerable<string> QuotedOperationIds(string? text)
  {
    if (string.IsNullOrEmpty(text))
    {
      yield break;
    }

    foreach (Match match in QuotedPattern().Matches(text))
    {
      string quoted = match.Groups[1].Value;
      if (IsOperationId(quoted))
      {
        yield return quoted;
      }
    }
  }

  /// <summary>The value of a public <c>const string</c> on <paramref name="type"/>, or null.</summary>
  internal static string? Constant(Type? type, string name)
  {
    FieldInfo? field = type?.GetField(name, BindingFlags.Public | BindingFlags.Static);
    return field is { IsLiteral: true } && field.GetRawConstantValue() is string value
      && !string.IsNullOrWhiteSpace(value)
      ? value
      : null;
  }

  private static Task NameOperationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    IList<object> metadata = context.Description.ActionDescriptor.EndpointMetadata;
    EndpointDefinition? endpoint = metadata.OfType<EndpointDefinition>().FirstOrDefault();
    Type? request = endpoint?.ReqDtoType;

    OpenApiOperationName? named = metadata.OfType<OpenApiOperationName>().LastOrDefault();
    string? name = named?.Name ?? Constant(request, OperationNameConstant);
    string? summary = named?.Description ?? Constant(request, OperationDescriptionConstant);
    if (name is not null)
    {
      operation.OperationId = name;
    }

    if (summary is not null)
    {
      operation.Summary = summary;
    }

    if (request is not null)
    {
      DescribeParameters(operation, request);
    }

    return Task.CompletedTask;
  }

  private static void DescribeParameters(OpenApiOperation operation, Type request)
  {
    foreach (IOpenApiParameter candidate in operation.Parameters ?? [])
    {
      if (candidate is not OpenApiParameter parameter
        || !string.IsNullOrWhiteSpace(parameter.Description)
        || string.IsNullOrEmpty(parameter.Name))
      {
        continue;
      }

      PropertyInfo? property = request.GetProperty
      (
        parameter.Name,
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
      );
      if (property is not null && OpenApiMemberDocumentation.Summary(property) is { } summary)
      {
        parameter.Description = summary;
      }
    }
  }

  private static Task DescribeSchemaAsync
  (
    OpenApiSchema schema,
    OpenApiSchemaTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    if (context.JsonPropertyInfo?.AttributeProvider is not PropertyInfo property)
    {
      return Task.CompletedTask;
    }

    bool isReference = schema.Metadata?.TryGetValue("x-schema-id", out object? id) == true
      && id is string { Length: > 0 };
    if (isReference)
    {
      if (schema.Metadata!.ContainsKey("x-ref-description"))
      {
        return Task.CompletedTask;
      }
    }
    else if (!string.IsNullOrWhiteSpace(schema.Description))
    {
      return Task.CompletedTask;
    }

    string? summary = OpenApiMemberDocumentation.Summary(property);
    if (summary is null)
    {
      return Task.CompletedTask;
    }

    if (isReference)
    {
      schema.Metadata!["x-ref-description"] = summary;
    }
    else
    {
      schema.Description = summary;
    }

    return Task.CompletedTask;
  }

  private static void CheckDocument(OpenApiDocument document)
  {
    List<string> problems = [];
    Dictionary<string, string> operationIds = new(StringComparer.Ordinal);
    List<(string Where, string? Text)> texts =
    [
      ("info.description", document.Info?.Description),
    ];

    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (KeyValuePair<HttpMethod, OpenApiOperation> entry in path.Value.Operations ?? [])
      {
        string where = entry.Key.Method.ToUpperInvariant() + " " + path.Key;
        OpenApiOperation operation = entry.Value;
        if (!string.IsNullOrEmpty(operation.OperationId) && !operationIds.TryAdd(operation.OperationId, where))
        {
          problems.Add(where + " repeats operationId '" + operation.OperationId + "' from " + operationIds[operation.OperationId!]);
        }

        texts.Add((where + " summary", operation.Summary));
        texts.Add((where + " description", operation.Description));
        texts.Add((where + " request body", operation.RequestBody?.Description));
        foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
        {
          texts.Add((where + " parameter " + parameter.Name, parameter.Description));
        }

        foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses ?? [])
        {
          texts.Add((where + " response " + response.Key, response.Value.Description));
        }
      }
    }

    foreach (OpenApiTag tag in document.Tags ?? new HashSet<OpenApiTag>())
    {
      texts.Add(("tag " + tag.Name, tag.Description));
    }

    foreach (KeyValuePair<string, IOpenApiSecurityScheme> scheme in document.Components?.SecuritySchemes ?? new Dictionary<string, IOpenApiSecurityScheme>())
    {
      texts.Add(("security scheme " + scheme.Key, scheme.Value.Description));
    }

    foreach (KeyValuePair<string, IOpenApiSchema> schema in document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
    {
      texts.Add(("schema " + schema.Key, schema.Value.Description));
      foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Value.Properties ?? new Dictionary<string, IOpenApiSchema>())
      {
        texts.Add(("schema " + schema.Key + "." + property.Key, property.Value.Description));
      }
    }

    foreach ((string where, string? text) in texts)
    {
      foreach (string quoted in QuotedOperationIds(text))
      {
        if (!operationIds.ContainsKey(quoted))
        {
          problems.Add(where + " quotes `" + quoted + "`, which is not an operationId in this document");
        }
      }
    }

    if (problems.Count > 0)
    {
      throw new InvalidOperationException
      (
        "OpenApiOperationNames: " + string.Join("; ", problems)
        + ". Quote only operation ids this document has, and give each operation its own"
        + " [Operation(\"<resource>.<action>\")] name (a hand-written endpoint adds OpenApiOperationName metadata)."
      );
    }
  }

  [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*(?:\\.[a-z][a-z0-9]*(?:-[a-z0-9]+)*)+$", RegexOptions.CultureInvariant)]
  private static partial Regex OperationIdPattern();

  [GeneratedRegex("`([^`\\s]+)`", RegexOptions.CultureInvariant)]
  private static partial Regex QuotedPattern();
}
