#region Purpose
// For a Blazor page class marked [Page("/route"[, "/alias", …][, Policy = Policies.X])] generates [Route]s,
// INavigableComponent/IStaticRoute, GetPageUrl(...), Policy accessor, and [Parameter] props for
// route tokens; plus one per-assembly PageRegistry of the pages that opt in as navigation
// destinations (task 239-001). Origin: web-spa Page Moxy template, replaced in task 053.
#endregion

#region Design
// Policy is a pit-of-success const binding (task 094), not a free-form string:
// - Omitted → emit Policies.Anonymous (product must define that const).
// - Policy = Policies.SettingsEdit (member access / simple identifier const ref) → emit that
//   expression verbatim so claim values like "settings.edit" work without identifier glue.
// - String literals and nameof(...) are rejected with TWE005 — one authoring form only.
// - Never silently fall back to Anonymous when Policy was written but unparseable.
// Route stays a string literal (routes are inherently literal templates).
// Attribute Policy property remains string so const string fields are valid attribute args;
// generation keys off syntax, not the attribute's compile-time string value.
// PageRegistry (task 239-001) is the single source of navigation destinations for the NavMenu
// and the Ctrl-K palette (239-003) — no second hand-copied route list:
// - Opt-in is a [Page] property, `Navigable = true` (default false), not a separate attribute:
//   one attribute already owns route + policy, and a second marker could drift from it. Literal
//   true/false only (syntax-keyed like Policy); anything else is TWE009.
// - Only static routes qualify: a parameterized route needs arguments, so Navigable on one is
//   TWE009 (fail-closed, never a silent omission). v1 has no parameterized destinations.
// - An opt-in page also gets INavigationDestination, the marker the registry test checks both
//   ways. NavMenu renders the registry itself (categories from NavGroup, gates from each page's
//   Policy), so it has no page list of its own to drift.
// - Entries are a generated array of static member reads (typeof + GetPageUrl/Title/NavIcon/
//   Policy): reflection-free, AOT/trim safe. Sorted by NavOrder, then route (ordinal).
// - The registry is emitted only when the assembly declares at least one [Page], so assemblies
//   that merely carry the generator never need INavigableComponent's Icon type.
// Multi-route pages (task 096) — one [Page] with params additional routes, not stacked [Page]:
// - [Page("/clients", "/clients/revenue", "/clients/me-close")]. The first argument is the
//   PRIMARY route; the rest are ADDITIONAL routes. Stacked [Page] was rejected because each copy
//   could carry its own Policy/Navigable, so "which declaration owns the page" would need a rule;
//   one attribute keeps route + policy + opt-in in one place (same reason Navigable is a property).
//   AllowMultiple = false makes the language enforce it; TWE011 adds the teaching message.
// - Primary owns the page surface: GetPageUrl, IStaticRoute, RouteTemplate, PageRegistry, and the
//   TWE009 Navigable judgment. Additional routes are emitted as [Route] aliases only — never registry
//   rows, never URL helpers — so a parameterized alias on a static navigable page is fine.
// - Policy (TWE005) is per page, not per route: every route of the page is the same component.
// - An alias token named like an earlier route's token inherits its type when untyped ({ClientId}
//   after {ClientId:string}) and must agree when typed (else TWE011) — also between two aliases,
//   since the token gets one [Parameter] property. An alias-only token still gets that property,
//   since Blazor fails to bind a route value with no matching property.
// - TWE010: two routes of one page that Blazor treats as the same route (case-insensitive,
//   token-name-free shape), including a hand-written [Route] repeating a [Page] route. Distinct
//   hand-written [Route] aliases stay legal (the pre-096 workaround still compiles). Shape
//   normalization covers the {name} / {name:type} tokens RouteParam recognizes; catch-all ({*x}),
//   optional ({id?}) and parameterized-constraint tokens are not [Page] route grammar (no
//   [Parameter] is generated for them) and compare as literal text.
// - Errors (TWE005/010/011) are fail-closed: reported, and no page surface is emitted.
// Menu and agent metadata (task 300) — one declaration feeds NavMenu, Ctrl-K, Ask and WebMCP:
// - NavGroup (string constant, "" = top level) is the menu category; NavOrder (int constant) is
//   the menu position. PageRegistry.All is sorted by NavOrder, then route, so the registry order
//   IS the menu order and NavMenu starts a category whenever NavGroup changes; the registry test
//   requires each NavGroup to be one contiguous run. NavPlacement is Top (default) or Bottom.
//   Bottom pins that group in the lower nav. One named group uses one placement. Description,
//   NavGroup and NavOrder accept any compile-time constant. NavPlacement does not: this
//   generator emits the enum, so the syntax pass never binds it and GetConstantValue has no
//   value for a NavPlacement member. Bottom is an unbound NavPlacement.Bottom, a using-static
//   Bottom, a const field followed to an initializer that is one of those, or a cast (or a bare
//   integer) whose constant value is 1. The same forms with Top, or any other known value, stay
//   Top. Any other shape stays Top, with no diagnostic.
//   The enum is emitted internal next to [Page]. Every project that references this generator
//   gets that attribute, and several share a root namespace, so a public enum would collide.
//   The registry stores it on an internal init property for the same reason.
// - AgentActions (typeof list) names the catalog actions the agent may execute while this page is
//   on screen. Types are resolved with the semantic model and emitted fully qualified, so the
//   generated file needs none of the page file's usings. Every page that declares any (navigable
//   or not, parameterized or not) is a PageRegistry.ActionScopes row with all its route templates,
//   so the agent matches the current path to its page without a hand-written route list.
// Incremental caching: PageModel is a value-equatable record (arrays by sequence) holding
// location-free PageDiagnostic values; the Diagnostic is built in RegisterSourceOutput. A held
// Diagnostic/Location pins old SyntaxTrees and never compares equal, which made every edit re-run
// every step — and pages.Collect() would keep all of them alive in one cached array.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Source generator that emits [Route], navigation helpers, and parameter props for [Page] Blazor pages.</summary>
[Generator]
public sealed partial class PageSourceGenerator : IIncrementalGenerator
{
  private const string AttributeName = "Page";
  private const int NavPlacementWalkLimit = 8;

