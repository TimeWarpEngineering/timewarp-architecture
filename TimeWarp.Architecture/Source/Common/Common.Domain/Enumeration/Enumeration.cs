namespace TimeWarp.Architecture.Enumerations;

/// <summary>
/// a base class for creating Enumerations.
/// https://gist.github.com/slovely/1076365
/// https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
/// </summary>
public abstract class Enumeration : IComparable
{
  //protected Enumeration() { }

  protected Enumeration(int value, string name, List<string>? alternateCodes)
  {
    Value = value;
    Name = name;
    AlternateCodes = alternateCodes ?? [];
  }

  public List<string> AlternateCodes { get; }
  public string Name { get; }

  public int Value { get; }

  /// <summary>
  /// Get the EnumerationItem form an alternate code.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="alternateCode"></param>
  /// <returns></returns>
  public static T? FromAlternateCode<T>(string alternateCode) where T : Enumeration
  {
    T? matchingItem =
      Parse<T, string>
      (
        alternateCode,
        "alternate code",
        item => item.AlternateCodes.Contains(alternateCode)
      );

    return matchingItem;
  }

  /// <summary>
  /// Get the EnumerationItem from  its Name
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="name"></param>
  /// <returns></returns>
  public static T? FromName<T>(string name) where T : Enumeration
  {
    T? matchingItem = Parse<T, string>(name, "name", item => item.Name == name);
    return matchingItem;
  }

  /// <summary>
  /// Get the EnumerationItem from a display name, alternate code or value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  public static T? FromString<T>(string aString) where T : Enumeration
  {
    T? matchingItem =
      Parse<T, string>
      (
        aString, "", item =>
        item.Name == aString ||
        item.AlternateCodes.Contains(aString)
      );

    return matchingItem;
  }

  /// <summary>
  /// Get the EnumerationItem from is value
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="value"></param>
  /// <returns></returns>
  public static T? FromValue<T>(int value) where T : Enumeration
  {
    T? matchingItem = Parse<T, int>(value, "value", item => item.Value == value);
    return matchingItem;
  }

  public static IEnumerable<T> GetAll<T>() where T : Enumeration
  {
    Type type = typeof(T);
    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

    return fields.Select(info => info.GetValue(null)).OfType<T>();
  }

  public int CompareTo(object? other) => Value.CompareTo(((Enumeration?)other)?.Value);

  public override bool Equals(object? value)
  {
    if (value is not Enumeration otherValue) return false;

    bool typeMatches = GetType().Equals(value?.GetType());
    bool valueMatches = Value.Equals(otherValue.Value);

    return typeMatches && valueMatches;
  }

  public override int GetHashCode() => Value.GetHashCode();

  public override string ToString() => Name;

  protected static T? Parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration
  {
    T? matchingItem = GetAll<T>().FirstOrDefault(predicate);

    if (matchingItem is null)
    {
      string message = $"'{value}' is not a valid {description} in {typeof(T)}";
      throw new Exception(message);
    }

    return matchingItem;
  }
}
