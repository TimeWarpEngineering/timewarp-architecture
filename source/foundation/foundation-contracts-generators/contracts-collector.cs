#region Purpose
// Collector for ContractsGenerator: turns one attributed partial class into a ContractTarget,
// reads [Operation] and its hint attributes, parses route templates, and merges the three
// attribute pipelines into one target per type.
#endregion

namespace TimeWarp.Foundation.Contracts.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>Builds ContractTarget values from attributed partial contract classes.</summary>
internal static partial class ContractsCollector
{
  internal const string RouteName = "ApiRoute";
  internal const string AuthName = "AuthApiRequest";
  internal const string OpenDataName = "OpenDataQueryParameters";
  internal const string OperationPartKind = "Operation";
  private const string OperationAttributeFullName = "TimeWarp.Contracts.OperationAttribute";
  private const string DestructiveAttributeFullName = "TimeWarp.Contracts.DestructiveAttribute";
  private const string OpenWorldAttributeFullName = "TimeWarp.Contracts.OpenWorldAttribute";

  // Colon is required before a constraint so `{Date}` / `{LocationId}` keep their full identifier.
  [GeneratedRegex(@"\{(\w+)(?:\s*:\s*(\w+(?:\(\d+\))?))?\}")]
  private static partial Regex RouteParam();

  internal static ImmutableArray<ContractTarget> MergeTargets(
    ImmutableArray<ContractTarget> routes,
    ImmutableArray<ContractTarget> auths,
    ImmutableArray<ContractTarget> openData)
  {
    Dictionary<string, ContractTarget> byHint = new(StringComparer.Ordinal);
    AppendTargets(byHint, routes);
    AppendTargets(byHint, auths);
    AppendTargets(byHint, openData);

    ImmutableArray<ContractTarget>.Builder builder = ImmutableArray.CreateBuilder<ContractTarget>(byHint.Count);
    foreach (ContractTarget target in byHint.Values.OrderBy(static t => t.HintBase, StringComparer.Ordinal))
      builder.Add(OrderParts(target));
    return builder.ToImmutable();
  }

  private static void AppendTargets(Dictionary<string, ContractTarget> byHint, ImmutableArray<ContractTarget> targets)
  {
    foreach (ContractTarget target in targets)
    {
      if (byHint.TryGetValue(target.HintBase, out ContractTarget existing))
      {
        byHint[target.HintBase] = existing with { Parts = existing.Parts.AddRange(target.Parts) };
      }
      else
      {
        byHint[target.HintBase] = target;
      }
    }
  }

  private static ImmutableArray<ContractPart> DedupeKind(ImmutableArray<ContractPart> parts, string kind)
  {
    bool seen = false;
    ImmutableArray<ContractPart>.Builder builder = ImmutableArray.CreateBuilder<ContractPart>(parts.Length);
    foreach (ContractPart part in parts)
    {
      if (part.Kind == kind)
      {
        if (seen)
          continue;

        seen = true;
      }

      builder.Add(part);
    }

    return builder.ToImmutable();
  }

  private static ContractTarget OrderParts(ContractTarget target)
  {
    ImmutableArray<ContractPart> parts = DedupeKind(WithAuthQueryHelperWhenOpenData(target.Parts), OperationPartKind);
    if (parts.Length <= 1)
      return target with { Parts = parts };

    ImmutableArray<ContractPart> ordered = [.. parts.OrderBy(static p => KindOrder(p.Kind))];
    return target with { Parts = ordered };
  }

  private static ImmutableArray<ContractPart> WithAuthQueryHelperWhenOpenData(ImmutableArray<ContractPart> parts)
  {
    bool hasOpenData = false;
    bool hasAuth = false;
    foreach (ContractPart part in parts)
    {
      if (part.Kind == OpenDataName)
        hasOpenData = true;
      else if (part.Kind == AuthName)
        hasAuth = true;
    }

    if (!hasOpenData || !hasAuth)
      return parts;

    return [.. parts.Select(static p => p.Kind == AuthName ? p with { Body = ContractsEmitter.AuthBody(includeQueryHelper: true) } : p)];
  }

  private static int KindOrder(string kind) => kind switch
  {
    RouteName => 0,
    AuthName => 1,
    OpenDataName => 2,
    OperationPartKind => 3,
    _ => 4
  };

