#region Purpose
// Reads and validates a [ReferenceableResource] contract: prefix, query search, item id and label.
#endregion

#region Design
// Linked into both analyzer packages, like HostedRouteDiscovery. ReferenceableResourceAnalyzer
// (TWA0033) reports Problem on the contract; ResourceReferenceRegistryGenerator emits only
// contracts that read cleanly, so a bad shape is a warning in contracts and absent from the
// registry, never a broken generated call. Attributes match by simple name: the attribute lives
// in the attributes package, and [TypedId] is an internal attribute generated into each assembly.
// A referenced-assembly typed id is also recognized by its generated surface (a struct with a
// Guid Value and a static From(Guid)), the same fallback TypedIdSourceGenerator uses.
// Ids may be Guid because a slice's [TypedId] can live in its domain project, which contracts
// cannot reference; IdTypeName records which it is for the agent.
// The query must carry [EndpointAuthorize]: the list runs as the signed-in user, and its Policy
// is how the SPA filters the type list. A null Policy means any signed-in user.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

internal sealed class ReferenceableResourceShape
{
  public const string AttributeSimpleName = "ReferenceableResourceAttribute";
  public const int MaxPrefixLength = 32;

  public string Prefix { get; private set; } = "";
  public string DisplayName { get; private set; } = "";
  public string? Policy { get; private set; }
  public string ContractType { get; private set; } = "";
  public string QueryType { get; private set; } = "";
  public string ResponseType { get; private set; } = "";
  public string ItemType { get; private set; } = "";
  public string ItemsProperty { get; private set; } = "";
  public string IdProperty { get; private set; } = "";
  public string IdTypeName { get; private set; } = "";
  public string LabelProperty { get; private set; } = "";
  public bool QuerySetsUserId { get; private set; }

  public static AttributeData? FindAttribute(INamedTypeSymbol type) =>
    type.GetAttributes().FirstOrDefault(static a => a.AttributeClass?.Name == AttributeSimpleName);

