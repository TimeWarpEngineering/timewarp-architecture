namespace TimeWarp.Architecture.Enumerations;

/// <summary>
/// a base class for creating Enumerations.
/// https://gist.github.com/slovely/1076365
/// https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
/// </summary>
public abstract class Enumeration : IComparable
{
  protected Enumeration(int value, string name, IReadOnlyList<string>? alternateCodes)
  {
    Value = value;
    Name = name;
    AlternateCodes = alternateCodes ?? [];
  }

  public IReadOnlyList<string> AlternateCodes { get; }
  public string Name { get; }

  public int Value { get; }

  /// <summary>
  /// Get the EnumerationItem from an alternate code.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="alternateCode"></param>
  /// <returns></returns>
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
  /// <typeparam name="T"></typeparam>
  /// <param name="name"></param>
  /// <returns></returns>
  public static T FromName<T>(string name) where T : Enumeration =>
    Parse<T>(name, "name", item => item.Name == name);

  /// <summary>
  /// Get the EnumerationItem from a display name or alternate code.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="value">The string value to search for</param>
  /// <returns></returns>
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
  /// <typeparam name="T"></typeparam>
  /// <param name="value"></param>
  /// <returns></returns>
  public static T FromValue<T>(int value) where T : Enumeration =>
    Parse<T>(value, "value", item => item.Value == value);

  public static IEnumerable<T> GetAll<T>() where T : Enumeration
  {
    Type type = typeof(T);
    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

    return fields.Select(info => info.GetValue(null)).OfType<T>();
  }

  public int CompareTo(object? other)
  {
    if (other is null) return 1;

    if (other is not Enumeration otherEnumeration)
    {
      throw new ArgumentException($"Object must be of type {nameof(Enumeration)}.", nameof(other));
    }

    return Value.CompareTo(otherEnumeration.Value);
  }

  public override bool Equals(object? value)
  {
    if (value is not Enumeration otherValue) return false;

    bool typeMatches = GetType().Equals(value.GetType());
    bool valueMatches = Value.Equals(otherValue.Value);

    return typeMatches && valueMatches;
  }

  public override int GetHashCode() => Value.GetHashCode();

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
