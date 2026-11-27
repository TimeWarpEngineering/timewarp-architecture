#region Purpose
// Reads a property's XML <summary> from its assembly's compiler documentation file, falling back
// to the same-named property on an interface the declaring type implements.
#endregion

#region Design
// FastEndpoints.OpenApi copies a member's XML <summary> onto its schema property and its
// parameter, but it reads only the member id of the property's declaring type. A contract that
// implements an I*Details interface keeps the shared summary on the interface property, and a
// parameter OpenApiDeleteRequestBody moves to the query string is never read at all. This reader
// answers both. The file is <assembly>.xml beside the assembly (the build copies it there), the
// same place FastEndpoints reads. A missing or unreadable file is no documentation, never an
// error. Text is the summary's inner text with each <see cref> reduced to its last name segment
// and wrapped lines joined into one line. The cache is per assembly and lives for the process.
#endregion

namespace TimeWarp.Foundation;

using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;

/// <summary>Property XML summaries for OpenAPI parameter and schema descriptions.</summary>
internal static class OpenApiMemberDocumentation
{
  private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, string>> Summaries = new();

  /// <summary>
  /// The summary on <paramref name="property"/>'s own declaration, else on the same-named
  /// property of an interface its declaring type implements. Null when neither has one.
  /// </summary>
  internal static string? Summary(PropertyInfo property, bool includeOwn = true)
  {
    ArgumentNullException.ThrowIfNull(property);
    if (includeOwn && Own(property) is { } own)
    {
      return own;
    }

    foreach (Type contract in property.DeclaringType?.GetInterfaces() ?? [])
    {
      PropertyInfo? declared = contract.GetProperty(property.Name);
      if (declared is not null && Own(declared) is { } inherited)
      {
        return inherited;
      }
    }

    return null;
  }

  private static string? Own(PropertyInfo property)
  {
    Type? declaring = property.DeclaringType;
    if (declaring is null)
    {
      return null;
    }

    string memberId = "P:" + TypeId(declaring) + "." + property.Name;
    return Summaries.GetOrAdd(declaring.Assembly, Load).TryGetValue(memberId, out string? summary)
      ? summary
      : null;
  }

  private static string TypeId(Type type)
  {
    string name = type.FullName ?? type.Name;
    int generic = name.IndexOf('[', StringComparison.Ordinal);
    if (generic >= 0)
    {
      name = name[..generic];
    }

    return name.Replace('+', '.');
  }

  private static IReadOnlyDictionary<string, string> Load(Assembly assembly)
  {
    string location = assembly.Location;
    string? path = string.IsNullOrEmpty(location)
      ? Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".xml")
      : Path.ChangeExtension(location, ".xml");
    Dictionary<string, string> summaries = new(StringComparer.Ordinal);
    if (!File.Exists(path))
    {
      return summaries;
    }

    XDocument document;
    try
    {
      document = XDocument.Load(path);
    }
    catch (XmlException)
    {
      return summaries;
    }
    catch (IOException)
    {
      return summaries;
    }

    foreach (XElement member in document.Descendants("member"))
    {
      string? name = member.Attribute("name")?.Value;
      XElement? summary = member.Element("summary");
      if (string.IsNullOrEmpty(name) || summary is null)
      {
        continue;
      }

      string text = Normalize(Text(summary));
      if (text.Length > 0)
      {
        summaries[name] = text;
      }
    }

    return summaries;
  }

  private static string Text(XElement element)
  {
    System.Text.StringBuilder builder = new();
    foreach (XNode node in element.Nodes())
    {
      if (node is XText text)
      {
        builder.Append(text.Value);
      }
      else if (node is XElement child)
      {
        string? reference = child.Attribute("cref")?.Value ?? child.Attribute("name")?.Value;
        if (reference is not null && child.IsEmpty)
        {
          int dot = reference.AsSpan().LastIndexOf('.');
          builder.Append(dot >= 0 ? reference[(dot + 1)..] : reference);
        }
        else
        {
          builder.Append(Text(child));
        }
      }
    }

    return builder.ToString();
  }

  private static string Normalize(string value) =>
    string.Join
    (
      ' ',
      value.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries)
    );
}
