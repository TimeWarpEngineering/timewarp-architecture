#region Purpose
// Publishes raw file request and response bodies in the OpenAPI document as binary, not JSON.
#endregion

#region Design
// A file upload's body is the file and a file download writes the bytes, but FastEndpoints
// describes both as the JSON DTO. A generated client then POSTs JSON and JSON-parses an image.
// The operation transformer finds those operations from the contract markers, not the route:
// - The request type implements IFileUploadRequest. Each request media type gets the schema
//   {type: string, format: binary}. The media types are the endpoint's accepts metadata, which is
//   also what the host checks for 415. An upload that still accepts application/json throws,
//   naming the endpoint: HttpApiService never sends one. The file name travels in X-File-Name
//   (FileUploadHeaders), so a required header parameter is added. The header is percent-encoded
//   and the limit applies after decoding, so the limit is in the description, not maxLength: a
//   maxLength would refuse a short name whose encoding is long. The 413 problem is added beside
//   the other problem responses.
// - A success arm of the response type has a Stream property. Each 2xx response loses its JSON
//   content and gets one binary entry per FileContentMetadata media type, or
//   application/octet-stream when the endpoint declares none, plus the Content-Disposition header.
// FileContentMetadata carries the feature's limits, because foundation-server cannot reference
// the feature's rules class.
//
// The document transformer removes the components only a replaced body used: the Stream schema
// and every component that reaches it. One still reachable from an operation stays, so a JSON
// operation that carries a Stream keeps its schema and the OpenApiBinaryContent_ test finds it.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;
using TimeWarp.Foundation.Features;

/// <summary>
/// Publishes file uploads and downloads as <c>format: binary</c> bodies.
/// </summary>
internal static class OpenApiBinaryContent
{
  private const string Json = "application/json";
  private const string OctetStream = "application/octet-stream";
  private const string ContentDisposition = "Content-Disposition";
  private const string SchemaIdKey = "x-schema-id";
  private const string TooLargeStatus = "413";

