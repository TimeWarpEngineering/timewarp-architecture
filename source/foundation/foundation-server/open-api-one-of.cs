#region Purpose
// Publishes OneOf unions in the OpenAPI document as their wire shape, not the OneOf struct.
#endregion

#region Design
// Contract endpoints declare OneOf<Response, SharedProblemDetails> as the FastEndpoints response
// type. System.Text.Json schema export describes the OneOf struct itself, so each endpoint added a
// OneOfOneOfOf… component with value, index, isT0…, and asT0… members. No client ever receives
// that object: BaseFastEndpoint writes success as bare Response with 200 and application/json,
// and failure as SharedProblemDetails with application/problem+json and the problem's status.
//
// Three hooks, all registered after the FastEndpoints transformers:
// - CreateSchemaReferenceId returns null for a OneOf type, so no component is registered and
//   the union is inlined at each use. Other types keep the FastEndpoints names.
// - The schema transformer replaces a OneOf schema with oneOf over its asT0… member schemas.
//   The FastEndpoints transformers have already run on those members, and reference resolution
//   walks oneOf, so each arm stays a $ref to the component of its type. A shape without the
//   expected asT members is left alone rather than guessed at. A DTO property typed as a OneOf
//   gets the same oneOf. The contract JSON options register no OneOf converter, and the default
//   serializer throws on the inactive AsT getter, so the response union is the only OneOf on the
//   wire today; the property rule keeps the document free of wrappers either way.
// - The operation transformer splits a response union that contains SharedProblemDetails. The
//   2xx content keeps the success arm, or oneOf over the success arms when there are several.
//   The problem arm is published under application/problem+json: on a 4xx or 5xx response that
//   already lists that media type, and on default, because the status is whatever the handler
//   put on the problem (400 when it sets none).
#endregion

namespace TimeWarp.Foundation;

using System.Text.Json.Serialization.Metadata;
using Microsoft.OpenApi;

/// <summary>
/// Publishes OneOf response and property types as <c>oneOf</c> over their arms.
/// </summary>
internal static class OpenApiOneOf
{
  private const string ProblemJson = "application/problem+json";
  private const string DefaultResponse = "default";

  /// <summary>
  /// Registers the OneOf reference id, schema, and operation transformers on the document.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    Func<JsonTypeInfo, string?> createSchemaReferenceId = openApiOptions.CreateSchemaReferenceId;
    openApiOptions.CreateSchemaReferenceId = typeInfo =>
      Arms(typeInfo.Type) is null ? createSchemaReferenceId(typeInfo) : null;
    openApiOptions.AddSchemaTransformer(TransformSchemaAsync);
    openApiOptions.AddOperationTransformer(TransformOperationAsync);
  }

  /// <summary>
  /// The OneOf arm types of <paramref name="type"/>, or null when it is not a OneOf union.
  /// </summary>
  internal static Type[]? Arms(Type? type)
  {
    type = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
    if (type is null || !typeof(IOneOf).IsAssignableFrom(type))
    {
      return null;
    }

    for (Type? current = type; current is not null; current = current.BaseType)
    {
      if (current.IsGenericType
        && string.Equals(current.Namespace, typeof(IOneOf).Namespace, StringComparison.Ordinal))
      {
        return current.GetGenericArguments();
      }
    }

    return null;
  }

  private static Task TransformSchemaAsync
  (
    OpenApiSchema schema,
    OpenApiSchemaTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    Type[]? arms = Arms(context.JsonTypeInfo.Type);
    if (arms is null || schema.Properties is null)
    {
      return Task.CompletedTask;
    }

    List<IOpenApiSchema> oneOf = [];
    for (int index = 0; index < arms.Length; index++)
    {
      IOpenApiSchema? arm = schema.Properties
        .FirstOrDefault(property => string.Equals(property.Key, $"asT{index}", StringComparison.OrdinalIgnoreCase))
        .Value;
      if (arm is null)
      {
        return Task.CompletedTask;
      }

      oneOf.Add(arm);
    }

    schema.Type = null;
    schema.Properties = null;
    schema.Required = null;
    schema.OneOf = oneOf;
    return Task.CompletedTask;
  }

  private static Task TransformOperationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .FirstOrDefault();
    Type[]? arms = Arms(endpoint?.ResDtoType);
    int problemIndex = arms is null ? -1 : Array.IndexOf(arms, typeof(SharedProblemDetails));
    if (arms is null || problemIndex < 0 || operation.Responses is null)
    {
      return Task.CompletedTask;
    }

    IOpenApiSchema? problem = null;
    foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses)
    {
      if (!IsStatusClass(response.Key, '2') || response.Value.Content is null)
      {
        continue;
      }

      foreach (OpenApiMediaType mediaType in response.Value.Content.Values)
      {
        if (mediaType.Schema is not OpenApiSchema { OneOf: { } union } || union.Count != arms.Length)
        {
          continue;
        }

        problem ??= union[problemIndex];
        List<IOpenApiSchema> success = [.. union.Where((_, index) => index != problemIndex)];
        mediaType.Schema = success.Count == 1 ? success[0] : new OpenApiSchema { OneOf = success };
      }
    }

    if (problem is null)
    {
      return Task.CompletedTask;
    }

    foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses)
    {
      if ((IsStatusClass(response.Key, '4') || IsStatusClass(response.Key, '5'))
        && response.Value.Content?.TryGetValue(ProblemJson, out OpenApiMediaType? mediaType) == true)
      {
        mediaType.Schema = problem;
      }
    }

    if (!operation.Responses.ContainsKey(DefaultResponse))
    {
      operation.Responses[DefaultResponse] = new OpenApiResponse
      {
        Description = "Problem details. The status is the problem's own status, 400 when the handler sets none.",
        Content = new Dictionary<string, OpenApiMediaType>
        {
          [ProblemJson] = new OpenApiMediaType { Schema = problem }
        }
      };
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// True when the response key is a status code in the class that starts with <paramref name="first"/>.
  /// </summary>
  private static bool IsStatusClass(string responseKey, char first) =>
    responseKey.Length == 3 && responseKey[0] == first;
}
