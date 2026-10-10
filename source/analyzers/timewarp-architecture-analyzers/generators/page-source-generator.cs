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
// - An opt-in page also gets INavigationDestination. TimeWarpNavLink constrains TPage to it, so
//   a NavMenu link to a page outside the registry is a compile error — the menu keeps its hand
//   markup (categories, AuthorizeView groups, template-flag regions) and still cannot drift.
// - Entries are a generated array of static member reads (typeof + GetPageUrl/Title/NavIcon/
//   Policy): reflection-free, AOT/trim safe. Sorted by route (ordinal) for stable output.
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
// Incremental caching: PageModel is a value-equatable record (arrays by sequence) holding
// location-free PageDiagnostic values; the Diagnostic is built in RegisterSourceOutput. A held
// Diagnostic/Location pins old SyntaxTrees and never compares equal, which made every edit re-run
// every step — and pages.Collect() would keep all of them alive in one cached array.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Source generator that emits [Route], navigation helpers, and parameter props for [Page] Blazor pages.</summary>
[Generator]
public sealed partial class PageSourceGenerator : IIncrementalGenerator
{
  private const string AttributeName = "Page";

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
      transform: static (ctx, _) => GetPage((ClassDeclarationSyntax)ctx.Node))
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
    string Description)
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
      && Description == other.Description;

    public override int GetHashCode()
    {
      unchecked
      {
        int hash = (StringComparer.Ordinal.GetHashCode(HintName) * 397) ^ StringComparer.Ordinal.GetHashCode(RouteAttribute);
        hash = (hash * 397) ^ AdditionalRouteAttributes.Length;
        hash = (hash * 397) ^ Parameters.Length;
        hash = (hash * 397) ^ Errors.Length;
        hash = (hash * 397) ^ Navigable.GetHashCode();
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
        Description: "");
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

  private static PageModel? GetPage(ClassDeclarationSyntax cls)
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
        if (arg.Expression is LiteralExpressionSyntax described && described.Token.Value is string text)
          description = text;
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
      Description: description);
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
    sb.Append("    string Description);\n\n");
    sb.Append("  /// <summary>Every navigation destination in this assembly, generated from [Page(Navigable = true)] (sorted by route).</summary>\n");
    sb.Append("  public static class PageRegistry\n  {\n");
    sb.Append("    public static global::System.Collections.Generic.IReadOnlyList<PageRegistryEntry> All { get; } =\n");
    sb.Append("    [\n");

    foreach (PageModel page in pages
      .Where(static p => p.Errors.IsEmpty && p.Navigable)
      .OrderBy(static p => p.RouteTemplate, System.StringComparer.Ordinal)
      .ThenBy(static p => p.HintName, System.StringComparer.Ordinal))
    {
      string type = "global::" + page.Namespace + "." + page.ClassName;
      sb.Append("      new(typeof(").Append(type).Append("), \"").Append(page.RouteTemplate).Append("\", ")
        .Append(type).Append(".GetPageUrl(), ")
        .Append(type).Append(".Title, ")
        .Append(type).Append(".NavIcon, ")
        .Append(type).Append(".Policy, ")
        .Append(CSharpString(page.Description)).Append("),\n");
    }

    sb.Append("    ];\n  }\n}\n");
    return sb.ToString();
  }

  private static string BuildAttribute(string ns)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(ns).Append("\n{\n");
    sb.Append("    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false)]\n");
    sb.Append("    internal sealed class PageAttribute : System.Attribute\n    {\n");
    sb.Append("        public string RouteTemplate { get; set; }\n");
    sb.Append("        /// <summary>Const policy field reference only (e.g. Policies.SettingsEdit). Omit Policy for the anonymous default.</summary>\n");
    sb.Append("        public string Policy { get; set; }\n");
    sb.Append("        /// <summary>Literal true lists this static-route page in PageRegistry (NavMenu + palette destinations). Default false.</summary>\n");
    sb.Append("        public bool Navigable { get; set; }\n");
    sb.Append("        /// <summary>One line saying what this page is for. Navigable pages set it.</summary>\n");
    sb.Append("        public string Description { get; set; }\n");
    sb.Append("        /// <summary>Extra routes emitted as [Route] aliases; GetPageUrl and PageRegistry use RouteTemplate (the primary route) only.</summary>\n");
    sb.Append("        public string[] AdditionalRoutes { get; }\n");
    sb.Append("        public PageAttribute(string RouteTemplate, params string[] AdditionalRoutes) { this.RouteTemplate = RouteTemplate; this.AdditionalRoutes = AdditionalRoutes; }\n");
    sb.Append("    }\n}\n");
    return sb.ToString();
  }

  // Roslyn's own literal formatter escapes quotes, backslashes, and control characters such as
  // a newline, so any literal Description compiles in the registry. Description is read only as a
  // string literal; any other expression emits "" and the registry guard test reports it.
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