  internal static ContractTarget? Transform(GeneratorAttributeSyntaxContext context, string kind)
  {
    if (context.TargetSymbol is not INamedTypeSymbol symbol)
      return null;

    ImmutableArray<ContractPart>.Builder parts = ImmutableArray.CreateBuilder<ContractPart>();
    foreach (AttributeData attr in context.Attributes)
    {
      ContractPart? part = kind switch
      {
        RouteName => BuildRoute(attr),
        AuthName => new ContractPart(AuthName, "global::TimeWarp.Foundation.Features.IAuthApiRequest", ContractsEmitter.AuthBody(WantsAuthQueryHelper(context))),
        OpenDataName => new ContractPart(OpenDataName, "global::TimeWarp.Foundation.Features.IOpenDataQueryParameters", ContractsEmitter.OpenDataBody()),
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

    AppendOperation(symbol, parts);
    return ToTarget(symbol, parts.ToImmutable());
  }

  private static void AppendOperation(INamedTypeSymbol symbol, ImmutableArray<ContractPart>.Builder parts)
  {
    AttributeData? operation = null;
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (!IsOperationAttribute(attribute))
        continue;

      operation = attribute;
      break;
    }

    if (operation is null)
      return;

    string? name = operation.ConstructorArguments.Length > 0
      ? operation.ConstructorArguments[0].Value as string
      : null;
    if (string.IsNullOrWhiteSpace(name))
      return;

    string? description = null;
    string? group = null;
    string? title = null;
    foreach (KeyValuePair<string, TypedConstant> named in operation.NamedArguments)
    {
      if (named.Key == "Description")
      {
        description = named.Value.Value as string;
      }
      else if (named.Key == "Group")
      {
        group = named.Value.Value as string;
      }
      else if (named.Key == "Title")
      {
        title = named.Value.Value as string;
      }
    }

    if (string.IsNullOrWhiteSpace(description))
      description = null;

    if (string.IsNullOrWhiteSpace(group))
      group = null;

    if (string.IsNullOrWhiteSpace(title))
      title = null;

    bool? destructive = ReadBoolHint(symbol, DestructiveAttributeFullName);
    bool? openWorld = ReadBoolHint(symbol, OpenWorldAttributeFullName);
    parts.Add(new ContractPart(
      OperationPartKind,
      null,
      ContractsEmitter.OperationBody(name, description, group, title, destructive, openWorld)));
  }

  private static bool IsOperationAttribute(AttributeData attribute) =>
    IsAttributeFullName(attribute, OperationAttributeFullName);

  private static bool IsAttributeFullName(AttributeData attribute, string fullName)
  {
    INamedTypeSymbol? type = attribute.AttributeClass;
    if (type?.ContainingNamespace is not { IsGlobalNamespace: false } containingNamespace)
      return false;

    return containingNamespace.ToDisplayString() + "." + type.Name == fullName;
  }

  /// <summary>
  /// Constructor argument of a tri-state hint attribute. True when the argument is omitted.
  /// Null when the attribute is absent.
  /// </summary>
  private static bool? ReadBoolHint(INamedTypeSymbol symbol, string attributeFullName)
  {
    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (!IsAttributeFullName(attribute, attributeFullName))
        continue;

      return attribute.ConstructorArguments.Length == 0
        || attribute.ConstructorArguments[0].Value is not false;
    }

    return null;
  }

  private static ContractTarget? ToTarget(INamedTypeSymbol symbol, ImmutableArray<ContractPart> parts)
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
    return new ContractTarget(ns, containerArray, className, hint, parts);
  }

  private static ContractPart? BuildRoute(AttributeData attr)
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

    return new ContractPart(RouteName, null, ContractsEmitter.RouteBody(routeConst, verb, formatString, parameters));
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

  private static bool WantsAuthQueryHelper(GeneratorAttributeSyntaxContext context)
  {
    if (HasQueryStringRouteProvider(context))
      return true;

    if (context.TargetSymbol is not INamedTypeSymbol symbol)
      return false;

    foreach (AttributeData attributeData in symbol.GetAttributes())
    {
      INamedTypeSymbol? attributeClass = attributeData.AttributeClass;
      if (attributeClass is null)
      {
        continue;
      }

      if (attributeClass.ContainingNamespace?.ToDisplayString() == ContractsGenerator.AttributeNamespace
        && attributeClass.Name == "OpenDataQueryParametersAttribute")
      {
        return true;
      }
    }

    return false;
  }

  private static bool HasQueryStringRouteProvider(GeneratorAttributeSyntaxContext context)
  {
    if (context.TargetSymbol is INamedTypeSymbol symbol)
    {
      foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
      {
        if (iface.Name == "IQueryStringRouteProvider")
          return true;
      }
    }

    if (context.TargetNode is ClassDeclarationSyntax { BaseList: { } baseList })
    {
      foreach (BaseTypeSyntax baseType in baseList.Types)
      {
        if (baseType.Type.ToString() is string typeName
          && typeName.EndsWith("IQueryStringRouteProvider", StringComparison.Ordinal))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static string MapConstraintToClrType(string constraint)
  {
    if (string.IsNullOrEmpty(constraint))
      return "string";

    string lower = constraint.ToLowerInvariant();
    if (lower == "guid")
      return "global::System.Guid";
    if (lower == "datetime")
      return "global::System.DateTime";
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
}
