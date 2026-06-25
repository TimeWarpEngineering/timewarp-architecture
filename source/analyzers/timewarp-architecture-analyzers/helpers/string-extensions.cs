namespace TimeWarp.Architecture.Analyzers;

using System.Globalization;

public static class StringExtensions
{
  public static string ToKebabCase(this string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    return string.Concat(value.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + char.ToLowerInvariant(x).ToString(CultureInfo.InvariantCulture) : x.ToString(CultureInfo.InvariantCulture)));
  }

#pragma warning disable CA1308 // Normalize strings to uppercase - ToCamelCase intentionally lowercases
  public static string ToCamelCase(this string str)
  {
    if (!string.IsNullOrEmpty(str) && str.Length > 1)
    {
      return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    return str.ToLowerInvariant();
  }
#pragma warning restore CA1308 // Normalize strings to uppercase
}
