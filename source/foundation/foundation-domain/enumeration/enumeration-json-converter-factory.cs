#region Purpose
// System.Text.Json factory that supplies EnumerationJsonConverter<T> for every Enumeration subclass.
#endregion

#region Design
// A factory rather than [JsonConverter] on the base class: System.Text.Json does not inherit a
// type-level JsonConverterAttribute from a base class, so one registration on the options
// (ContractSerializationDefaults.Apply) is what covers every subclass. The converter is closed
// over the declared type — a property declared as CorsPolicy resolves names against
// GetAll<CorsPolicy>() even though each member's runtime type is a nested subclass.
#endregion

namespace TimeWarp.Foundation.Enumerations;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Creates an <see cref="EnumerationJsonConverter{T}"/> for any type deriving from <see cref="Enumeration"/>.
/// Add it to <see cref="JsonSerializerOptions.Converters"/> to round-trip Enumeration members by name.
/// </summary>
public sealed class EnumerationJsonConverterFactory : JsonConverterFactory
{
  /// <summary>True for <see cref="Enumeration"/> and every type deriving from it.</summary>
  public override bool CanConvert(Type typeToConvert) => typeof(Enumeration).IsAssignableFrom(typeToConvert);

  /// <summary>Creates the converter closed over <paramref name="typeToConvert"/>.</summary>
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
    (JsonConverter)Activator.CreateInstance(typeof(EnumerationJsonConverter<>).MakeGenericType(typeToConvert))!;
}
