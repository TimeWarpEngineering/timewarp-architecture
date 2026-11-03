#region Purpose
// Pipeline models for PageSourceGenerator: the per-page PageModel, its location-free
// PageDiagnostic payload, and the PageParsedRoute a route template renders to.
#endregion

#region Design
// Value-equatable (incremental caching): arrays compare by sequence, and diagnostics are
// location-free PageDiagnostic values — a Diagnostic/Location would pin old SyntaxTrees in the
// cache and never compare equal, so every edit would re-run every step and PageRegistry.
// Errors are fail-closed (TWE005/010/011): reported, and the page surface is not emitted.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;

/// <summary>One [Page] class as the generator sees it: route surface, policy, registry metadata, and errors.</summary>
internal sealed record PageModel(
  string Namespace,
  string ClassName,
  string HintName,
  string RouteAttribute,
  ImmutableArray<string> AdditionalRouteAttributes,
  string Signature,
  string Format,
  ImmutableArray<(string Type, string Name)> Parameters,
  string PolicyExpression,
  ImmutableArray<PageDiagnostic> Errors,
  string RouteTemplate,
  bool Navigable, // valid opt-in only (static primary route): INavigationDestination + registry membership
  PageDiagnostic? NavigableDiagnostic,
  string Description,
  ImmutableArray<string> RouteTemplates,
  string NavGroup,
  int NavOrder,
  bool NavPinBottom,
  ImmutableArray<string> AgentActions)
{
  public bool Equals(PageModel? other) =>
    other is not null
    && Namespace == other.Namespace
    && ClassName == other.ClassName
    && HintName == other.HintName
    && RouteAttribute == other.RouteAttribute
    && AdditionalRouteAttributes.SequenceEqual(other.AdditionalRouteAttributes)
    && Signature == other.Signature
    && Format == other.Format
    && Parameters.SequenceEqual(other.Parameters)
    && PolicyExpression == other.PolicyExpression
    && Errors.SequenceEqual(other.Errors)
    && RouteTemplate == other.RouteTemplate
    && Navigable == other.Navigable
    && Equals(NavigableDiagnostic, other.NavigableDiagnostic)
    && Description == other.Description
    && RouteTemplates.SequenceEqual(other.RouteTemplates)
    && NavGroup == other.NavGroup
    && NavOrder == other.NavOrder
    && NavPinBottom == other.NavPinBottom
    && AgentActions.SequenceEqual(other.AgentActions);

  public override int GetHashCode()
  {
    unchecked
    {
      int hash = (StringComparer.Ordinal.GetHashCode(HintName) * 397) ^ StringComparer.Ordinal.GetHashCode(RouteAttribute);
      hash = (hash * 397) ^ AdditionalRouteAttributes.Length;
      hash = (hash * 397) ^ Parameters.Length;
      hash = (hash * 397) ^ Errors.Length;
      hash = (hash * 397) ^ Navigable.GetHashCode();
      hash = (hash * 397) ^ NavOrder;
      hash = (hash * 397) ^ NavPinBottom.GetHashCode();
      hash = (hash * 397) ^ AgentActions.Length;
      return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Description);
    }
  }

  public static PageModel Failed(ClassDeclarationSyntax cls, string route, IEnumerable<PageDiagnostic> errors) =>
    new(
      Namespace: cls.Identifier.Text,
      ClassName: cls.Identifier.Text,
      HintName: cls.Identifier.Text,
      RouteAttribute: route,
      AdditionalRouteAttributes: [],
      Signature: "",
      Format: "",
      Parameters: [],
      PolicyExpression: "",
      Errors: [.. errors],
      RouteTemplate: route,
      Navigable: false,
      NavigableDiagnostic: null,
      Description: "",
      RouteTemplates: [],
      NavGroup: "",
      NavOrder: 0,
      NavPinBottom: false,
      AgentActions: []);
}

/// <summary>Location-free diagnostic payload; the Diagnostic is built in RegisterSourceOutput.</summary>
internal sealed record PageDiagnostic(
  DiagnosticDescriptor Descriptor,
  string FilePath,
  TextSpan Span,
  LinePositionSpan LineSpan,
  string Arg0 = "",
  string Arg1 = "")
{
  public static PageDiagnostic From(DiagnosticDescriptor descriptor, SyntaxNode node, string arg0 = "", string arg1 = "")
  {
    FileLinePositionSpan line = node.GetLocation().GetLineSpan();
    return new PageDiagnostic(descriptor, line.Path, node.Span, line.Span, arg0, arg1);
  }

  public Diagnostic ToDiagnostic() =>
    Diagnostic.Create(Descriptor, Location.Create(FilePath, Span, LineSpan), Arg0, Arg1);
}

/// <summary>One route template rendered for emission.</summary>
internal sealed class PageParsedRoute
{
  public string RouteAttribute { get; set; } = "";
  public string Format { get; set; } = "";
  public string ShapeKey { get; set; } = ""; // case-insensitive, token-name-free: what Blazor treats as "the same route"
  public List<(string Type, string Name)> Parameters { get; } = [];
}
