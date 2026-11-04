#region Purpose
// Resolves the OpenAPI summary and remarks for one [ApiEndpoint] contract from its XML docs
// and its [Operation] description.
#endregion

#region Design
// Summary resolution order (task 472). The first non-empty source wins:
//   1. XML <summary> on the nested Query/Command.
//   2. [Operation(Description = ...)] on that request (the caller passes the value it read).
//   3. XML <summary> on the outer [ApiEndpoint] static class.
//   4. Empty. The emitter then writes no s.Summary line.
// Remarks come from the request XML <remarks>, else from the outer class <remarks>. The
// [Operation] description is not used as remarks, so it is not written to OpenAPI twice.
// Each XML source reads the symbol's own documentation comment first. When that is empty, it
// reads the compiler .xml AdditionalFile (ContractDocumentation). csc attaches no documentation
// provider to metadata references, so the file is the source on a command-line build.
// Text is the element's inner text with wrapped lines joined into one line, the same as
// ContractDocumentation. The emitter puts it in a C# string literal, so a line break would not
// compile.
#endregion

namespace TimeWarp.Architecture.Analyzers.Models;

using System.Xml.Linq;

internal static class EndpointDocumentationResolver
{
  private static readonly ContractMemberDocumentation None = new(string.Empty, string.Empty);

  /// <summary>Returns the endpoint summary and remarks in the order the Design region states.</summary>
  public static (string Summary, string Remarks) Resolve(
    INamedTypeSymbol outerClass,
    INamedTypeSymbol requestClass,
    string? operationDescription,
    ContractDocumentation? documentation)
  {
    ContractMemberDocumentation request = Read(requestClass, documentation);
    ContractMemberDocumentation outer = Read(outerClass, documentation);
    string operation = ContractDocumentation.Normalize(operationDescription);

    string summary = request.Summary.Length > 0
      ? request.Summary
      : operation.Length > 0
        ? operation
        : outer.Summary;
    string remarks = request.Remarks.Length > 0 ? request.Remarks : outer.Remarks;
    return (summary, remarks);
  }

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
