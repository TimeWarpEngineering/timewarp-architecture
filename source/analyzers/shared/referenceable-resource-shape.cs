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
// The query must carry [EndpointAuthorize(Policy = ...)]: the list runs as the signed-in user, and
// its Policy is how the SPA filters the type list. Roles alone is rejected because the SPA filter
// reads only Policy, so a Roles-gated type would be offered to users the server then refuses.
// Read also rejects every shape the generated ResourceReferenceType.Create call cannot compile:
// Query, Response and item type must be public (with public containing types), Query must
// implement IApiRequest, have a public parameterless constructor and no required member besides
// Search (and a stamped UserId), Response must be a reference type, and a stamped UserId needs a
// public setter.
// UserId: for an IAuthApiRequest query the generator stamps a fresh Guid, not the signed-in
// user's id (FetchCredentials resolves the real one; the generated search does not). The handler
// of a referenceable IAuthApiRequest query must therefore not rely on the request UserId; it must
// resolve the user from the authenticated principal, as GetCredentials does.
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

    string? policy = authorize.NamedArguments.FirstOrDefault(static n => n.Key == "Policy").Value.Value as string;
    if (string.IsNullOrWhiteSpace(policy))
    {
      problem = "[EndpointAuthorize] has no Policy; the SPA filters referenceable types by Policy, so set Policy (Roles alone is not read)";
      return null;
    }

    INamedTypeSymbol? query = contract.GetTypeMembers("Query").FirstOrDefault();
    if (query is null)
    {
      problem = "the contract has no nested Query";
      return null;
    }

    if (!IsPublic(query))
    {
      problem = "Query (and every containing type) must be public";
      return null;
    }

    if (!query.AllInterfaces.Any(static i => i.Name == "IApiRequest"))
    {
      problem = "Query does not implement IApiRequest";
      return null;
    }

    if (!query.IsValueType
      && (query.IsAbstract
        || !query.InstanceConstructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)))
    {
      problem = "Query has no public parameterless constructor";
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

    bool isAuthRequest = query.AllInterfaces.Any(static i => i.Name == "IAuthApiRequest");
    IPropertySymbol? userId = isAuthRequest ? FindProperty(query, "UserId") : null;
    if (userId is { SetMethod: not { DeclaredAccessibility: Accessibility.Public } })
    {
      problem = "Query is an IAuthApiRequest whose UserId has no public setter";
      return null;
    }

    string? required = RequiredMembers(query)
      .FirstOrDefault(name => name != "Search" && !(userId is not null && name == "UserId"));
    if (required is not null)
    {
      problem = $"Query has required member '{required}'; only Search may be required";
      return null;
    }

    INamedTypeSymbol? response = contract.GetTypeMembers("Response").FirstOrDefault();
    if (response is null)
    {
      problem = "the contract has no nested Response";
      return null;
    }

    if (!response.IsReferenceType)
    {
      problem = "Response must be a class or record class";
      return null;
    }

    if (!IsPublic(response))
    {
      problem = "Response (and every containing type) must be public";
      return null;
    }

    IPropertySymbol? items = FindProperty(response, itemsProperty);
    ITypeSymbol? itemType = items is null ? null : ElementType(items.Type);
    if (itemType is null)
    {
      problem = $"Response has no public enumerable '{itemsProperty}' property (set ItemsProperty)";
      return null;
    }

    if (!IsPublic(itemType))
    {
      problem = $"item type {itemType.Name} (and every containing type) must be public";
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
      Policy = policy,
      ContractType = contract.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      QueryType = query.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ResponseType = response.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ItemType = itemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      ItemsProperty = itemsProperty,
      IdProperty = idProperty,
      IdTypeName = id.Type.Name,
      LabelProperty = labelProperty,
      QuerySetsUserId = userId is not null,
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

  private static bool IsPublic(ITypeSymbol type)
  {
    if (type is IArrayTypeSymbol array)
    {
      return IsPublic(array.ElementType);
    }

    if (type is ITypeParameterSymbol)
    {
      return false;
    }

    for (ITypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      if (current.DeclaredAccessibility != Accessibility.Public)
      {
        return false;
      }
    }

    return type is not INamedTypeSymbol { IsGenericType: true } generic
      || generic.TypeArguments.All(IsPublic);
  }

  private static IEnumerable<string> RequiredMembers(INamedTypeSymbol type)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      foreach (ISymbol member in current.GetMembers())
      {
        bool isRequired = member switch
        {
          IPropertySymbol property => property.IsRequired,
          IFieldSymbol field => field.IsRequired,
          _ => false,
        };
        if (isRequired)
        {
          yield return member.Name;
        }
      }
    }
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
