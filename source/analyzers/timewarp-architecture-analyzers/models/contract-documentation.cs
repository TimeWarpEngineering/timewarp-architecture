#region Purpose
// Reads summary and remarks from a compiler XML documentation file.
#endregion

#region Design
// csc does not attach a documentation provider to metadata references, so
// GetDocumentationCommentXml is empty for contract types during a command-line build.
// The host passes the contract assembly's generated .xml as an AdditionalFile, and this
// index answers the documentation id the symbol would have had. A file that is not a
// compiler documentation document is skipped. Wrapped lines become one line because the
// endpoint generator emits the text inside a C# string literal.
#endregion

namespace TimeWarp.Architecture.Analyzers.Models;

using System.Xml.Linq;

internal readonly record struct ContractMemberDocumentation(string Summary, string Remarks);

internal sealed class ContractDocumentation
{
  public static ContractDocumentation Empty { get; } = new(ImmutableDictionary<string, ContractMemberDocumentation>.Empty);

  private readonly ImmutableDictionary<string, ContractMemberDocumentation> Members;

  private ContractDocumentation(ImmutableDictionary<string, ContractMemberDocumentation> members)
  {
    Members = members;
  }

  public bool TryGetMember(string documentationId, out ContractMemberDocumentation documentation)
    => Members.TryGetValue(documentationId, out documentation);

  public static ContractDocumentation FromXml(IEnumerable<string?> documents)
  {
    ImmutableDictionary<string, ContractMemberDocumentation>.Builder builder =
      ImmutableDictionary.CreateBuilder<string, ContractMemberDocumentation>(StringComparer.Ordinal);

    foreach (string? document in documents)
    {
      if (string.IsNullOrWhiteSpace(document))
      {
        continue;
      }

      ReadDocument(document, builder);
    }

    return builder.Count == 0
      ? Empty
      : new ContractDocumentation(builder.ToImmutable());
  }

  private static void ReadDocument(
    string document,
    ImmutableDictionary<string, ContractMemberDocumentation>.Builder builder)
  {
    XDocument parsed;
    try
    {
      parsed = XDocument.Parse(document);
    }
    catch (System.Xml.XmlException)
    {
      return;
    }

    XElement? members = parsed.Root?.Element("members");
    if (members is null)
    {
      return;
    }

    foreach (XElement member in members.Elements("member"))
    {
      string? name = member.Attribute("name")?.Value;
      if (string.IsNullOrEmpty(name))
      {
        continue;
      }

      string summary = Normalize(member.Element("summary")?.Value);
      string remarks = Normalize(member.Element("remarks")?.Value);
      if (summary.Length == 0 && remarks.Length == 0)
      {
        continue;
      }

      builder[name] = new ContractMemberDocumentation(summary, remarks);
    }
  }

  private static string Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return string.Empty;
    }

    string[] lines = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    return string.Join(' ', lines.Select(static line => line.Trim()));
  }
}
