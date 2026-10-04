#region Purpose
// System.Text.Json converter that round-trips one Enumeration subclass by member Name, failing closed.
#endregion

#region Design
// Name, not Value, on the wire: the contract seam already writes plain C# enums as member-name
// strings (ContractSerializationDefaults) because names are self-describing for agent consumers;
// Enumeration members follow the same rule. Read is ordinal (case-sensitive) via TryFromName — an
// unknown name, a number, or any other non-string token throws JsonException rather than
// producing null or a default member (the same silent-failure class the read side must never
// reintroduce). JSON null is handled by System.Text.Json itself (HandleNull stays false), so a
// nullable Enumeration property round-trips null. Property-name overrides make an Enumeration
// usable as a dictionary key.
#endregion

namespace TimeWarp.Foundation.Enumerations;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes a <typeparamref name="T"/> member as its <see cref="Enumeration.Name"/> string and
/// reads it back with <see cref="Enumeration.TryFromName{T}"/>; unknown names and non-string tokens
/// throw <see cref="JsonException"/>.
/// </summary>
/// <typeparam name="T">The Enumeration subclass whose members are converted.</typeparam>
public sealed class EnumerationJsonConverter<T> : JsonConverter<T> where T : Enumeration
{
  /// <summary>Reads a member name string and resolves it to the <typeparamref name="T"/> member.</summary>
  /// <exception cref="JsonException">The token is not a string, or names no member of <typeparamref name="T"/>.</exception>
  public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.String)
    {
      throw new JsonException($"Expected a {typeof(T).Name} member name string but found {reader.TokenType}.");
    }

    return Resolve(reader.GetString());
  }

  /// <summary>Writes the member's <see cref="Enumeration.Name"/> as a JSON string.</summary>
  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
    writer.WriteStringValue(value.Name);

  /// <summary>Reads a dictionary key as a member name.</summary>
  /// <exception cref="JsonException">The key names no member of <typeparamref name="T"/>.</exception>
  public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    Resolve(reader.GetString());

  /// <summary>Writes the member's <see cref="Enumeration.Name"/> as a dictionary key.</summary>
  public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
    writer.WritePropertyName(value.Name);

  private static T Resolve(string? name)
  {
    if (name is not null && Enumeration.TryFromName(name, out T? member)) return member;

    throw new JsonException($"'{name}' is not a valid name in {typeof(T)}.");
  }
}
