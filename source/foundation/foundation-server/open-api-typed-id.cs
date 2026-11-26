#region Purpose
// Publishes [TypedId] structs in the OpenAPI document as the Guid string they are on the wire.
#endregion

#region Design
// The typed-id generator gives each [TypedId] struct a JsonConverter that writes the Guid as a
// string. System.Text.Json schema export cannot see through a custom converter, so the component
// was {} plus the XML doc description, and Nullable<T> of one was {}. Kiota and NSwag then emit
// object or any for every id property.
//
// The schema transformer sets type string, format uuid on a typed id, and type [null, string],
// format uuid on Nullable<T> of one. The description and the component name stay as they are.
//
// A typed id is recognized from what the generator stamps on it, not from a list: the
// TypedIdAttribute it injects, matched by simple name because each assembly that runs the
// generator gets its own internal copy in the platform root namespace (the generator matches the
// same way), and the generated Guid Value property. A [TypedId] type without a Guid Value is not
// generator output and is left alone. The answer per type is cached: the transformer runs for
// every schema in the document.
//
// Registration goes before OpenApiOneOf. ASP.NET runs each schema transformer as its own walk,
// parent first, and the OneOf transformer replaces the union's members with oneOf, which the
// walk does not follow. Registered after it, this transformer never reaches the ids inside
// response DTOs, which is where most of them are.
#endregion

namespace TimeWarp.Foundation;

using System.Collections.Concurrent;
using Microsoft.OpenApi;

/// <summary>
/// Publishes <c>[TypedId]</c> types as <c>string</c> with format <c>uuid</c>.
/// </summary>
internal static class OpenApiTypedId
{
  private const string TypedIdAttributeName = "TypedIdAttribute";
  private const string UuidFormat = "uuid";

  private static readonly ConcurrentDictionary<Type, bool> TypedIds = new();

  /// <summary>
  /// Registers the typed-id schema transformer on the document.
  /// </summary>
  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddSchemaTransformer(TransformSchemaAsync);
  }

  /// <summary>
  /// True when <paramref name="type"/> is a struct the typed-id generator emitted: it carries
  /// <c>[TypedId]</c> and a <see cref="Guid"/> <c>Value</c> property.
  /// </summary>
  internal static bool IsTypedId(Type type)
  {
    ArgumentNullException.ThrowIfNull(type);
    return TypedIds.GetOrAdd(type, static candidate =>
      candidate.IsValueType
      && candidate.GetCustomAttributes(inherit: false)
        .Any(static attribute => string.Equals(attribute.GetType().Name, TypedIdAttributeName, StringComparison.Ordinal))
      && candidate.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.PropertyType == typeof(Guid));
  }

  private static Task TransformSchemaAsync
  (
    OpenApiSchema schema,
    OpenApiSchemaTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    Type type = context.JsonTypeInfo.Type;
    Type? underlying = Nullable.GetUnderlyingType(type);
    if (!IsTypedId(underlying ?? type))
    {
      return Task.CompletedTask;
    }

    schema.Type = underlying is null ? JsonSchemaType.String : JsonSchemaType.Null | JsonSchemaType.String;
    schema.Format = UuidFormat;
    return Task.CompletedTask;
  }
}
