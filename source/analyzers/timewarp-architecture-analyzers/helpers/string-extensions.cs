#region Purpose
// Case-convention conversions shared by the analyzers and generator.
#endregion

#region Design
// ToKebabCase must mirror the repo's file-naming convention exactly — the partial-class file-name
// analyzer derives expected file names from it, so a drift here changes what the analyzer accepts.
#endregion

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
