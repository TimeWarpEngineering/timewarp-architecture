#region Purpose
// Enumeration-class base (Bogard pattern) so "enum" members can carry behavior and data.
#endregion

#region Design
// Chosen over C# enums so each member can be a full object (see CorsPolicy, which overrides
// Apply per member) and can map external-system identifiers via AlternateCodes.
// GetAll discovers members by reflecting over public static fields declared on the subclass —
// members MUST be declared as public static readonly fields or lookups silently miss them.
// Equality is by Value + exact type; Parse throws rather than returning null so callers cannot
// ignore an unknown code.
#endregion

namespace TimeWarp.Foundation.Enumerations;

/// <summary>
/// a base class for creating Enumerations.
/// https://gist.github.com/slovely/1076365
/// https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
/// </summary>
public abstract class Enumeration : IComparable
{
  /// <summary>
  /// Initializes an enumeration member with its numeric value, display name, and optional alternate codes.
  /// </summary>
  protected Enumeration(int value, string name, IReadOnlyList<string>? alternateCodes)
  {
    Value = value;
    Name = name;
    AlternateCodes = alternateCodes ?? [];
  }

  /// <summary>
  /// External-system identifiers that resolve to this member via <see cref="FromAlternateCode{T}"/> or <see cref="FromString{T}"/>.
  /// </summary>
  public IReadOnlyList<string> AlternateCodes { get; }
  /// <summary>
  /// Canonical display name used by <see cref="FromName{T}"/> and returned by <see cref="ToString"/>.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Stable numeric identity used for equality, comparison, and <see cref="FromValue{T}"/> lookup.
  /// </summary>
  public int Value { get; }

  /// <summary>
  /// Get the EnumerationItem from an alternate code.
  /// </summary>
  public static T FromAlternateCode<T>(string alternateCode) where T : Enumeration =>
    Parse<T>
    (
      alternateCode,
      "alternate code",
      item => item.AlternateCodes.Contains(alternateCode)
    );

  /// <summary>
  /// Get the EnumerationItem from its Name
  /// </summary>
  public static T FromName<T>(string name) where T : Enumeration =>
    Parse<T>(name, "name", item => item.Name == name);

  /// <summary>
  /// Get the EnumerationItem from a display name or alternate code.
  /// </summary>
  /// <param name="value">The string value to search for</param>
  public static T FromString<T>(string value) where T : Enumeration =>
    Parse<T>
    (
      value, "name or alternate code", item =>
      item.Name == value ||
      item.AlternateCodes.Contains(value)
    );

  /// <summary>
  /// Get the EnumerationItem from its value
  /// </summary>
  public static T FromValue<T>(int value) where T : Enumeration =>
    Parse<T>(value, "value", item => item.Value == value);

  /// <summary>
  /// Enumerates every public static readonly field declared on <typeparamref name="T"/>.
  /// </summary>
  public static IEnumerable<T> GetAll<T>() where T : Enumeration
  {
    Type type = typeof(T);
    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

    return fields.Select(info => info.GetValue(null)).OfType<T>();
  }

  /// <summary>
  /// Compares members by <see cref="Value"/>; null sorts before any member.
  /// </summary>
  public int CompareTo(object? other)
  {
    if (other is null) return 1;

    if (other is not Enumeration otherEnumeration)
    {
      throw new ArgumentException($"Object must be of type {nameof(Enumeration)}.", nameof(other));
    }

    return Value.CompareTo(otherEnumeration.Value);
  }

  /// <summary>
  /// True when <paramref name="value"/> is the same runtime type and <see cref="Value"/>.
  /// </summary>
  public override bool Equals(object? value)
  {
    if (value is not Enumeration otherValue) return false;

    bool typeMatches = GetType().Equals(value.GetType());
    bool valueMatches = Value.Equals(otherValue.Value);

    return typeMatches && valueMatches;
  }

  /// <summary>
  /// Hash code derived from <see cref="Value"/>.
  /// </summary>
  public override int GetHashCode() => Value.GetHashCode();

  /// <summary>
  /// Returns the member's <see cref="Name"/>.
  /// </summary>
  public override string ToString() => Name;

  private static T Parse<T>(object value, string description, Func<T, bool> predicate) where T : Enumeration
  {
    T? matchingItem = GetAll<T>().FirstOrDefault(predicate);

    if (matchingItem is null)
    {
      string message = $"'{value}' is not a valid {description} in {typeof(T)}";
      throw new InvalidOperationException(message);
    }

    return matchingItem;
  }
}