  /// <summary>
  /// Registers the binary body operation transformer and the component cleanup.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddOperationTransformer(TransformOperationAsync);
    openApiOptions.AddDocumentTransformer(RemoveStreamComponentsAsync);
  }

  /// <summary>
  /// True when <paramref name="type"/> is a file upload request.
  /// </summary>
  internal static bool IsFileUpload(Type? type) =>
    type is not null && typeof(IFileUploadRequest).IsAssignableFrom(type);

  /// <summary>
  /// True when a success arm of <paramref name="responseType"/> has a <see cref="Stream"/> property.
  /// </summary>
  internal static bool IsFileDownload(Type? responseType)
  {
    if (responseType is null)
    {
      return false;
    }

    Type[] arms = OpenApiOneOf.Arms(responseType) ?? [responseType];
    return arms.Any(static arm => arm != typeof(SharedProblemDetails) && HasStream(arm));
  }

  private static bool HasStream(Type type) =>
    type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Any(static property => typeof(Stream).IsAssignableFrom(property.PropertyType));

  private static async Task TransformOperationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .FirstOrDefault();
    if (endpoint is null)
    {
      return;
    }

    FileContentMetadata? metadata = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<FileContentMetadata>()
      .FirstOrDefault();
    if (IsFileUpload(endpoint.ReqDtoType))
    {
      await TransformUploadAsync(operation, context, metadata, cancellationToken).ConfigureAwait(false);
    }

    if (IsFileDownload(endpoint.ResDtoType))
    {
      TransformDownload(operation, metadata);
    }
  }

  private static async Task TransformUploadAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    FileContentMetadata? metadata,
    CancellationToken cancellationToken
  )
  {
    string label = $"{context.Description.HttpMethod} /{context.Description.RelativePath}";
    List<string> mediaTypes = [.. operation.RequestBody?.Content?.Keys ?? []];
    if (mediaTypes.Any(IsJson))
    {
      throw new InvalidOperationException
      (
        $"OpenApiBinaryContent: {label} takes an IFileUploadRequest but accepts {Json}. The body is the "
        + "file; clear the default accepts metadata and declare the file's media types."
      );
    }

    if (mediaTypes.Count == 0)
    {
      mediaTypes.Add(OctetStream);
    }

    operation.RequestBody = new OpenApiRequestBody
    {
      Description = "The file bytes. Content-Type is the file's media type.",
      Required = true,
      Content = BinaryContent(mediaTypes)
    };

    if (operation.Parameters?.IsReadOnly != false)
    {
      operation.Parameters = operation.Parameters is null ? [] : [.. operation.Parameters];
    }

    string limit = metadata?.MaxFileNameLength is int maxLength
      ? $" At most {maxLength} characters after decoding."
      : string.Empty;
    operation.Parameters.Add
    (
      new OpenApiParameter
      {
        Name = FileUploadHeaders.FileName,
        In = ParameterLocation.Header,
        Required = true,
        Description = "Original file name without a directory, percent-encoded." + limit,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 }
      }
    );

    operation.Responses ??= [];
    if (!operation.Responses.ContainsKey(TooLargeStatus))
    {
      string size = metadata?.MaxBytes is long maxBytes ? $" of {maxBytes} bytes" : string.Empty;
      OpenApiResponse tooLarge = new() { Description = $"The file is larger than the limit{size}." };
      OpenApiProblemDetails.SetContent
      (
        tooLarge,
        await OpenApiProblemDetails.ReferenceAsync(context, cancellationToken).ConfigureAwait(false)
      );
      operation.Responses[TooLargeStatus] = tooLarge;
    }
  }

  private static void TransformDownload(OpenApiOperation operation, FileContentMetadata? metadata)
  {
    List<string> mediaTypes = metadata is { ContentTypes.Count: > 0 } ? [.. metadata.ContentTypes] : [OctetStream];
    foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses ?? [])
    {
      if (response.Key.Length != 3 || response.Key[0] != '2' || response.Value is not OpenApiResponse success)
      {
        continue;
      }

      success.Description = "The file bytes. Content-Type is the file's media type.";
      success.Content = BinaryContent(mediaTypes);
      success.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.OrdinalIgnoreCase);
      success.Headers[ContentDisposition] = new OpenApiHeader
      {
        Description = "inline or attachment, with the file name in filename and the UTF-8 name in filename*.",
        Required = true,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String }
      };
    }
  }

  private static Dictionary<string, IOpenApiMediaType> BinaryContent(IEnumerable<string> mediaTypes)
  {
    Dictionary<string, IOpenApiMediaType> content = new(StringComparer.OrdinalIgnoreCase);
    foreach (string mediaType in mediaTypes)
    {
      content[mediaType] = new OpenApiMediaType
      {
        Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
      };
    }

    return content;
  }

  private static bool IsJson(string mediaType) =>
    mediaType.StartsWith(Json, StringComparison.OrdinalIgnoreCase)
    || mediaType.Contains("+json", StringComparison.OrdinalIgnoreCase)
    || string.Equals(mediaType, "*/*", StringComparison.Ordinal);

  private static async Task RemoveStreamComponentsAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    IDictionary<string, IOpenApiSchema>? components = document.Components?.Schemas;
    if (components is null || components.Count == 0)
    {
      return;
    }

    OpenApiSchema streamSchema = await context
      .GetOrCreateSchemaAsync(typeof(Stream), parameterDescription: null, cancellationToken)
      .ConfigureAwait(false);
    if (streamSchema.Metadata?.TryGetValue(SchemaIdKey, out object? value) != true
      || value is not string streamId
      || !components.ContainsKey(streamId))
    {
      return;
    }

    Dictionary<string, HashSet<string>> edges = new(StringComparer.Ordinal);
    foreach (KeyValuePair<string, IOpenApiSchema> component in components)
    {
      HashSet<string> references = new(StringComparer.Ordinal);
      OpenApiProblemDetails.CollectReferences(component.Value, references, component.Key);
      edges[component.Key] = references;
    }

    HashSet<string> carriesStream = new(StringComparer.Ordinal) { streamId };
    bool grew = true;
    while (grew)
    {
      grew = false;
      foreach (KeyValuePair<string, HashSet<string>> edge in edges)
      {
        if (!carriesStream.Contains(edge.Key) && edge.Value.Overlaps(carriesStream))
        {
          carriesStream.Add(edge.Key);
          grew = true;
        }
      }
    }

    HashSet<string> reachable = new(StringComparer.Ordinal);
    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (OpenApiOperation operation in path.Value.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
      {
        OpenApiProblemDetails.CollectOperationReferences(operation, reachable);
      }
    }

    Queue<string> pending = new(reachable);
    while (pending.TryDequeue(out string? id))
    {
      foreach (string child in edges.GetValueOrDefault(id) ?? [])
      {
        if (reachable.Add(child))
        {
          pending.Enqueue(child);
        }
      }
    }

    foreach (string id in carriesStream.Where(id => !reachable.Contains(id)))
    {
      components.Remove(id);
    }
  }
}
