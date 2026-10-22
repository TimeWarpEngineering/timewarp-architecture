#region Purpose
// Keeps a component root class when a caller also passes class through unmatched attributes.
#endregion

#region Design
// CaptureUnmatchedValues puts class in the splat dictionary. Applying that dictionary after
// the root class replaces the component class. Merge appends the caller class and drops class
// from the dictionary so the other unmatched attributes still splat.
#endregion

namespace TimeWarp.Architecture.Components;

internal static class SplattedClass
{
  internal static string Merge(string rootClass, IReadOnlyDictionary<string, object>? attributes)
  {
    string? extra = Value(attributes);
    return string.IsNullOrWhiteSpace(extra) ? rootClass : $"{rootClass} {extra}";
  }

  internal static IReadOnlyDictionary<string, object>? WithoutClass(
    IReadOnlyDictionary<string, object>? attributes)
  {
    if (attributes is null)
    {
      return null;
    }

    if (!attributes.ContainsKey("class"))
    {
      return attributes;
    }

    Dictionary<string, object> rest = new(attributes);
    rest.Remove("class");
    return rest;
  }

  private static string? Value(IReadOnlyDictionary<string, object>? attributes) =>
    attributes is not null && attributes.TryGetValue("class", out object? value)
      ? value?.ToString()
      : null;
}
