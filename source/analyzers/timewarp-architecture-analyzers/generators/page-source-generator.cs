#region Purpose
// Roslyn replacement for the web-spa Page Moxy mixin (task 053). For a Blazor page class marked
// [Page("/route"[, Policy = Policies.X])] generates [Route], INavigableComponent/IStaticRoute,
// GetPageUrl(...), Policy accessor, and [Parameter] props for route tokens.
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
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;
using System.Text.RegularExpressions;

[Generator]
public sealed class PageSourceGenerator : IIncrementalGenerator
{
  private const string AttributeName = "Page";

  // {name:type}? — page route tokens carry real C# type names (Guid, int, …), used directly.
  private static readonly Regex RouteParam = new(@"\{(\w+)\s*:?(\w+)\}?", RegexOptions.Compiled);

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(
      static (p, _) => Sanitize(p.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? ns) && !string.IsNullOrWhiteSpace(ns)
        ? ns!
        : "GeneratedMixins"));

    context.RegisterSourceOutput(rootNamespace, static (spc, ns) =>
      spc.AddSource("PageAttribute.g.cs", SourceText.From(BuildAttribute(ns), Encoding.UTF8)));

    IncrementalValuesProvider<PageModel> pages = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
      transform: static (ctx, _) => GetPage((ClassDeclarationSyntax)ctx.Node))
      .Where(static p => p is not null)
      .Select(static (p, _) => p!.Value);

    context.RegisterSourceOutput(pages.Combine(rootNamespace), static (spc, pair) =>
    {
      PageModel page = pair.Left;
      if (page.PolicyDiagnostic is not null)
      {
        spc.ReportDiagnostic(page.PolicyDiagnostic);
        return;
      }

      spc.AddSource($"{page.HintName}.Page.g.cs", SourceText.From(Emit(page, pair.Right), Encoding.UTF8));
    });
  }

  private readonly struct PageModel
  {
    public PageModel(
      string ns,
      string className,
      string hintName,
      string routeAttribute,
      string signature,
      string format,
      IReadOnlyList<(string Type, string Name)> parameters,
      string policyExpression,
      Diagnostic? policyDiagnostic)
    {
      Namespace = ns;
      ClassName = className;
      HintName = hintName;
      RouteAttribute = routeAttribute;
      Signature = signature;
      Format = format;
      Parameters = parameters;
      PolicyExpression = policyExpression;
      PolicyDiagnostic = policyDiagnostic;
    }

    public string Namespace { get; }
    public string ClassName { get; }
    public string HintName { get; }
    public string RouteAttribute { get; }
    public string Signature { get; }
    public string Format { get; }
    public IReadOnlyList<(string Type, string Name)> Parameters { get; }
    public string PolicyExpression { get; }
    public Diagnostic? PolicyDiagnostic { get; }
  }

  private static PageModel? GetPage(ClassDeclarationSyntax cls)
  {
    AttributeSyntax? attr = cls.AttributeLists
      .SelectMany(static l => l.Attributes)
      .FirstOrDefault(static a => StripAttribute(a.Name.ToString()) == AttributeName);

    if (attr?.ArgumentList is null) return null;

    string? route = null;
    string? policyExpression = null;
    Diagnostic? policyDiagnostic = null;
    bool policyArgumentPresent = false;

    foreach (AttributeArgumentSyntax arg in attr.ArgumentList.Arguments)
    {
      string? argName = arg.NameEquals?.Name.Identifier.Text;

      if (argName == "Policy")
      {
        policyArgumentPresent = true;
        if (TryGetConstPolicyExpression(arg.Expression, out string expression))
        {
          policyExpression = expression;
        }
        else
        {
          policyDiagnostic = Diagnostic.Create(DiagnosticDescriptors.PageInvalidPolicy, arg.Expression.GetLocation());
        }

        continue;
      }

      // Route: constructor positional or named RouteTemplate — string literal only.
      if (arg.Expression is LiteralExpressionSyntax lit && lit.Token.Value is string value)
      {
        if (argName is null || argName == "RouteTemplate")
          route ??= value;
      }
    }

    if (route is null) return null;

    // Invalid Policy: still return a model so RegisterSourceOutput can report TWE005.
    if (policyDiagnostic is not null)
    {
      return new PageModel(
        ns: cls.Identifier.Text,
        className: cls.Identifier.Text,
        hintName: cls.Identifier.Text,
        routeAttribute: route,
        signature: "",
        format: "",
        parameters: [],
        policyExpression: "",
        policyDiagnostic: policyDiagnostic);
    }

    string? ns = null;
    for (SyntaxNode? p = cls.Parent; p is not null; p = p.Parent)
    {
      if (p is BaseNamespaceDeclarationSyntax n) { ns = n.Name.ToString(); break; }
    }

    if (ns is null) return null;

    var urlSegments = new List<string>();
    var formatParts = new List<string>();
    var parameters = new List<(string Type, string Name)>();

    foreach (string segment in route.Split('/'))
    {
      Match m = RouteParam.Match(segment);
      if (!m.Success)
      {
        urlSegments.Add(segment);
        formatParts.Add(segment);
        continue;
      }

      string name = m.Groups[1].Value;
      string type = m.Groups[2].Value;
      string lower = type.ToLowerInvariant();
      string fmt = lower == "datetime" ? ":yyyy-MM-dd" : string.Empty;

      formatParts.Add("{" + name + fmt + "}");
      parameters.Add((type, name));
      urlSegments.Add(lower != "string" ? "{" + name + ":" + lower + "}" : "{" + name + "}");
    }

    string routeAttribute = string.Join("/", urlSegments);
    string signature = string.Join(", ", parameters.Select(static p => p.Type + " " + p.Name));
    string format = string.Join("/", formatParts);
    string hint = ns + "." + cls.Identifier.Text;
    string policy = policyArgumentPresent
      ? policyExpression!
      : "Policies.Anonymous";

    return new PageModel(ns, cls.Identifier.Text, hint, routeAttribute, signature, format, parameters, policy, policyDiagnostic: null);
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
    bool hasParameters = page.Parameters.Count > 0;

    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(page.Namespace).Append('\n').Append("{\n");
    sb.Append("  using Microsoft.AspNetCore.Components;\n");
    sb.Append("  using ").Append(rootNamespace).Append(";\n\n");

    sb.Append("  [Route(\"").Append(page.RouteAttribute).Append("\")]\n");
    sb.Append("  partial class ").Append(page.ClassName).Append(" : INavigableComponent");
    if (!hasParameters) sb.Append(", IStaticRoute");
    sb.Append('\n').Append("  {\n");

    sb.Append("    public static string GetPageUrl(").Append(page.Signature)
      .Append(") => global::System.FormattableString.Invariant($\"").Append(page.Format).Append("\");\n");
    sb.Append("    public static string Policy { get; } = ").Append(page.PolicyExpression).Append(";\n");

    foreach ((string type, string name) in page.Parameters)
      sb.Append("    [Parameter] public ").Append(type).Append(' ').Append(name).Append(" { get; set; }\n");

    sb.Append("  }\n").Append("}\n");
    return sb.ToString();
  }

  private static string BuildAttribute(string ns)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(ns).Append("\n{\n");
    sb.Append("    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true)]\n");
    sb.Append("    internal sealed class PageAttribute : System.Attribute\n    {\n");
    sb.Append("        public string RouteTemplate { get; set; }\n");
    sb.Append("        /// <summary>Const policy field reference only (e.g. Policies.SettingsEdit). Omit Policy for the anonymous default.</summary>\n");
    sb.Append("        public string Policy { get; set; }\n");
    sb.Append("        public PageAttribute(string RouteTemplate) { this.RouteTemplate = RouteTemplate; }\n");
    sb.Append("    }\n}\n");
    return sb.ToString();
  }

  private static string StripAttribute(string name)
  {
    int dot = name.LastIndexOf('.');
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
