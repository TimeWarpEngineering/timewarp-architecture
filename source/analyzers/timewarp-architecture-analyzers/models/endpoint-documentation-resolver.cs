#region Purpose
// Resolves the OpenAPI summary and description for one [ApiEndpoint] contract from its
// [Operation] description and its XML docs.
#endregion

#region Design
// Summary resolution order (task 458-005, which flipped task 472's order). The first non-empty
// source wins:
//   1. [Operation(Description = ...)] on the nested Query/Command (the caller passes the value
//      it read). TWA0035 requires it on every hosted contract.
//   2. XML <summary> on that request.
//   3. XML <summary> on the outer [ApiEndpoint] static class.
//   4. Empty. The emitter then writes no s.Summary line.
// The description is the longer text: the request XML <summary> (else the outer class
// <summary>) followed by the request <remarks> (else the outer class <remarks>). A part is
// left out when it is the text already used as the summary, so OpenAPI never repeats the
// summary in the description, and the [Operation] description is never written there.
// Each XML source reads the symbol's own documentation comment first. When that is empty, it
// reads the compiler .xml AdditionalFile (ContractDocumentation). csc attaches no documentation
// provider to metadata references, so the file is the source on a command-line build.
// Text is the element's inner text with wrapped lines joined into one line, the same as
// ContractDocumentation. The emitter puts it in a C# string literal, so a line break would not
// compile.
#endregion

namespace TimeWarp.Architecture.Analyzers.Models;

using System.Collections.Generic;
using System.Xml.Linq;

internal static class EndpointDocumentationResolver
{
  private static readonly ContractMemberDocumentation None = new(string.Empty, string.Empty);

  /// <summary>Returns the endpoint summary and description in the order the Design region states.</summary>
  public static (string Summary, string Description) Resolve(
    INamedTypeSymbol outerClass,
    INamedTypeSymbol requestClass,
    string? operationDescription,
    ContractDocumentation? documentation)
  {
    ContractMemberDocumentation request = Read(requestClass, documentation);
    ContractMemberDocumentation outer = Read(outerClass, documentation);
    string operation = ContractDocumentation.Normalize(operationDescription);
    string xmlSummary = request.Summary.Length > 0 ? request.Summary : outer.Summary;
    string remarks = request.Remarks.Length > 0 ? request.Remarks : outer.Remarks;

    string summary = operation.Length > 0 ? operation : xmlSummary;
    List<string> parts = [];
    if (xmlSummary.Length > 0 && !SameText(xmlSummary, summary))
    {
      parts.Add(xmlSummary);
    }

    if (remarks.Length > 0 && !SameText(remarks, summary))
    {
      parts.Add(remarks);
    }

    return (summary, string.Join(" ", parts));
  }

  /// <summary>Same sentence, ignoring case and a trailing period.</summary>
  private static bool SameText(string left, string right) =>
    string.Equals(left.TrimEnd('.'), right.TrimEnd('.'), StringComparison.OrdinalIgnoreCase);

  private static ContractMemberDocumentation Read(INamedTypeSymbol type, ContractDocumentation? documentation)
  {
    ContractMemberDocumentation fromSymbol = ReadComment(type.GetDocumentationCommentXml());
    if (fromSymbol.Summary.Length > 0 || fromSymbol.Remarks.Length > 0 || documentation is null)
    {
      return fromSymbol;
    }

    string? documentationId = DocumentationCommentId.CreateDeclarationId(type);
    return documentationId is not null
      && documentation.TryGetMember(documentationId, out ContractMemberDocumentation member)
      ? member
      : fromSymbol;
  }

  private static ContractMemberDocumentation ReadComment(string? xml)
  {
    if (string.IsNullOrWhiteSpace(xml))
    {
      return None;
    }

    XElement member;
    try
    {
      member = XElement.Parse(xml);
    }
    catch (System.Xml.XmlException)
    {
      return None;
    }

    return new ContractMemberDocumentation(
      ContractDocumentation.Normalize(member.Element("summary")?.Value),
      ContractDocumentation.Normalize(member.Element("remarks")?.Value));
  }
}