  // {name:type}? — page route tokens carry real C# type names (Guid, int, …), used directly.
  // An untyped token ({name}) is a string; the previous greedy pattern split its name instead.
  [GeneratedRegex(@"\{(\w+)\s*(?::\s*(\w+))?\}")]
  private static partial Regex RouteParam();

  /// <summary>Registers incremental generator steps for [Page] surface emission.</summary>
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(
      static (p, _) => Sanitize(p.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? ns) && !string.IsNullOrWhiteSpace(ns)
        ? ns!
        : "Generated"));

    context.RegisterSourceOutput(rootNamespace, static (spc, ns) =>
      spc.AddSource("PageAttribute.g.cs", SourceText.From(BuildAttribute(ns), Encoding.UTF8)));

    IncrementalValuesProvider<PageModel> pages = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Any(),
      transform: static (ctx, _) => GetPage((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel))
      .Where(static p => p is not null)
      .Select(static (p, _) => p!);

    context.RegisterSourceOutput(pages.Combine(rootNamespace), static (spc, pair) =>
    {
      PageModel page = pair.Left;
      if (!page.Errors.IsEmpty)
      {
        foreach (PageDiagnostic error in page.Errors)
          spc.ReportDiagnostic(error.ToDiagnostic());
        return;
      }

      if (page.NavigableDiagnostic is not null)
        spc.ReportDiagnostic(page.NavigableDiagnostic.ToDiagnostic());

      spc.AddSource($"{page.HintName}.Page.g.cs", SourceText.From(Emit(page, pair.Right), Encoding.UTF8));
    });

    context.RegisterSourceOutput(pages.Collect().Combine(rootNamespace), static (spc, pair) =>
    {
      if (pair.Left.IsDefaultOrEmpty) return;
      spc.AddSource("PageRegistry.g.cs", SourceText.From(EmitRegistry(pair.Left, pair.Right), Encoding.UTF8));
    });
  }

  // Value-equatable (incremental caching): arrays compare by sequence, and diagnostics are
  // location-free PageDiagnostic values — a Diagnostic/Location would pin old SyntaxTrees in the
  // cache and never compare equal, so every edit would re-run every step and PageRegistry.
  // Errors are fail-closed (TWE005/010/011): reported, and the page surface is not emitted.
  private sealed record PageModel(
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
  private sealed record PageDiagnostic(
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
  private sealed class ParsedRoute
  {
    public string RouteAttribute { get; set; } = "";
    public string Format { get; set; } = "";
    public string ShapeKey { get; set; } = ""; // case-insensitive, token-name-free: what Blazor treats as "the same route"
    public List<(string Type, string Name)> Parameters { get; } = [];
  }

  private static PageModel? GetPage(ClassDeclarationSyntax cls, SemanticModel semanticModel)
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
        navPinBottom = IsNavPinBottom(semanticModel, arg.Expression);
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

    ParsedRoute primary = ParseRoute(route, knownParameters: null, out _);
    var seen = new Dictionary<string, string>(StringComparer.Ordinal) { [primary.ShapeKey] = route };
    var additionalAttributes = new List<string>();
    var parameters = new List<(string Type, string Name)>(primary.Parameters);

    foreach ((string additional, SyntaxNode node) in additionalRoutes)
    {
      // Agreement is checked against every token seen so far, so two aliases cannot give one
      // alias-only token different types (the [Parameter] property has exactly one).
      ParsedRoute parsed = ParseRoute(additional, parameters, out string? conflict);
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
  private static ParsedRoute ParseRoute(string route, List<(string Type, string Name)>? knownParameters, out string? conflict)
  {
    conflict = null;
    var result = new ParsedRoute();
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

  private static string Emit(PageModel page, string rootNamespace)
  {
    // IStaticRoute follows the primary route (GetPageUrl's signature); Parameters also holds
    // alias-only tokens, which need [Parameter] props but never URL arguments.
    bool hasParameters = page.Signature.Length > 0;

    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(page.Namespace).Append('\n').Append("{\n");
    sb.Append("  using Microsoft.AspNetCore.Components;\n");
    sb.Append("  using ").Append(rootNamespace).Append(";\n\n");

    sb.Append("  [Route(\"").Append(page.RouteAttribute).Append("\")]\n");
    foreach (string additional in page.AdditionalRouteAttributes)
      sb.Append("  [Route(\"").Append(additional).Append("\")]\n");
    sb.Append("  partial class ").Append(page.ClassName).Append(" : INavigableComponent");
    if (!hasParameters) sb.Append(", IStaticRoute");
    if (page.Navigable) sb.Append(", INavigationDestination");
    sb.Append('\n').Append("  {\n");

    sb.Append("    public static string GetPageUrl(").Append(page.Signature)
      .Append(") => global::System.FormattableString.Invariant($\"").Append(page.Format).Append("\");\n");
    sb.Append("    public static string Policy { get; } = ").Append(page.PolicyExpression).Append(";\n");

    foreach ((string type, string name) in page.Parameters)
      sb.Append("    [Parameter] public ").Append(type).Append(' ').Append(name).Append(" { get; set; }\n");

    sb.Append("  }\n").Append("}\n");
    return sb.ToString();
  }

  private static string EmitRegistry(ImmutableArray<PageModel> pages, string rootNamespace)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("#nullable enable\n");
    sb.Append("namespace ").Append(rootNamespace).Append("\n{\n");
    sb.Append("  /// <summary>One navigation destination: a [Page(Navigable = true)] page with a static route.</summary>\n");
    sb.Append("  public sealed record PageRegistryEntry(\n");
    sb.Append("    global::System.Type PageType,\n");
    sb.Append("    string RouteTemplate,\n");
    sb.Append("    string Url,\n");
    sb.Append("    string Title,\n");
    sb.Append("    Icon? NavIcon,\n");
    sb.Append("    string Policy,\n");
    sb.Append("    string Description,\n");
    sb.Append("    string NavGroup,\n");
    sb.Append("    int NavOrder)\n");
    sb.Append("  {\n");
    sb.Append("    /// <summary>Bottom pins this entry's NavGroup in the lower nav. Top is the default.</summary>\n");
    sb.Append("    internal global::").Append(rootNamespace).Append(".NavPlacement NavPlacement { get; init; }\n");
    sb.Append("  }\n\n");
    sb.Append("  /// <summary>A page that declares [Page(AgentActions = ...)]: its route templates and the actions the agent may run on it.</summary>\n");
    sb.Append("  public sealed record PageActionScope(\n");
    sb.Append("    global::System.Type PageType,\n");
    sb.Append("    global::System.Collections.Generic.IReadOnlyList<string> RouteTemplates,\n");
    sb.Append("    global::System.Collections.Generic.IReadOnlyList<global::System.Type> ActionTypes);\n\n");
    sb.Append("  /// <summary>Every navigation destination in this assembly, generated from [Page(Navigable = true)] (sorted by NavOrder, then route: the menu order).</summary>\n");
    sb.Append("  public static class PageRegistry\n  {\n");
    sb.Append("    public static global::System.Collections.Generic.IReadOnlyList<PageRegistryEntry> All { get; } =\n");
    sb.Append("    [\n");

    foreach (PageModel page in pages
      .Where(static p => p.Errors.IsEmpty && p.Navigable)
      .OrderBy(static p => p.NavOrder)
      .ThenBy(static p => p.RouteTemplate, System.StringComparer.Ordinal)
      .ThenBy(static p => p.HintName, System.StringComparer.Ordinal))
    {
      string type = "global::" + page.Namespace + "." + page.ClassName;
      sb.Append("      new(typeof(").Append(type).Append("), \"").Append(page.RouteTemplate).Append("\", ")
        .Append(type).Append(".GetPageUrl(), ")
        .Append(type).Append(".Title, ")
        .Append(type).Append(".NavIcon, ")
        .Append(type).Append(".Policy, ")
        .Append(CSharpString(page.Description)).Append(", ")
        .Append(CSharpString(page.NavGroup)).Append(", ")
        .Append(page.NavOrder.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Append(") { NavPlacement = global::").Append(rootNamespace)
        .Append(page.NavPinBottom ? ".NavPlacement.Bottom" : ".NavPlacement.Top")
        .Append(" },\n");
    }

    sb.Append("    ];\n\n");
    sb.Append("    /// <summary>Pages that declare AgentActions, generated from [Page(AgentActions = ...)] (sorted by route).</summary>\n");
    sb.Append("    public static global::System.Collections.Generic.IReadOnlyList<PageActionScope> ActionScopes { get; } =\n");
    sb.Append("    [\n");
    foreach (PageModel page in pages
      .Where(static p => p.Errors.IsEmpty && !p.AgentActions.IsEmpty)
      .OrderBy(static p => p.RouteTemplate, System.StringComparer.Ordinal)
      .ThenBy(static p => p.HintName, System.StringComparer.Ordinal))
    {
      sb.Append("      new(typeof(global::").Append(page.Namespace).Append('.').Append(page.ClassName).Append("), [");
      sb.Append(string.Join(", ", page.RouteTemplates.Select(CSharpString)));
      sb.Append("], [");
      sb.Append(string.Join(", ", page.AgentActions.Select(static type => "typeof(" + type + ")")));
      sb.Append("]),\n");
    }

    sb.Append("    ];\n  }\n}\n");
    return sb.ToString();
  }

  private static string BuildAttribute(string ns)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(ns).Append("\n{\n");
    sb.Append("    /// <summary>Where a [Page] NavGroup sits. Top is the default. Bottom pins the group.</summary>\n");
    sb.Append("    internal enum NavPlacement\n");
    sb.Append("    {\n");
    sb.Append("        Top = 0,\n");
    sb.Append("        Bottom = 1,\n");
    sb.Append("    }\n\n");
    sb.Append("    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false)]\n");
    sb.Append("    internal sealed class PageAttribute : System.Attribute\n    {\n");
    sb.Append("        public string RouteTemplate { get; set; }\n");
    sb.Append("        /// <summary>Const policy field reference only (e.g. Policies.SettingsEdit). Omit Policy for the anonymous default.</summary>\n");
    sb.Append("        public string Policy { get; set; }\n");
    sb.Append("        /// <summary>Literal true lists this static-route page in PageRegistry (NavMenu + palette destinations). Default false.</summary>\n");
    sb.Append("        public bool Navigable { get; set; }\n");
    sb.Append("        /// <summary>One line saying what this page is for. Navigable pages set it.</summary>\n");
    sb.Append("        public string Description { get; set; }\n");
    sb.Append("        /// <summary>Menu category title; omit for the top level. A string literal.</summary>\n");
    sb.Append("        public string NavGroup { get; set; }\n");
    sb.Append("        /// <summary>Menu position: PageRegistry.All is sorted by NavOrder, then route. An int literal.</summary>\n");
    sb.Append("        public int NavOrder { get; set; }\n");
    sb.Append("        /// <summary>Bottom pins this page's NavGroup in the lower nav. Top is the default.</summary>\n");
    sb.Append("        public NavPlacement NavPlacement { get; set; }\n");
    sb.Append("        /// <summary>Catalog Action types the agent may execute while this page is on screen.</summary>\n");
    sb.Append("        public System.Type[] AgentActions { get; set; }\n");
    sb.Append("        /// <summary>Extra routes emitted as [Route] aliases; GetPageUrl and PageRegistry use RouteTemplate (the primary route) only.</summary>\n");
    sb.Append("        public string[] AdditionalRoutes { get; }\n");
    sb.Append("        public PageAttribute(string RouteTemplate, params string[] AdditionalRoutes) { this.RouteTemplate = RouteTemplate; this.AdditionalRoutes = AdditionalRoutes; }\n");
    sb.Append("    }\n}\n");
    return sb.ToString();
  }

  // 1 is Bottom. The generated enum is not in this compilation, so a NavPlacement member has no
  // constant value. A const field is followed to its initializer. A cast is read from its operand.
  // An unbound NavPlacement.Bottom, or a using-static Bottom, is read by that name. Any other
  // shape stays Top.
  private static bool IsNavPinBottom(SemanticModel semanticModel, ExpressionSyntax expression) =>
    ReadNavPlacementValue(semanticModel, expression, 0) == 1;

  private static long? ReadNavPlacementValue(SemanticModel semanticModel, ExpressionSyntax expression, int depth)
  {
    if (depth > NavPlacementWalkLimit)
    {
      return null;
    }

    ExpressionSyntax current = expression;
    while (current is ParenthesizedExpressionSyntax parenthesized)
    {
      current = parenthesized.Expression;
    }

    if (current is CheckedExpressionSyntax checkedExpression)
    {
      return ReadNavPlacementValue(semanticModel, checkedExpression.Expression, depth + 1);
    }

    if (TryReadIntegralConstant(semanticModel, current, out long known))
    {
      return known;
    }

    if (current is CastExpressionSyntax cast)
    {
      return ReadNavPlacementValue(semanticModel, cast.Expression, depth + 1);
    }

    if (FindConstFieldInitializer(semanticModel, current) is { } initializer)
    {
      return ReadNavPlacementValue(initializer.SemanticModel, initializer.Expression, depth + 1);
    }

    return UnboundNavPlacementValue(semanticModel, current);
  }

  private static bool TryReadIntegralConstant(SemanticModel semanticModel, ExpressionSyntax expression, out long value)
  {
    value = 0;
    Optional<object?> constant = semanticModel.GetConstantValue(expression);
    if (!constant.HasValue || constant.Value is not (byte or sbyte or short or ushort or int or uint or long or ulong or Enum))
    {
      return false;
    }

    try
    {
      value = Convert.ToInt64(constant.Value, CultureInfo.InvariantCulture);
      return true;
    }
    catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
    {
      return false;
    }
  }

  private static (ExpressionSyntax Expression, SemanticModel SemanticModel)? FindConstFieldInitializer
  (
    SemanticModel semanticModel,
    ExpressionSyntax expression
  )
  {
    SymbolInfo info = semanticModel.GetSymbolInfo(expression);
    ISymbol? symbol = info.Symbol;
    if (symbol is null && info.CandidateSymbols.Length == 1)
    {
      symbol = info.CandidateSymbols[0];
    }

    if (symbol is not IFieldSymbol { IsConst: true } field)
    {
      return null;
    }

    foreach (SyntaxReference reference in field.DeclaringSyntaxReferences)
    {
      if (reference.GetSyntax() is not VariableDeclaratorSyntax declarator)
      {
        continue;
      }

      if (declarator.Initializer?.Value is not ExpressionSyntax value)
      {
        continue;
      }

      SemanticModel initializerModel = value.SyntaxTree == semanticModel.SyntaxTree
        ? semanticModel
        : semanticModel.Compilation.GetSemanticModel(value.SyntaxTree);
      return (value, initializerModel);
    }

    return null;
  }

  private static long? UnboundNavPlacementValue(SemanticModel semanticModel, ExpressionSyntax expression)
  {
    SymbolInfo info = semanticModel.GetSymbolInfo(expression);
    if (info.Symbol is not null || info.CandidateSymbols.Length > 0)
    {
      return null;
    }

    if (expression is IdentifierNameSyntax identifier)
    {
      return PlacementValue(identifier.Identifier.Text);
    }

    if (expression is MemberAccessExpressionSyntax member && IsNavPlacementTypeName(member.Expression))
    {
      return PlacementValue(member.Name.Identifier.Text);
    }

    return null;
  }

  private static long? PlacementValue(string name) => name switch
  {
    "Bottom" => 1,
    "Top" => 0,
    _ => null
  };

  private static bool IsNavPlacementTypeName(ExpressionSyntax expression)
  {
    ExpressionSyntax current = expression;
    while (current is MemberAccessExpressionSyntax member)
    {
      if (member.Name.Identifier.Text == "NavPlacement")
      {
        return true;
      }

      current = member.Expression;
    }

    return current switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.Text == "NavPlacement",
      AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text == "NavPlacement",
      _ => false
    };
  }

  // Roslyn's own literal formatter escapes quotes, backslashes, and control characters such as
  // a newline, so any constant Description compiles in the registry. A non-constant expression
  // emits ""; the destinations test requires a Description on every menu destination.
  private static string CSharpString(string value) =>
    Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

  private static string StripAttribute(string name)
  {
    int dot = name.LastIndexOf('.', StringComparison.Ordinal);
    if (dot >= 0) name = name.Substring(dot + 1);
    return name.EndsWith("Attribute", System.StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
  }

  private static string Sanitize(string ns)
  {
    var sb = new StringBuilder(ns.Length);
    foreach (char c in ns)
      sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
    return sb.ToString();
  }
}
