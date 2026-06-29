#region Purpose
// Roslyn replacement for the three foundation-contracts Moxy mixins (task 053-001):
//   [RouteMixin(route, HttpVerb)]  [IAuthApiRequestMixin]  [IOpenDataQueryParametersMixin]
// Emits the marker attributes into the consumer's RootNamespace (matching Moxy, so the
// FastEndpoint generator still resolves <RootNamespace>.RouteMixinAttribute from metadata) and
// generates the partial-class members that Moxy used to template. Ships as an analyzer asset in
// the TimeWarp.Foundation.Contracts package, so it flows to consumers via PackageReference.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class ContractsMixinGenerator : IIncrementalGenerator
{
  private const string RouteName = "RouteMixin";
  private const string AuthName = "IAuthApiRequestMixin";
  private const string OpenDataName = "IOpenDataQueryParametersMixin";
  private const string Verb = "global::TimeWarp.Foundation.Features.HttpVerb";
  private const string Nvc = "global::System.Collections.Specialized.NameValueCollection";

  private static readonly Regex RouteParam = new(@"\{(\w+)\s*:?(\w+(\(\d+\))?)\}?", RegexOptions.Compiled);

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Marker attributes go into the consumer's RootNamespace (Moxy parity).
    IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider.Select(
      static (p, _) => SanitizeNamespace(p.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? ns) && !string.IsNullOrWhiteSpace(ns)
        ? ns!
        : "GeneratedMixins"));

    context.RegisterSourceOutput(rootNamespace, static (spc, ns) =>
      spc.AddSource("ContractsMixinAttributes.g.cs", SourceText.From(BuildAttributes(ns), Encoding.UTF8)));

    IncrementalValuesProvider<Target> targets = context.SyntaxProvider.CreateSyntaxProvider(
      predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
      transform: static (ctx, _) => GetTarget((ClassDeclarationSyntax)ctx.Node))
      .Where(static t => t is not null)
      .Select(static (t, _) => t!.Value);

    context.RegisterSourceOutput(targets, static (spc, t) =>
    {
      foreach (Part part in t.Parts)
        spc.AddSource($"{t.HintBase}.{part.Kind}.g.cs", SourceText.From(Wrap(t, part), Encoding.UTF8));
    });
  }

  private readonly struct Part
  {
    public Part(string kind, string? baseType, string body) { Kind = kind; BaseType = baseType; Body = body; }
    public string Kind { get; }
    public string? BaseType { get; }
    public string Body { get; }
  }

  private readonly struct Target
  {
    public Target(string ns, IReadOnlyList<string> containers, string className, string hintBase, IReadOnlyList<Part> parts)
    { Namespace = ns; Containers = containers; ClassName = className; HintBase = hintBase; Parts = parts; }
    public string Namespace { get; }
    public IReadOnlyList<string> Containers { get; }   // outer -> inner, excluding the target class
    public string ClassName { get; }
    public string HintBase { get; }
    public IReadOnlyList<Part> Parts { get; }
  }

  private static Target? GetTarget(ClassDeclarationSyntax cls)
  {
    var parts = new List<Part>();
    foreach (AttributeSyntax attr in cls.AttributeLists.SelectMany(static l => l.Attributes))
    {
      string name = StripAttribute(attr.Name.ToString());
      switch (name)
      {
        case RouteName:
          Part? route = BuildRoute(attr);
          if (route is not null) parts.Add(route.Value);
          break;
        case AuthName:
          parts.Add(new Part(AuthName, "global::TimeWarp.Foundation.Features.IAuthApiRequest", AuthBody()));
          break;
        case OpenDataName:
          parts.Add(new Part(OpenDataName, "global::TimeWarp.Foundation.Features.IOpenDataQueryParameters", OpenDataBody()));
          break;
      }
    }

    if (parts.Count == 0) return null;

    // Namespace + containing type chain (outer -> inner), from syntax.
    string? ns = null;
    var containers = new List<string>();
    for (SyntaxNode? p = cls.Parent; p is not null; p = p.Parent)
    {
      switch (p)
      {
        case TypeDeclarationSyntax t:
          containers.Insert(0, t.Identifier.Text);
          break;
        case BaseNamespaceDeclarationSyntax n:
          ns = n.Name.ToString();
          break;
      }
    }

    if (ns is null) return null;

    string hint = ns + "." + string.Join(".", containers.Concat(new[] { cls.Identifier.Text }));
    return new Target(ns, containers, cls.Identifier.Text, hint, parts);
  }

  private static Part? BuildRoute(AttributeSyntax attr)
  {
    if (attr.ArgumentList is null) return null;

    string? route = null;
    string? verb = null;
    foreach (AttributeArgumentSyntax arg in attr.ArgumentList.Arguments)
    {
      switch (arg.Expression)
      {
        case LiteralExpressionSyntax lit when lit.Token.Value is string s:
          route = s;
          break;
        case MemberAccessExpressionSyntax member:
          verb = member.Name.Identifier.Text;   // HttpVerb.Get -> Get
          break;
      }
    }

    if (route is null || verb is null) return null;

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

      string paramName = m.Groups[1].Value;
      string constraint = m.Groups[2].Value;
      string lower = constraint.ToLowerInvariant();
      string format = lower == "datetime" ? ":yyyy-MM-dd" : string.Empty;

      string csType =
        lower == "guid" ? "Guid" :
        lower == "datetime" ? "DateTime" :
        lower == "alpha" || lower == "required" || lower.StartsWith("minlength", System.StringComparison.Ordinal) || lower.StartsWith("maxlength", System.StringComparison.Ordinal)
          || lower.StartsWith("range", System.StringComparison.Ordinal) || lower.StartsWith("regex", System.StringComparison.Ordinal) || lower.StartsWith("length", System.StringComparison.Ordinal) ? "string" :
        lower.StartsWith("min", System.StringComparison.Ordinal) || lower.StartsWith("max", System.StringComparison.Ordinal) ? "int" :
        constraint;

      formatParts.Add("{" + paramName + format + "}");
      parameters.Add((csType, paramName));
      urlSegments.Add(lower != "string" ? "{" + paramName + ":" + lower + "}" : "{" + paramName + "}");
    }

    string routeConst = string.Join("/", urlSegments);
    string formatString = string.Join("/", formatParts);

    var sb = new StringBuilder();
    sb.Append("    public const string RouteTemplate = \"").Append(routeConst).Append("\";\n");
    sb.Append("    public ").Append(Verb).Append(" GetHttpVerb() => ").Append(Verb).Append('.').Append(verb).Append(";\n");
    if (parameters.Count > 0)
    {
      string sig = string.Join(", ", parameters.Select(static p => p.Type + " " + p.Name));
      sb.Append("    public string GetRoute(").Append(sig).Append(") => global::System.FormattableString.Invariant($\"").Append(formatString).Append("\");\n");
    }

    sb.Append("    public string GetRoute() => global::System.FormattableString.Invariant($\"").Append(formatString).Append("\");\n");
    foreach ((string type, string name) in parameters)
      sb.Append("    public ").Append(type).Append(' ').Append(name).Append(" { get; set; }\n");

    return new Part(RouteName, null, sb.ToString());
  }

  private static string AuthBody() =>
    "    public global::System.Guid UserId { get; set; }\n" +
    "    private " + Nvc + " GetAuthQueryParameters() =>\n" +
    "      new " + Nvc + "\n" +
    "      {\n" +
    "        { nameof(UserId), UserId.ToString() }\n" +
    "      };\n";

  private static string OpenDataBody() =>
    "    public int? Top { get; set; }\n" +
    "    public int? Skip { get; set; }\n" +
    "    public string? Filter { get; set; }\n" +
    "    public string? OrderBy { get; set; }\n" +
    "    public bool ReturnTotalCount { get; set; }\n" +
    "    private " + Nvc + " GetOpenDataQueryParameters() =>\n" +
    "      new " + Nvc + "\n" +
    "      {\n" +
    "        { nameof(Top), Top?.ToString() },\n" +
    "        { nameof(Skip), Skip?.ToString() },\n" +
    "        { nameof(Filter), Filter },\n" +
    "        { nameof(OrderBy), OrderBy },\n" +
    "        { nameof(ReturnTotalCount), ReturnTotalCount.ToString() }\n" +
    "      };\n";

  private static string Wrap(Target t, Part part)
  {
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n#nullable enable\n");
    sb.Append("namespace ").Append(t.Namespace).Append(";\n\n");

    int indent = 0;
    foreach (string container in t.Containers)
    {
      sb.Append(Indent(indent)).Append("partial class ").Append(container).Append('\n');
      sb.Append(Indent(indent)).Append("{\n");
      indent++;
    }

    sb.Append(Indent(indent)).Append("partial class ").Append(t.ClassName);
    if (part.BaseType is not null) sb.Append(" : ").Append(part.BaseType);
    sb.Append('\n').Append(Indent(indent)).Append("{\n");
    sb.Append(part.Body);
    sb.Append(Indent(indent)).Append("}\n");

    for (int i = t.Containers.Count - 1; i >= 0; i--)
    {
      indent--;
      sb.Append(Indent(indent)).Append("}\n");
    }

    return sb.ToString();
  }

  private static string Indent(int level) => new(' ', level * 2);

  // RootNamespace can default to a hyphenated project name (e.g. "foundation-contracts"), which is
  // not a valid namespace. Replace any char that isn't a letter/digit/'.'/'_' with '_'.
  private static string SanitizeNamespace(string ns)
  {
    var sb = new StringBuilder(ns.Length);
    foreach (char c in ns)
      sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' ? c : '_');
    return sb.ToString();
  }

  private static string StripAttribute(string name)
  {
    int dot = name.LastIndexOf('.');
    if (dot >= 0) name = name.Substring(dot + 1);
    return name.EndsWith("Attribute", System.StringComparison.Ordinal) ? name.Substring(0, name.Length - "Attribute".Length) : name;
  }

  private static string BuildAttributes(string ns)
  {
    const string usage = "[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true)]";
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("namespace ").Append(ns).Append("\n{\n");
    sb.Append("    ").Append(usage).Append('\n');
    sb.Append("    internal sealed class RouteMixinAttribute : System.Attribute\n    {\n");
    sb.Append("        public string RouteTemplate { get; }\n");
    sb.Append("        public ").Append(Verb).Append(" HttpVerb { get; }\n");
    sb.Append("        public RouteMixinAttribute(string RouteTemplate, ").Append(Verb).Append(" HttpVerb)\n");
    sb.Append("        {\n            this.RouteTemplate = RouteTemplate;\n            this.HttpVerb = HttpVerb;\n        }\n    }\n\n");
    sb.Append("    ").Append(usage).Append('\n');
    sb.Append("    internal sealed class IAuthApiRequestMixinAttribute : System.Attribute { }\n\n");
    sb.Append("    ").Append(usage).Append('\n');
    sb.Append("    internal sealed class IOpenDataQueryParametersMixinAttribute : System.Attribute { }\n");
    sb.Append("}\n");
    return sb.ToString();
  }
}
