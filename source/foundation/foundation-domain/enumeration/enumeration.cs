#region Purpose
// Enumeration-class base (Bogard pattern) so "enum" members can carry behavior and data.
#endregion

#region Design
// Chosen over C# enums so each member can be a full object (see CorsPolicy, which overrides
// Apply per member) and can map external-system identifiers via AlternateCodes.
//
// Member discovery: GetAll reflects over the public static fields declared on the subclass —
// members MUST be declared as public static readonly fields or lookups silently miss them.
// TWA0028 (EnumerationMemberShapeAnalyzer) makes that a build error instead of a memory rule.
//
// Cache: the member set is reflected once per subclass into MemberCache<T> (a generic static
// holder — the CLR gives each closed T its own slot, no dictionary lookup) together with
// Value / Name / alternate-code indexes, so Parse/From*/TryFrom* are dictionary hits. A snapshot
// taken while the subclass's static initializer is still running (a member field still null) is
// returned but NOT cached, so an early GetAll call from inside the subclass cannot freeze a
// partial member set.
//
// Ambiguity fails closed: building the snapshot throws InvalidOperationException when two
// members share a Value, a Name, or an alternate code, or when one member's Name equals another
// member's alternate code. FromString therefore never depends on declaration order — a malformed
// subclass is rejected on first lookup instead of resolving to whichever member came first.
//
// Equality is by Value + exact runtime type (IEquatable plus ==/!=, so `a == b` is semantic, not
// reference, equality). Comparison is by Value only (IComparable and IComparable<Enumeration>,
// plus the relational operators); null sorts before any member. Parse/From* throw rather than
// returning null so callers cannot ignore an unknown code; TryFrom* are the non-throwing forms.
//
// JSON: EnumerationJsonConverterFactory writes the Name and reads it back via TryFromName,
// failing closed (JsonException) on an unknown name or a non-string token. The converter is NOT
// registered on the contract seam by default (contracts use plain enums and carry no domain
// dependency; see the trigger in ContractSerializationDefaults). Apps register it themselves on
// their own JsonSerializerOptions where needed.
#endregion