  /// <summary>Reads the contract; returns null and a problem sentence when the shape is wrong.</summary>
  public static ReferenceableResourceShape? Read(INamedTypeSymbol contract, AttributeData attribute, out string? problem)
  {
    problem = null;
    string prefix = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string ?? "" : "";
    string displayName = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value as string ?? "" : "";
    string idProperty = Named(attribute, "IdProperty") ?? "";
    string labelProperty = Named(attribute, "LabelProperty") ?? "";
    string itemsProperty = Named(attribute, "ItemsProperty") ?? "Items";

    if (!IsValidPrefix(prefix))
    {
      problem = $"prefix '{prefix}' must be 1-{MaxPrefixLength} lowercase letters, digits or '-', starting with a letter";
      return null;
    }

    if (string.IsNullOrWhiteSpace(displayName))
    {
      problem = "DisplayName is empty";
      return null;
    }

    if (!contract.GetAttributes().Any(static a => a.AttributeClass?.Name == HostedRouteDiscovery.ApiEndpointAttributeSimpleName))
    {
      problem = "the contract has no [ApiEndpoint]";
      return null;
    }

    AttributeData? authorize = contract.GetAttributes()
      .FirstOrDefault(static a => a.AttributeClass?.Name == "EndpointAuthorizeAttribute");
    if (authorize is null)
    {
      problem = "the contract has no [EndpointAuthorize]; a referenceable list runs as the signed-in user";
      return null;
    }

    INamedTypeSymbol? query = contract.GetTypeMembers("Query").FirstOrDefault();
    if (query is null)
    {
      problem = "the contract has no nested Query";
      return null;
    }

    IPropertySymbol? search = FindProperty(query, "Search");
    if (search is null
      || search.Type.SpecialType != SpecialType.System_String
      || search.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
    {
      problem = "Query has no public settable string Search property";
      return null;
    }

    INamedTypeSymbol? response = contract.GetTypeMembers("Response").FirstOrDefault();
    if (response is null)
    {
      problem = "the contract has no nested Response";
      return null;
    }

    IPropertySymbol? items = FindProperty(response, itemsProperty);
    ITypeSymbol? itemType = items is null ? null : ElementType(items.Type);
    if (itemType is null)
    {
      problem = $"Response has no public enumerable '{itemsProperty}' property (set ItemsProperty)";
      return null;
    }

    IPropertySymbol? id = idProperty.Length == 0 ? null : FindProperty(itemType, idProperty);
    if (id is null)
    {
      problem = $"item type {itemType.Name} has no public IdProperty '{idProperty}'";
      return null;
    }

    if (!IsGuid(id.Type) && !IsTypedId(id.Type))
    {
      problem = $"IdProperty '{idProperty}' is {id.Type.Name}; it must be a [TypedId] struct or Guid";
      return null;
    }

    IPropertySymbol? label = labelProperty.Length == 0 ? null : FindProperty(itemType, labelProperty);
    if (label is null || label.Type.SpecialType != SpecialType.System_String)
    {
      problem = $"item type {itemType.Name} has no public string LabelProperty '{labelProperty}'";
      return null;
    }

    return new ReferenceableResourceShape
    {
      Prefix = prefix,
      DisplayName = displayName,
      Policy = authorize.NamedArguments.FirstOrDefault(static n => n.Key == "Policy").Value.Value as string,
      ContractType = contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      QueryType = query.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ResponseType = response.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ItemType = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ItemsProperty = itemsProperty,
      IdProperty = idProperty,
      IdTypeName = id.Type.Name,
      LabelProperty = labelProperty,
      QuerySetsUserId = query.AllInterfaces.Any(static i => i.Name == "IAuthApiRequest")
        && FindProperty(query, "UserId") is { SetMethod: not null },
    };
  }

  public static bool IsValidPrefix(string prefix)
  {
    if (prefix.Length == 0 || prefix.Length > MaxPrefixLength || prefix[0] < 'a' || prefix[0] > 'z')
    {
      return false;
    }

    foreach (char character in prefix)
    {
      bool ok = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
      if (!ok)
      {
        return false;
      }
    }

    return true;
  }

  private static string? Named(AttributeData attribute, string name) =>
    attribute.NamedArguments.FirstOrDefault(n => n.Key == name).Value.Value as string;

  private static IPropertySymbol? FindProperty(ITypeSymbol type, string name)
  {
    for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      IPropertySymbol? property = current.GetMembers(name).OfType<IPropertySymbol>()
        .FirstOrDefault(static p => p is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null });
      if (property is not null)
      {
        return property;
      }
    }

    return null;
  }

  private static ITypeSymbol? ElementType(ITypeSymbol type)
  {
    if (type.SpecialType == SpecialType.System_String)
    {
      return null;
    }

    if (type is IArrayTypeSymbol array)
    {
      return array.ElementType;
    }

    IEnumerable<INamedTypeSymbol> candidates = type is INamedTypeSymbol named
      ? new[] { named }.Concat(type.AllInterfaces)
      : type.AllInterfaces;
    INamedTypeSymbol? enumerable = candidates.FirstOrDefault(static i =>
      i.IsGenericType
      && i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
    return enumerable?.TypeArguments[0];
  }

  private static bool IsGuid(ITypeSymbol type) =>
    type.Name == "Guid" && type.ContainingNamespace?.ToDisplayString() == "System";

  private static bool IsTypedId(ITypeSymbol type)
  {
    if (!type.IsValueType)
    {
      return false;
    }

    if (type.GetAttributes().Any(static a => a.AttributeClass?.Name is "TypedIdAttribute" or "TypedId"))
    {
      return true;
    }

    bool guidValue = type.GetMembers("Value").OfType<IPropertySymbol>().Any(static p => IsGuid(p.Type));
    bool from = type.GetMembers("From").OfType<IMethodSymbol>()
      .Any(static m => m.IsStatic && m.Parameters.Length == 1 && IsGuid(m.Parameters[0].Type));
    return guidValue && from;
  }
}
