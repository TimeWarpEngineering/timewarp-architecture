#region Purpose
// Drops required request bodies from DELETE operations in the OpenAPI document.
#endregion

#region Design
// HttpApiService sends no body for DELETE. Non-route values travel on the query string
// (IQueryStringRouteProvider). FastEndpoints.OpenApi promotes unbound properties to query
// parameters only for GET and HEAD, so a DELETE command that also carries UserId is published
// as requestBody.required with */* and application/json. Contracts cannot take a FastEndpoints
// [QueryParam]: web-contracts does not reference that package. This transformer runs after the
// FastEndpoints operation transformer, moves leftover scalar properties to query parameters, and
// removes the body. A leftover object or collection stays, with required set to false, so a real
// payload is not rewritten into query parameters.
#endregion

namespace TimeWarp.Foundation;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Publishes DELETE operations without a required JSON body.
/// </summary>
internal static class OpenApiDeleteRequestBody
{
  /// <summary>
  /// Registers the DELETE request-body transformer on the document.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddOperationTransformer(TransformAsync);
  }

  private static Task TransformAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    if (!string.Equals(context.Description.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
    {
      return Task.CompletedTask;
    }

    if (operation.RequestBody is null)
    {
      return Task.CompletedTask;
    }

    EndpointDefinition? endpoint = context.Description.ActionDescriptor.EndpointMetadata
      .OfType<EndpointDefinition>()
      .FirstOrDefault();
    if (endpoint is null)
    {
      return Task.CompletedTask;
    }

    HashSet<string> routeNames = RouteNames(endpoint, context.Description.RelativePath);
    List<PropertyInfo>? queryProperties = QueryProperties(endpoint.ReqDtoType, routeNames);
    if (queryProperties is null)
    {
      if (operation.RequestBody is OpenApiRequestBody requestBody)
      {
        requestBody.Required = false;
      }

      return Task.CompletedTask;
    }

    if (operation.Parameters?.IsReadOnly != false)
    {
      operation.Parameters = operation.Parameters is null
        ? []
        : [.. operation.Parameters];
    }

    foreach (PropertyInfo property in queryProperties)
    {
      string name = QueryName(property);
      if (HasParameter(operation, name))
      {
        continue;
      }

      operation.Parameters.Add
      (
        new OpenApiParameter
        {
          Name = name,
          In = ParameterLocation.Query,
          Required = !IsOptional(property),
          Schema = BodyPropertySchema(operation, property.Name, name) ?? SchemaFor(property.PropertyType)
        }
      );
    }

    operation.RequestBody = null;
    return Task.CompletedTask;
  }

  /// <summary>
  /// Scalar properties that are not route parameters. Null when any leftover property is a
  /// complex object or collection, which must not be rewritten as a query parameter.
  /// </summary>
  private static List<PropertyInfo>? QueryProperties(Type requestType, HashSet<string> routeNames)
  {
    List<PropertyInfo> properties = [];
    foreach (PropertyInfo property in requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      if (property.GetIndexParameters().Length != 0 || property.GetMethod?.IsPublic != true)
      {
        continue;
      }

      if (property.SetMethod?.IsPublic != true)
      {
        continue;
      }

      JsonIgnoreAttribute? ignored = property.GetCustomAttribute<JsonIgnoreAttribute>();
      if (ignored is { Condition: JsonIgnoreCondition.Always })
      {
        continue;
      }

      if (routeNames.Contains(property.Name))
      {
        continue;
      }

      if (!IsScalar(property.PropertyType))
      {
        return null;
      }

      properties.Add(property);
    }

    return properties;
  }

  private static HashSet<string> RouteNames(EndpointDefinition endpoint, string? relativePath)
  {
    HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
    foreach (string route in endpoint.Routes)
    {
      AddRouteNames(names, route);
    }

    AddRouteNames(names, relativePath);
    return names;
  }

  private static void AddRouteNames(HashSet<string> names, string? route)
  {
    if (string.IsNullOrEmpty(route))
    {
      return;
    }

    int start = -1;
    for (int index = 0; index < route.Length; index++)
    {
      char character = route[index];
      if (character == '{')
      {
        start = index + 1;
        continue;
      }

      if (character != '}' || start < 0)
      {
        continue;
      }

      string token = route[start..index];
      int colon = token.IndexOf(':', StringComparison.Ordinal);
      if (colon >= 0)
      {
        token = token[..colon];
      }

      int equals = token.IndexOf('=', StringComparison.Ordinal);
      if (equals >= 0)
      {
        token = token[..equals];
      }

      token = token.Trim().TrimStart('*').TrimEnd('?');
      if (token.Length > 0)
      {
        names.Add(token);
      }

      start = -1;
    }
  }

  private static bool HasParameter(OpenApiOperation operation, string name)
  {
    if (operation.Parameters is null)
    {
      return false;
    }

    foreach (IOpenApiParameter parameter in operation.Parameters)
    {
      if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static string QueryName(PropertyInfo property)
  {
    JsonPropertyNameAttribute? named = property.GetCustomAttribute<JsonPropertyNameAttribute>();
    return named?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
  }

  private static bool IsOptional(PropertyInfo property)
  {
    if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
    {
      return true;
    }

    if (!property.PropertyType.IsValueType)
    {
      NullabilityInfo info = new NullabilityInfoContext().Create(property);
      return info.WriteState == NullabilityState.Nullable;
    }

    return false;
  }

  private static bool IsScalar(Type type)
  {
    Type underlying = Nullable.GetUnderlyingType(type) ?? type;
    return underlying.IsPrimitive
      || underlying.IsEnum
      || underlying == typeof(string)
      || underlying == typeof(Guid)
      || underlying == typeof(decimal)
      || underlying == typeof(DateTime)
      || underlying == typeof(DateTimeOffset)
      || underlying == typeof(DateOnly)
      || underlying == typeof(TimeOnly)
      || underlying == typeof(TimeSpan)
      || underlying == typeof(Uri);
  }

  private static IOpenApiSchema? BodyPropertySchema(OpenApiOperation operation, string clrName, string queryName)
  {
    if (operation.RequestBody is not OpenApiRequestBody requestBody || requestBody.Content is null)
    {
      return null;
    }

    foreach (OpenApiMediaType mediaType in requestBody.Content.Values)
    {
      OpenApiSchema? schema = Resolve(mediaType.Schema);
      if (schema?.Properties is null)
      {
        continue;
      }

      foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties)
      {
        if (string.Equals(property.Key, queryName, StringComparison.OrdinalIgnoreCase)
          || string.Equals(property.Key, clrName, StringComparison.OrdinalIgnoreCase))
        {
          return property.Value;
        }
      }
    }

    return null;
  }

  private static OpenApiSchema? Resolve(IOpenApiSchema? schema, int depth = 0)
  {
    if (schema is null || depth > 8)
    {
      return null;
    }

    if (schema is OpenApiSchema concrete)
    {
      return concrete;
    }

    if (schema is OpenApiSchemaReference reference)
    {
      return Resolve(reference.Target, depth + 1);
    }

    return null;
  }

  private static OpenApiSchema SchemaFor(Type type)
  {
    Type underlying = Nullable.GetUnderlyingType(type) ?? type;
    if (underlying == typeof(Guid))
    {
      return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
    }

    if (underlying == typeof(Uri))
    {
      return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" };
    }

    if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly))
    {
      return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
    }

    if (underlying == typeof(bool))
    {
      return new OpenApiSchema { Type = JsonSchemaType.Boolean };
    }

    if (underlying == typeof(byte) || underlying == typeof(short) || underlying == typeof(int) || underlying == typeof(long))
    {
      return new OpenApiSchema { Type = JsonSchemaType.Integer };
    }

    if (underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
    {
      return new OpenApiSchema { Type = JsonSchemaType.Number };
    }

    return new OpenApiSchema { Type = JsonSchemaType.String };
  }
}
