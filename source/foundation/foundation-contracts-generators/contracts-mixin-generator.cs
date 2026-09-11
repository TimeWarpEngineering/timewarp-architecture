#region Purpose
// Roslyn replacement for the three foundation-contracts Moxy mixins (task 053-001), renamed from
// [RouteMixin]/[IAuthApiRequestMixin]/[IOpenDataQueryParametersMixin] in task 053-002:
//   [ApiRoute(route, HttpVerb)]  [AuthApiRequest]  [OpenDataQueryParameters]
// Emits the marker attributes as public types in TimeWarp.Foundation.Features (same namespace as
// IAuthApiRequest / HttpVerb) via RegisterPostInitializationOutput, then discovers applications with
// ForAttributeWithMetadataName. FastEndpoint/ingress match the same FQN — not per-consumer
// RootNamespace internals, and not simple-name string match.
#endregion

#region Design
// Attributes are public in TimeWarp.Foundation.Features so ForAttributeWithMetadataName can key a
// metadata name that does not vary with the generated app's RootNamespace (task 115 sourceName
// rewrite leaves TimeWarp.Foundation.* intact) and a foreign ApiRouteAttribute cannot collide.
// Discovery is one ForAttributeWithMetadataName per metadata name; the predicate requires a partial
// class (records/structs are skipped). The three pipelines are collected and merged into an
// equatable record struct Target (ImmutableArray parts) so trivia-only re-transforms do not re-emit,
// and one `{fqn}.g.cs` per type avoids AllowMultiple hint-name collisions. Extra same-kind
// attributes on one type are ignored after the first successful Part (first-wins) so that merged
// file stays compilable; multi-route member APIs are out of scope here (053-006).
// Route tokens are `{Name}` or `{Name:constraint}`. The colon is the only delimiter that starts a
// type/constraint token (task 053-003). An optional colon (`:?`) plus a required second `\w+` stole
// the last letter of identifiers that look like types (`{Date}` → Dat + e, `{LocationId}` → LocationI + d).
// Recognized constraint tokens and the C# type they emit:
//   guid → Guid; datetime → DateTime (GetRoute formats yyyy-MM-dd);
//   string / alpha / required / minlength* / maxlength* / length* / range* / regex* → string;
//   min* / max* (that did not match minlength/maxlength) → int;
//   any other token is used as the C# type as-is (int, long, bool, …);
//   omitted constraint → string.
// Constraint args are the parenthesized-digits form only (`min(1)`, `minlength(3)`). Multiple
// constraints, comma args, catch-alls, and `{name=default}` are not parsed.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed partial class ContractsMixinGenerator : IIncrementalGenerator
{
  internal const string AttributeNamespace = "TimeWarp.Foundation.Features";
  internal const string ApiRouteAttributeMetadataName = AttributeNamespace + ".ApiRouteAttribute";
  internal const string AuthApiRequestAttributeMetadataName = AttributeNamespace + ".AuthApiRequestAttribute";
  internal const string OpenDataQueryParametersAttributeMetadataName = AttributeNamespace + ".OpenDataQueryParametersAttribute";

  private const string RouteName = "ApiRoute";
  private const string AuthName = "AuthApiRequest";
  private const string OpenDataName = "OpenDataQueryParameters";
  private const string Verb = "global::TimeWarp.Foundation.Features.HttpVerb";
  private const string Nvc = "global::System.Collections.Specialized.NameValueCollection";

  // Colon is required before a constraint so `{Date}` / `{LocationId}` keep their full identifier.
  [GeneratedRegex(@"\{(\w+)(?:\s*:\s*(\w+(?:\(\d+\))?))?\}")]
  private static partial Regex RouteParam();

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(static ctx =>
      ctx.AddSource("ContractsMixinAttributes.g.cs", SourceText.From(BuildAttributes(), Encoding.UTF8)));

    IncrementalValuesProvider<Target> routes = CreateMixinProvider(
      context, ApiRouteAttributeMetadataName, static (ctx, _) => Transform(ctx, RouteName));
    IncrementalValuesProvider<Target> auths = CreateMixinProvider(
      context, AuthApiRequestAttributeMetadataName, static (ctx, _) => Transform(ctx, AuthName));
    IncrementalValuesProvider<Target> openData = CreateMixinProvider(
      context, OpenDataQueryParametersAttributeMetadataName, static (ctx, _) => Transform(ctx, OpenDataName));

    IncrementalValuesProvider<Target> merged = routes.Collect()
      .Combine(auths.Collect())
      .Combine(openData.Collect())
      .SelectMany(static (tuple, _) => MergeTargets(tuple.Left.Left, tuple.Left.Right, tuple.Right));

    context.RegisterSourceOutput(merged, static (spc, t) =>
      spc.AddSource($"{t.HintBase}.g.cs", SourceText.From(Wrap(t), Encoding.UTF8)));
  }

  private static IncrementalValuesProvider<Target> CreateMixinProvider(
    IncrementalGeneratorInitializationContext context,
    string metadataName,
    Func<GeneratorAttributeSyntaxContext, CancellationToken, Target?> transform)
  {
    return context.SyntaxProvider
      .ForAttributeWithMetadataName(
        metadataName,
        predicate: static (node, _) => IsPartialClass(node),
        transform: transform)
      .Where(static t => t is not null)
      .Select(static (t, _) => t!.Value);
  }

  private static bool IsPartialClass(SyntaxNode node) =>
    node is ClassDeclarationSyntax classDeclaration
    && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);

  private readonly record struct Part(string Kind, string? BaseType, string Body);

  // Equatable so RegisterSourceOutput skips when parts/identity are unchanged (IReadOnlyList used
  // reference equality and re-emitted on every transform). ImmutableArray.Equals is also by
  // backing-array reference, so this type SequenceEquals the arrays. Containers are outer -> inner,
  // excluding the target class.
  private readonly record struct Target(
    string Namespace,
    ImmutableArray<string> Containers,
    string ClassName,
    string HintBase,
    ImmutableArray<Part> Parts)
  {
    public bool Equals(Target other) =>
      Namespace == other.Namespace
      && ClassName == other.ClassName
      && HintBase == other.HintBase
      && Containers.SequenceEqual(other.Containers)
      && Parts.SequenceEqual(other.Parts);

    public override int GetHashCode()
    {
      HashCode hashCode = new();
      hashCode.Add(Namespace);
      hashCode.Add(ClassName);
      hashCode.Add(HintBase);
      foreach (string container in Containers)
        hashCode.Add(container);
      foreach (Part part in Parts)
        hashCode.Add(part);
      return hashCode.ToHashCode();
    }
  }

  private static ImmutableArray<Target> MergeTargets(
    ImmutableArray<Target> routes,
    ImmutableArray<Target> auths,
    ImmutableArray<Target> openData)
  {
    Dictionary<string, Target> byHint = new(StringComparer.Ordinal);
    AppendTargets(byHint, routes);
    AppendTargets(byHint, auths);
    AppendTargets(byHint, openData);

    ImmutableArray<Target>.Builder builder = ImmutableArray.CreateBuilder<Target>(byHint.Count);
    foreach (Target target in byHint.Values.OrderBy(static t => t.HintBase, StringComparer.Ordinal))
      builder.Add(OrderParts(target));
    return builder.ToImmutable();
  }

  private static void AppendTargets(Dictionary<string, Target> byHint, ImmutableArray<Target> targets)
  {
    foreach (Target target in targets)
    {
      if (byHint.TryGetValue(target.HintBase, out Target existing))
      {
        byHint[target.HintBase] = existing with { Parts = existing.Parts.AddRange(target.Parts) };
      }
      else
      {
        byHint[target.HintBase] = target;
      }
    }
  }

  private static Target OrderParts(Target target)
  {
    if (target.Parts.Length <= 1)
      return target;

    ImmutableArray<Part> ordered = [.. target.Parts.OrderBy(static p => KindOrder(p.Kind))];
    return target with { Parts = ordered };
  }

  private static int KindOrder(string kind) => kind switch
  {
    RouteName => 0,
    AuthName => 1,
    OpenDataName => 2,
    _ => 3
  };

  private static Target? Transform(GeneratorAttributeSyntaxContext context, string kind)
  {
    if (context.TargetSymbol is not INamedTypeSymbol symbol)
      return null;

    ImmutableArray<Part>.Builder parts = ImmutableArray.CreateBuilder<Part>();
    foreach (AttributeData attr in context.Attributes)
    {
      Part? part = kind switch
      {
        RouteName => BuildRoute(attr),
        AuthName => new Part(AuthName, "global::TimeWarp.Foundation.Features.IAuthApiRequest", AuthBody()),
        OpenDataName => new Part(OpenDataName, "global::TimeWarp.Foundation.Features.IOpenDataQueryParameters", OpenDataBody()),
        _ => null
      };
      if (part is not null)
      {
        parts.Add(part.Value);
        break;
      }
    }

    if (parts.Count == 0)
      return null;

    return ToTarget(symbol, parts.ToImmutable());
  }

  private static Target? ToTarget(INamedTypeSymbol symbol, ImmutableArray<Part> parts)
  {
    if (symbol.ContainingNamespace is not { IsGlobalNamespace: false } containingNamespace)
      return null;

    string ns = containingNamespace.ToDisplayString();
    List<string> containers = [];
    for (INamedTypeSymbol? container = symbol.ContainingType; container is not null; container = container.ContainingType)
      containers.Insert(0, container.Name);

    string className = symbol.Name;
    ImmutableArray<string> containerArray = [.. containers];
    string hint = ns + "." + string.Join(".", containerArray.Add(className));
    return new Target(ns, containerArray, className, hint, parts);
  }

  private static Part? BuildRoute(AttributeData attr)
  {
    if (attr.ConstructorArguments.Length < 2)
      return null;

    string? route = attr.ConstructorArguments[0].Value as string;
    string? verb = ResolveVerbName(attr.ConstructorArguments[1]);
    if (route is null || verb is null)
      return null;

    var urlSegments = new List<string>();
    var formatParts = new List<string>();
    var parameters = new List<(string Type, string Name)>();

    foreach (string segment in route.Split('/'))
    {
      Match m = RouteParam().Match(segment);
      if (!m.Success)
      {
        urlSegments.Add(segment);
        formatParts.Add(segment);
        continue;
      }

      string paramName = m.Groups[1].Value;
      string constraint = m.Groups[2].Success ? m.Groups[2].Value : "string";
      string lower = constraint.ToLowerInvariant();
      string format = lower == "datetime" ? ":yyyy-MM-dd" : string.Empty;
      string csType = MapConstraintToClrType(constraint);

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

  private static string? ResolveVerbName(TypedConstant verbArgument)
  {
    if (verbArgument.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
    {
      IFieldSymbol? field = enumType.GetMembers().OfType<IFieldSymbol>()
        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, verbArgument.Value));
      if (field is not null)
        return field.Name;
    }

    return verbArgument.Value?.ToString();
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

  private static string Wrap(Target t)
  {
    StringBuilder sb = new();
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
    List<string> baseTypes = [];
    foreach (Part part in t.Parts)
    {
      if (part.BaseType is not null && !baseTypes.Contains(part.BaseType, StringComparer.Ordinal))
        baseTypes.Add(part.BaseType);
    }

    if (baseTypes.Count > 0)
      sb.Append(" : ").Append(string.Join(", ", baseTypes));
    sb.Append('\n').Append(Indent(indent)).Append("{\n");
    foreach (Part part in t.Parts)
      sb.Append(part.Body);
    sb.Append(Indent(indent)).Append("}\n");

    for (int i = t.Containers.Length - 1; i >= 0; i--)
    {
      indent--;
      sb.Append(Indent(indent)).Append("}\n");
    }

    return sb.ToString();
  }

  private static string Indent(int level) => new(' ', level * 2);

  private static string MapConstraintToClrType(string constraint)
  {
    if (string.IsNullOrEmpty(constraint))
      return "string";

    string lower = constraint.ToLowerInvariant();
    if (lower == "guid")
      return "Guid";
    if (lower == "datetime")
      return "DateTime";
    if (lower is "string" or "alpha" or "required"
      || lower.StartsWith("minlength", System.StringComparison.Ordinal)
      || lower.StartsWith("maxlength", System.StringComparison.Ordinal)
      || lower.StartsWith("length", System.StringComparison.Ordinal)
      || lower.StartsWith("range", System.StringComparison.Ordinal)
      || lower.StartsWith("regex", System.StringComparison.Ordinal))
    {
      return "string";
    }

    if (lower.StartsWith("min", System.StringComparison.Ordinal) || lower.StartsWith("max", System.StringComparison.Ordinal))
      return "int";

    return constraint;
  }

  private static string BuildAttributes()
  {
    const string usage = "[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true)]";
    var sb = new StringBuilder();
    sb.Append("// <auto-generated/>\n");
    sb.Append("#nullable enable\n");
    sb.Append("namespace ").Append(AttributeNamespace).Append(";\n\n");
    sb.Append(usage).Append('\n');
    sb.Append("public sealed class ApiRouteAttribute : System.Attribute\n{\n");
    sb.Append("  public string RouteTemplate { get; }\n");
    sb.Append("  public ").Append(Verb).Append(" HttpVerb { get; }\n");
    sb.Append("  public ApiRouteAttribute(string RouteTemplate, ").Append(Verb).Append(" HttpVerb)\n");
    sb.Append("  {\n    this.RouteTemplate = RouteTemplate;\n    this.HttpVerb = HttpVerb;\n  }\n");
    sb.Append("}\n\n");
    sb.Append(usage).Append('\n');
    sb.Append("public sealed class AuthApiRequestAttribute : System.Attribute { }\n\n");
    sb.Append(usage).Append('\n');
    sb.Append("public sealed class OpenDataQueryParametersAttribute : System.Attribute { }\n");
    return sb.ToString();
  }
}
