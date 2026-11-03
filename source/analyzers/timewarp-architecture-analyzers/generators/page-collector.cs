#region Purpose
// Collector for PageSourceGenerator: reads one [Page] class declaration (syntax plus semantic
// model) into a location-free PageModel, including TWE005/009/010/011 judgments.
#endregion

#region Design
// Generation keys off attribute syntax, not the attribute's compile-time values (see the
// PageSourceGenerator Design region for the Policy, Navigable and multi-route rules).
// NavPlacement is read by PageNavPlacementReader because the generated enum is not bound here.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Builds PageModel values from [Page] class declarations.</summary>
internal static partial class PageCollector
{
  private const string AttributeName = "Page";

  // {name:type}? — page route tokens carry real C# type names (Guid, int, …), used directly.
  // An untyped token ({name}) is a string; the previous greedy pattern split its name instead.
  [GeneratedRegex(@"\{(\w+)\s*(?::\s*(\w+))?\}")]
  private static partial Regex RouteParam();

  internal static PageModel? GetPage(ClassDeclarationSyntax cls, SemanticModel semanticModel)
  {
    AttributeSyntax[] pageAttributes =
    [
      .. cls.AttributeLists
        .SelectMany(static l => l.Attributes)
        .Where(static a => StripAttribute(a.Name.ToString()) == AttributeName),
    ];

    AttributeSyntax? attr = pageAttributes.FirstOrDefault();
    if (attr?.ArgumentList is null) return null;

    string className = cls.Identifier.Text;
    string? route = null;
    var additionalRoutes = new List<(string Route, SyntaxNode Node)>();
    var errors = new List<PageDiagnostic>();
    string? policyExpression = null;
    bool policyArgumentPresent = false;
    bool navigableRequested = false;
    ExpressionSyntax? invalidNavigable = null;
    string description = "";
    string navGroup = "";
    int navOrder = 0;
    bool navPinBottom = false;
    var agentActions = new List<string>();

    foreach (AttributeArgumentSyntax arg in attr.ArgumentList.Arguments)
    {
      string? argName = arg.NameEquals?.Name.Identifier.Text;

      if (argName == "Policy")
      {
        policyArgumentPresent = true;
        if (TryGetConstPolicyExpression(arg.Expression, out string expression))
          policyExpression = expression;
        else
          errors.Add(PageDiagnostic.From(DiagnosticDescriptors.PageInvalidPolicy, arg.Expression));

        continue;
      }

      if (argName == "Navigable")
      {
        if (arg.Expression.IsKind(SyntaxKind.TrueLiteralExpression)) navigableRequested = true;
        else if (!arg.Expression.IsKind(SyntaxKind.FalseLiteralExpression)) invalidNavigable = arg.Expression;
        continue;
      }

      if (argName == "Description")
      {
        if (semanticModel.GetConstantValue(arg.Expression) is { HasValue: true, Value: string text })
          description = text;
        continue;
      }

      if (argName == "NavGroup")
      {
        if (semanticModel.GetConstantValue(arg.Expression) is { HasValue: true, Value: string group })
          navGroup = group;
        continue;
      }

      if (argName == "NavOrder")
      {
        if (semanticModel.GetConstantValue(arg.Expression) is { HasValue: true, Value: int order })
          navOrder = order;
        continue;
      }

      if (argName == "NavPlacement")
      {
        navPinBottom = PageNavPlacementReader.IsNavPinBottom(semanticModel, arg.Expression);
        continue;
      }

      if (argName == "AgentActions")
      {
        foreach (TypeOfExpressionSyntax typeOf in arg.Expression.DescendantNodesAndSelf().OfType<TypeOfExpressionSyntax>())
        {
          ITypeSymbol? type = semanticModel.GetTypeInfo(typeOf.Type).Type;
          agentActions.Add(type is null or IErrorTypeSymbol
            ? typeOf.Type.WithoutTrivia().ToString()
            : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        continue;
      }

      string? literal = arg.Expression is LiteralExpressionSyntax lit && lit.Token.Value is string value ? value : null;

      // Primary route: first positional argument (or named RouteTemplate) — string literal only.
      if (route is null && (argName is null || argName == "RouteTemplate"))
      {
        if (literal is null) return null;
        route = literal;
        continue;
      }

      // Additional routes: every further positional argument (params string[]).
      if (argName is null)
      {
        if (literal is null)
        {
          errors.Add(PageDiagnostic.From(
            DiagnosticDescriptors.PageConflictingRouteDeclaration, arg.Expression, className,
            $"has a non-literal additional route '{arg.Expression}' (additional routes must be string literals)"));
        }
        else
        {
          additionalRoutes.Add((literal, arg.Expression));
        }
      }
    }

    if (route is null) return null;

    foreach (AttributeSyntax extra in pageAttributes.Skip(1))
    {
      errors.Add(PageDiagnostic.From(
        DiagnosticDescriptors.PageConflictingRouteDeclaration, extra, className,
        "is declared more than once (pass additional routes as further arguments of the one [Page]: [Page(\"/primary\", \"/alias\")])"));
    }

    string? ns = null;
    for (SyntaxNode? p = cls.Parent; p is not null; p = p.Parent)
    {
      if (p is BaseNamespaceDeclarationSyntax n) { ns = n.Name.ToString(); break; }
    }

    if (ns is null && errors.Count == 0) return null;

    PageParsedRoute primary = ParseRoute(route, knownParameters: null, out _);
    var seen = new Dictionary<string, string>(StringComparer.Ordinal) { [primary.ShapeKey] = route };
    var additionalAttributes = new List<string>();
    var parameters = new List<(string Type, string Name)>(primary.Parameters);

    foreach ((string additional, SyntaxNode node) in additionalRoutes)
    {
      // Agreement is checked against every token seen so far, so two aliases cannot give one
      // alias-only token different types (the [Parameter] property has exactly one).
      PageParsedRoute parsed = ParseRoute(additional, parameters, out string? conflict);
      if (conflict is not null)
      {
        errors.Add(PageDiagnostic.From(DiagnosticDescriptors.PageConflictingRouteDeclaration, node, className, conflict));
        continue;
      }

      if (seen.ContainsKey(parsed.ShapeKey))
      {
        errors.Add(PageDiagnostic.From(DiagnosticDescriptors.PageDuplicateRoute, node, className, additional));
        continue;
      }

      seen[parsed.ShapeKey] = additional;
      additionalAttributes.Add(parsed.RouteAttribute);

      // Tokens only an additional route declares still need a [Parameter] for Blazor to bind.
      foreach ((string Type, string Name) p in parsed.Parameters)
      {
        if (!parameters.Any(existing => string.Equals(existing.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
          parameters.Add(p);
      }
    }

    // A hand-written [Route] on the same declaration that repeats a [Page] route is the same
    // ambiguous-route bug; distinct hand-written aliases stay legal (the pre-096 workaround).
    foreach (AttributeSyntax routeAttr in cls.AttributeLists
      .SelectMany(static l => l.Attributes)
      .Where(static a => StripAttribute(a.Name.ToString()) == "Route"))
    {
      if (routeAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax { Token.Value: string handWritten }
        && seen.ContainsKey(ParseRoute(handWritten, primary.Parameters, out _).ShapeKey))
      {
        errors.Add(PageDiagnostic.From(DiagnosticDescriptors.PageDuplicateRoute, routeAttr, className, handWritten));
      }
    }

    if (errors.Count > 0 || ns is null) return PageModel.Failed(cls, route, errors);

    string signature = string.Join(", ", primary.Parameters.Select(static p => p.Type + " " + p.Name));
    string hint = ns + "." + className;
    string policy = policyArgumentPresent
      ? policyExpression!
      : "Policies.Anonymous";

    // Navigable judges the primary route only; parameterized additional routes are plain aliases.
    PageDiagnostic? navigableDiagnostic = null;
    if (invalidNavigable is not null)
    {
      navigableDiagnostic = PageDiagnostic.From(
        DiagnosticDescriptors.PageInvalidNavigable, invalidNavigable, className, "must be the literal true or false");
    }
    else if (navigableRequested && primary.Parameters.Count > 0)
    {
      navigableDiagnostic = PageDiagnostic.From(
        DiagnosticDescriptors.PageInvalidNavigable, attr, className, $"requires a static route, but '{route}' has parameters");
    }

    bool navigable = navigableRequested && navigableDiagnostic is null;

    return new PageModel(
      ns, className, hint, primary.RouteAttribute, [.. additionalAttributes], signature, primary.Format, [.. parameters], policy,
      Errors: [], RouteTemplate: route, Navigable: navigable, NavigableDiagnostic: navigableDiagnostic,
      Description: description, RouteTemplates: [route, .. additionalRoutes.Select(static r => r.Route)],
      NavGroup: navGroup, NavOrder: navOrder, NavPinBottom: navPinBottom, AgentActions: [.. agentActions]);
  }

  /// <summary>
  /// Renders one route template. For an additional route (<paramref name="knownParameters"/> set),
  /// a token named like an earlier route's token inherits its type when untyped and must agree when typed.
  /// </summary>
  private static PageParsedRoute ParseRoute(string route, List<(string Type, string Name)>? knownParameters, out string? conflict)
  {
    conflict = null;
    var result = new PageParsedRoute();
    var urlSegments = new List<string>();
    var formatParts = new List<string>();
    var keyParts = new List<string>();

    foreach (string segment in route.Split('/'))
    {
      Match m = RouteParam().Match(segment);
      if (!m.Success)
      {
        urlSegments.Add(segment);
        formatParts.Add(segment);
        keyParts.Add(segment.ToLowerInvariant());
        continue;
      }

      string name = m.Groups[1].Value;
      string? declared = m.Groups[2].Success ? m.Groups[2].Value : null;
      string type = declared ?? "string";

      if (knownParameters is not null)
      {
        (string Type, string Name) match = knownParameters
          .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match.Name is not null)
        {
          if (declared is not null && !string.Equals(declared, match.Type, StringComparison.OrdinalIgnoreCase))
            conflict ??= $"route '{route}' declares token '{name}' as {declared} but an earlier route declares it as {match.Type}";
          name = match.Name;
          type = match.Type;
        }
      }

      string lower = type.ToLowerInvariant();
      string fmt = lower == "datetime" ? ":yyyy-MM-dd" : string.Empty;

      formatParts.Add("{" + name + fmt + "}");
      result.Parameters.Add((type, name));
      urlSegments.Add(lower != "string" ? "{" + name + ":" + lower + "}" : "{" + name + "}");
      keyParts.Add("{" + (lower != "string" ? lower : "") + "}");
    }

    result.RouteAttribute = string.Join("/", urlSegments);
    result.Format = string.Join("/", formatParts);
    result.ShapeKey = string.Join("/", keyParts).TrimEnd('/');
    return result;
  }

  /// <summary>
  /// Accepts only const-style field references: <c>Policies.X</c>, <c>AuthorizationConstants.Policies.X</c>,
  /// or a simple <c>IdentifierName</c> (e.g. after <c>using static</c>).
  /// </summary>
  private static bool TryGetConstPolicyExpression(ExpressionSyntax expression, out string expressionText)
  {
    expressionText = "";

    if (IsNameOfExpression(expression))
      return false;

    if (expression is LiteralExpressionSyntax)
      return false;

    if (expression is MemberAccessExpressionSyntax or IdentifierNameSyntax)
    {
      expressionText = expression.WithoutTrivia().ToString();
      return expressionText.Length > 0;
    }

    return false;
  }

  private static bool IsNameOfExpression(ExpressionSyntax expression)
  {
    if (expression is not InvocationExpressionSyntax invocation)
      return false;

    return invocation.Expression switch
    {
      IdentifierNameSyntax id => id.Identifier.Text == "nameof",
      MemberAccessExpressionSyntax member => member.Name.Identifier.Text == "nameof",
      _ => false
    };
  }

  private static string StripAttribute(string name)
  {
    int dot = name.LastIndexOf('.', StringComparison.Ordinal);
    if (dot >= 0) name = name.Substring(dot + 1);
    return name.EndsWith("Attribute", System.StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
  }
}
