#region Purpose
// Publishes one problem details schema, SharedProblemDetails, for every problem+json response in the OpenAPI document.
#endregion

#region Design
// Every problem body the hosts write is SharedProblemDetails: the contract union's problem arm,
// FluentValidationBehavior's validation 400, the agent bearer 401 and 403, the payment 402 and
// 503, and the Entra ceremony 400 and 403. The document names that one component.
//
// The schema transformer completes the component. It is registered before OpenApiOneOf: the
// transformers run one at a time over the whole schema tree, and the OneOf transformer replaces a
// union's members with oneOf, so a later transformer never visits the SharedProblemDetails arm. SharedProblemDetails keeps everything past
// type, title, status, detail, and instance in a JsonExtensionData dictionary, which schema export
// does not describe. The validation 400 always carries errors (member name to its messages), so
// the component declares errors, and additionalProperties {} states that other extension
// members (traceId, payment, policy) may appear.
//
// ReferenceAsync is how a transformer points a response at the component. It asks the document
// service for the schema of SharedProblemDetails, reads the component id that service assigned
// (x-schema-id, from the CreateSchemaReferenceId chain), registers the component if no
// operation has registered it yet, and returns a $ref. No transformer spells the component id.
//
// The operation transformer documents 400 on every operation whose request type has a
// registered IValidator. FluentValidationBehavior runs those validators in the mediator
// pipeline and returns the problem with status 400, so a registered validator is the condition
// for that response on the wire.
//
// The document transformer runs last. It throws when a problem+json response does not
// reference the component, directly or as the first allOf member, naming the operation and
// status. A ProducesProblem call registers MVC ProblemDetails; when every response that used it
// was rewritten to SharedProblemDetails, the unreferenced component is removed. The throw makes
// /openapi/{doc}.json return 500 with the cause, so the OpenAPI tests fail instead of the
// document carrying a second problem shape.
#endregion

namespace TimeWarp.Foundation;

using Microsoft.OpenApi;
using TimeWarp.Foundation.Features;

/// <summary>
/// One problem details component for every <c>application/problem+json</c> response.
/// </summary>
public static class OpenApiProblemDetails
{
  private const string SchemaIdKey = "x-schema-id";
  private const string ValidationStatus = "400";

  private const string ValidationDescription =
    "Validation failed. The body is application/problem+json; errors maps each invalid request member to its messages.";

  private static readonly Type[] ReplaceableProblemTypes =
  [
    typeof(Microsoft.AspNetCore.Mvc.ProblemDetails),
    typeof(HttpValidationProblemDetails)
  ];