namespace TimeWarp.Foundation.Enumerations;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A base class for creating Enumerations — enum-like types whose members are objects that can
/// carry data and behavior. Members are declared as <c>public static readonly</c> fields on the subclass.
/// https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
/// </summary>
public abstract class Enumeration : IComparable, IComparable<Enumeration>, IEquatable<Enumeration>
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
  /// Get the member of <typeparamref name="T"/> that lists <paramref name="alternateCode"/> in its <see cref="AlternateCodes"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">No member has that alternate code, or <typeparamref name="T"/> is malformed.</exception>
  public static T FromAlternateCode<T>(string alternateCode) where T : Enumeration =>
    Parse(MemberCache<T>.Get().ByAlternateCode, alternateCode, "alternate code");

  /// <summary>
  /// Get the member of <typeparamref name="T"/> whose <see cref="Name"/> is <paramref name="name"/> (ordinal, case-sensitive).
  /// </summary>
  /// <exception cref="InvalidOperationException">No member has that name, or <typeparamref name="T"/> is malformed.</exception>
  public static T FromName<T>(string name) where T : Enumeration =>
    Parse(MemberCache<T>.Get().ByName, name, "name");

  /// <summary>
  /// Get the member of <typeparamref name="T"/> whose <see cref="Name"/> or one of whose <see cref="AlternateCodes"/> is <paramref name="value"/>.
  /// </summary>
  /// <param name="value">The string value to search for</param>
  /// <exception cref="InvalidOperationException">No member matches, or <typeparamref name="T"/> is malformed.</exception>
  public static T FromString<T>(string value) where T : Enumeration =>
    Parse(MemberCache<T>.Get().ByString, value, "name or alternate code");

  /// <summary>
  /// Get the member of <typeparamref name="T"/> whose <see cref="Value"/> is <paramref name="value"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">No member has that value, or <typeparamref name="T"/> is malformed.</exception>
  public static T FromValue<T>(int value) where T : Enumeration =>
    Parse(MemberCache<T>.Get().ByValue, value, "value");

  /// <summary>
  /// Non-throwing <see cref="FromName{T}"/>: true and the member when <paramref name="name"/> matches.
  /// </summary>
  /// <exception cref="InvalidOperationException"><typeparamref name="T"/> is malformed (duplicate value, name, or code).</exception>
  public static bool TryFromName<T>(string name, [NotNullWhen(true)] out T? result) where T : Enumeration =>
    MemberCache<T>.Get().ByName.TryGetValue(name, out result);

  /// <summary>
  /// Non-throwing <see cref="FromString{T}"/>: true and the member when <paramref name="value"/> is a name or alternate code.
  /// </summary>
  /// <exception cref="InvalidOperationException"><typeparamref name="T"/> is malformed (duplicate value, name, or code).</exception>
  public static bool TryFromString<T>(string value, [NotNullWhen(true)] out T? result) where T : Enumeration =>
    MemberCache<T>.Get().ByString.TryGetValue(value, out result);

  /// <summary>
  /// Non-throwing <see cref="FromValue{T}"/>: true and the member when <paramref name="value"/> matches.
  /// </summary>
  /// <exception cref="InvalidOperationException"><typeparamref name="T"/> is malformed (duplicate value, name, or code).</exception>
  public static bool TryFromValue<T>(int value, [NotNullWhen(true)] out T? result) where T : Enumeration =>
    MemberCache<T>.Get().ByValue.TryGetValue(value, out result);

  /// <summary>
  /// Every member of <typeparamref name="T"/> (its public static readonly fields), in declaration order.
  /// </summary>
  /// <exception cref="InvalidOperationException"><typeparamref name="T"/> is malformed (duplicate value, name, or code).</exception>
  public static IEnumerable<T> GetAll<T>() where T : Enumeration => MemberCache<T>.Get().All;

  /// <summary>
  /// Compares members by <see cref="Value"/>; null sorts before any member.
  /// </summary>
  /// <exception cref="ArgumentException"><paramref name="obj"/> is not an <see cref="Enumeration"/>.</exception>
  public int CompareTo(object? obj)
  {
    if (obj is null) return 1;

    if (obj is not Enumeration other)
    {
      throw new ArgumentException($"Object must be of type {nameof(Enumeration)}.", nameof(obj));
    }

    return CompareTo(other);
  }

  /// <summary>
  /// Compares members by <see cref="Value"/>; null sorts before any member.
  /// </summary>
  public int CompareTo(Enumeration? other) => other is null ? 1 : Value.CompareTo(other.Value);

  /// <summary>
  /// True when <paramref name="other"/> is the same runtime type and <see cref="Value"/>.
  /// </summary>
  public bool Equals(Enumeration? other) =>
    other is not null && GetType() == other.GetType() && Value == other.Value;

  /// <summary>
  /// True when <paramref name="obj"/> is the same runtime type and <see cref="Value"/>.
  /// </summary>
  public override bool Equals(object? obj) => Equals(obj as Enumeration);

  /// <summary>
  /// Hash code derived from <see cref="Value"/>.
  /// </summary>
  public override int GetHashCode() => Value.GetHashCode();

  /// <summary>
  /// Returns the member's <see cref="Name"/>.
  /// </summary>
  public override string ToString() => Name;

  /// <summary>Semantic equality: same runtime type and <see cref="Value"/> (both null is equal).</summary>
  public static bool operator ==(Enumeration? left, Enumeration? right) =>
    left is null ? right is null : left.Equals(right);

  /// <summary>Semantic inequality: the negation of <see cref="op_Equality"/>.</summary>
  public static bool operator !=(Enumeration? left, Enumeration? right) => !(left == right);

  /// <summary>True when <paramref name="left"/> sorts before <paramref name="right"/> by <see cref="Value"/> (null first).</summary>
  public static bool operator <(Enumeration? left, Enumeration? right) => Compare(left, right) < 0;

  /// <summary>True when <paramref name="left"/> sorts before or with <paramref name="right"/> by <see cref="Value"/> (null first).</summary>
  public static bool operator <=(Enumeration? left, Enumeration? right) => Compare(left, right) <= 0;

  /// <summary>True when <paramref name="left"/> sorts after <paramref name="right"/> by <see cref="Value"/> (null first).</summary>
  public static bool operator >(Enumeration? left, Enumeration? right) => Compare(left, right) > 0;

  /// <summary>True when <paramref name="left"/> sorts after or with <paramref name="right"/> by <see cref="Value"/> (null first).</summary>
  public static bool operator >=(Enumeration? left, Enumeration? right) => Compare(left, right) >= 0;

  private static int Compare(Enumeration? left, Enumeration? right) =>
    left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

  private static T Parse<TKey, T>(Dictionary<TKey, T> index, TKey key, string description)
    where TKey : notnull
    where T : Enumeration
  {
    if (index.TryGetValue(key, out T? member)) return member;

    throw new InvalidOperationException($"'{key}' is not a valid {description} in {typeof(T)}");
  }

  /// <summary>One subclass's members and lookup indexes, built and validated once.</summary>
  private sealed class Members<T> where T : Enumeration
  {
    public Members(T[] all)
    {
      All = new ReadOnlyCollection<T>(all);

      foreach (T member in all)
      {
        if (!ByValue.TryAdd(member.Value, member))
        {
          throw Ambiguous($"value {member.Value} is declared by both '{ByValue[member.Value].Name}' and '{member.Name}'");
        }

        if (!ByName.TryAdd(member.Name, member))
        {
          throw Ambiguous($"name '{member.Name}' is declared twice");
        }

        foreach (string code in member.AlternateCodes)
        {
          if (ByAlternateCode.TryGetValue(code, out T? owner))
          {
            if (ReferenceEquals(owner, member)) continue;
            throw Ambiguous($"alternate code '{code}' is declared by both '{owner.Name}' and '{member.Name}'");
          }

          ByAlternateCode.Add(code, member);
        }
      }

      foreach (KeyValuePair<string, T> entry in ByName)
      {
        ByString.Add(entry.Key, entry.Value);
      }

      foreach (KeyValuePair<string, T> entry in ByAlternateCode)
      {
        if (ByString.TryGetValue(entry.Key, out T? named) && !ReferenceEquals(named, entry.Value))
        {
          throw Ambiguous($"'{entry.Key}' is the name of '{named.Name}' and an alternate code of '{entry.Value.Name}'");
        }

        ByString.TryAdd(entry.Key, entry.Value);
      }
    }

    public ReadOnlyCollection<T> All { get; }
    public Dictionary<string, T> ByAlternateCode { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, T> ByName { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, T> ByString { get; } = new(StringComparer.Ordinal);
    public Dictionary<int, T> ByValue { get; } = [];

    private static InvalidOperationException Ambiguous(string detail) =>
      new($"Enumeration {typeof(T)} is ambiguous: {detail}.");
  }

  /// <summary>Per-subclass cache of <see cref="Members{T}"/>; see the Design region for the null-field rule.</summary>
  private static class MemberCache<T> where T : Enumeration
  {
    private static volatile Members<T>? Cached;

    public static Members<T> Get()
    {
      Members<T>? cached = Cached;
      if (cached is not null) return cached;

      FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
      List<T> members = new(fields.Length);
      bool complete = true;

      foreach (FieldInfo field in fields)
      {
        object? value = field.GetValue(null);
        if (value is T member)
        {
          members.Add(member);
        }
        else if (value is null && typeof(T).IsAssignableFrom(field.FieldType))
        {
          complete = false;
        }
      }

      Members<T> built = new([.. members]);
      if (complete) Cached = built;

      return built;
    }
  }
}