  /// <summary>
  /// Registers the schema, validation 400, and document check transformers.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddSchemaTransformer(TransformSchemaAsync);
    openApiOptions.AddOperationTransformer(DocumentValidationAsync);
  }

  /// <summary>
  /// Registers the document check. Call after every transformer that writes problem responses.
  /// </summary>
  internal static void RegisterDocumentCheck(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(CheckDocumentAsync);
  }

  /// <summary>
  /// A <c>$ref</c> to the SharedProblemDetails component, registering the component when it is not there yet.
  /// </summary>
  public static async Task<IOpenApiSchema> ReferenceAsync
  (
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(context);
    OpenApiDocument document = context.Document
      ?? throw new InvalidOperationException("OpenApiProblemDetails: the operation transformer context has no document.");
    OpenApiSchema schema = await context
      .GetOrCreateSchemaAsync(typeof(SharedProblemDetails), parameterDescription: null, cancellationToken)
      .ConfigureAwait(false);
    string id = SchemaId(schema)
      ?? throw new InvalidOperationException
      (
        $"OpenApiProblemDetails: {typeof(SharedProblemDetails)} has no component id, so problem responses "
        + "cannot reference it. Check OpenApiOptions.CreateSchemaReferenceId."
      );
    document.Workspace ??= new OpenApiWorkspace();
    document.AddComponent<IOpenApiSchema>(id, schema);
    return new OpenApiSchemaReference(id, document);
  }

  /// <summary>
  /// Sets the problem+json content of <paramref name="response"/> to <paramref name="schema"/>.
  /// </summary>
  public static void SetContent(OpenApiResponse response, IOpenApiSchema schema)
  {
    ArgumentNullException.ThrowIfNull(response);
    ArgumentNullException.ThrowIfNull(schema);
    response.Content ??= new Dictionary<string, IOpenApiMediaType>();
    response.Content[SharedProblemDetailsHttp.MediaType] = new OpenApiMediaType { Schema = schema };
  }

  private static Task TransformSchemaAsync
  (
    OpenApiSchema schema,
    OpenApiSchemaTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    if (context.JsonTypeInfo.Type != typeof(SharedProblemDetails))
    {
      return Task.CompletedTask;
    }

    schema.Description ??=
      "RFC 9457 problem details. errors is present on a validation failure. "
      + "Other extension members, such as traceId, may appear.";
    schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
    schema.Properties["errors"] = new OpenApiSchema
    {
      Type = JsonSchemaType.Object,
      Description = "Validation failures. Each key is an invalid request member; each value lists its messages.",
      AdditionalProperties = new OpenApiSchema
      {
        Type = JsonSchemaType.Array,
        Items = new OpenApiSchema { Type = JsonSchemaType.String }
      }
    };
    schema.AdditionalPropertiesAllowed = true;
    schema.AdditionalProperties = new OpenApiSchema();
    return Task.CompletedTask;
  }

  private static async Task DocumentValidationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    Type? requestType = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .FirstOrDefault()?
      .ReqDtoType;
    if (requestType is null || !HasValidator(context.ApplicationServices, requestType))
    {
      return;
    }

    operation.Responses ??= new OpenApiResponses();
    OpenApiResponse response;
    if (operation.Responses.TryGetValue(ValidationStatus, out IOpenApiResponse? existing) && existing is OpenApiResponse concrete)
    {
      response = concrete;
    }
    else
    {
      response = new OpenApiResponse();
      operation.Responses[ValidationStatus] = response;
    }

    response.Description = OpenApiBearerSecurity.AppendNote(response.Description, ValidationDescription);
    SetContent(response, await ReferenceAsync(context, cancellationToken).ConfigureAwait(false));
  }

  private static bool HasValidator(IServiceProvider services, Type requestType)
  {
    if (requestType.IsGenericTypeDefinition || requestType.ContainsGenericParameters)
    {
      return false;
    }

    IServiceProviderIsService? isService = services.GetService<IServiceProviderIsService>();
    return isService?.IsService(typeof(IValidator<>).MakeGenericType(requestType)) == true;
  }

  private static async Task CheckDocumentAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    string? sharedId = SchemaId
    (
      await context.GetOrCreateSchemaAsync(typeof(SharedProblemDetails), parameterDescription: null, cancellationToken)
        .ConfigureAwait(false)
    );
    List<string> wrong = [];
    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations ?? [])
      {
        foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Value.Responses ?? [])
        {
          foreach (KeyValuePair<string, IOpenApiMediaType> content in response.Value.Content ?? new Dictionary<string, IOpenApiMediaType>())
          {
            if (content.Key.StartsWith(SharedProblemDetailsHttp.MediaType, StringComparison.OrdinalIgnoreCase)
              && !References(content.Value.Schema, sharedId))
            {
              wrong.Add($"{operation.Key} {path.Key} {response.Key}");
            }
          }
        }
      }
    }

    if (wrong.Count > 0)
    {
      throw new InvalidOperationException
      (
        $"OpenApiProblemDetails: these problem+json responses do not reference {sharedId ?? nameof(SharedProblemDetails)}: "
        + string.Join(", ", wrong)
        + ". Declare the response as Produces<SharedProblemDetails>(status, \"application/problem+json\") "
        + "or set its schema with OpenApiProblemDetails.ReferenceAsync."
      );
    }

    await RemoveUnreferencedProblemComponentsAsync(document, context, cancellationToken).ConfigureAwait(false);
  }

  private static async Task RemoveUnreferencedProblemComponentsAsync
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

    HashSet<string> referenced = new(StringComparer.Ordinal);
    foreach (KeyValuePair<string, IOpenApiSchema> component in components)
    {
      CollectReferences(component.Value, referenced, component.Key);
    }

    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      foreach (OpenApiOperation operation in path.Value.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
      {
        CollectOperationReferences(operation, referenced);
      }
    }

    foreach (Type problemType in ReplaceableProblemTypes)
    {
      string? id = SchemaId
      (
        await context.GetOrCreateSchemaAsync(problemType, parameterDescription: null, cancellationToken).ConfigureAwait(false)
      );
      if (id is not null && !referenced.Contains(id))
      {
        components.Remove(id);
      }
    }
  }

  private static void CollectOperationReferences(OpenApiOperation operation, HashSet<string> referenced)
  {
    foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
    {
      CollectReferences(parameter.Schema, referenced, selfId: null);
    }

    foreach (IOpenApiMediaType mediaType in operation.RequestBody?.Content?.Values ?? [])
    {
      CollectReferences(mediaType.Schema, referenced, selfId: null);
    }

    foreach (IOpenApiResponse response in operation.Responses?.Values ?? Enumerable.Empty<IOpenApiResponse>())
    {
      foreach (IOpenApiMediaType mediaType in response.Content?.Values ?? [])
      {
        CollectReferences(mediaType.Schema, referenced, selfId: null);
      }

      foreach (IOpenApiHeader header in response.Headers?.Values ?? [])
      {
        CollectReferences(header.Schema, referenced, selfId: null);
      }
    }
  }

  /// <summary>
  /// Adds every component id <paramref name="schema"/> references, not counting a reference to <paramref name="selfId"/>.
  /// </summary>
  private static void CollectReferences(IOpenApiSchema? schema, HashSet<string> referenced, string? selfId)
  {
    if (schema is OpenApiSchemaReference reference)
    {
      string? id = reference.Reference.Id;
      if (id is not null && !string.Equals(id, selfId, StringComparison.Ordinal))
      {
        referenced.Add(id);
      }

      return;
    }

    if (schema is not OpenApiSchema concrete)
    {
      return;
    }

    IEnumerable<IOpenApiSchema?> children =
    [
      .. concrete.Properties?.Values ?? [],
      .. concrete.AllOf ?? [],
      .. concrete.OneOf ?? [],
      .. concrete.AnyOf ?? [],
      concrete.Items,
      concrete.AdditionalProperties,
      concrete.Not
    ];
    foreach (IOpenApiSchema? child in children)
    {
      CollectReferences(child, referenced, selfId);
    }
  }

  /// <summary>
  /// True when <paramref name="schema"/> is a <c>$ref</c> to <paramref name="sharedId"/>, or an allOf whose first member is.
  /// </summary>
  private static bool References(IOpenApiSchema? schema, string? sharedId) =>
    schema switch
    {
      OpenApiSchemaReference reference => sharedId is not null
        && string.Equals(reference.Reference.Id, sharedId, StringComparison.Ordinal),
      OpenApiSchema { AllOf: [IOpenApiSchema first, ..] } => References(first, sharedId),
      _ => false
    };

  private static string? SchemaId(OpenApiSchema schema) =>
    schema.Metadata is not null
      && schema.Metadata.TryGetValue(SchemaIdKey, out object? value)
      && value is string id
      && !string.IsNullOrEmpty(id)
      ? id
      : null;
}
