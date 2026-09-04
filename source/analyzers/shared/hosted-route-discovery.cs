#region Purpose
// Shared symbol-walk helpers for discovering hosted [ApiEndpoint]/[ApiRoute] operations across
// generators (FastEndpoint, ingress) and convention analyzers (endpoint coverage, policy agreement).
#endregion

#region Design
// Linked into BOTH TimeWarp.Architecture.Generators and TimeWarp.Architecture.Analyzers via
// <Compile Include Link=…> — no ProjectReference between those packages (F-004 / task 131-001).
// Simple-name attribute matching: attributes are emitted into each consumer's RootNamespace, so a
// full metadata-name match would pin one root and break generated apps.
// ClientOnly = outer operation OR nested Query/Command: either placement means "not hosted" for
// generators/ingress and for TWA0006 coverage opt-out.
// TryGetHostedOperation is the generation/ingress gate: [ApiEndpoint] + nested Query|Command +
// [ApiRoute] + !ClientOnly. TryGetRoutedRequest is the coverage gate: any type carrying [ApiRoute]
// (typically nested Query/Command), with ClientOnly checked on self and containing type.
// GetPairedContractAssemblies is the TWA0006/TWA0024 family pairing (web-server ↔ web-contracts):
// a server referencing another family's contracts as a client must not vouch for them.
// GetAllNamespaces / GetAllTypes replace the triplicated private copies that previously drifted.
#endregion

namespace TimeWarp.Architecture.Analyzers;

using System.Collections.Generic;

internal static class HostedRouteDiscovery
{
  public const string ApiEndpointAttributeFullName = "TimeWarp.Architecture.Attributes.ApiEndpointAttribute";
  public const string ApiEndpointAttributeSimpleName = "ApiEndpointAttribute";
  public const string ApiRouteAttributeSimpleName = "ApiRouteAttribute";
  public const string ClientOnlyContractAttributeSimpleName = "ClientOnlyContractAttribute";

  public static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol root)
  {
    yield return root;
    foreach (INamespaceSymbol child in root.GetNamespaceMembers())
    {
      foreach (INamespaceSymbol descendant in GetAllNamespaces(child))
      {
        yield return descendant;
      }
    }
  }

  public static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
  {
    foreach (INamespaceOrTypeSymbol member in root.GetMembers())
    {
      switch (member)
      {
        case INamespaceSymbol ns:
          foreach (INamedTypeSymbol nested in GetAllTypes(ns))
          {
            yield return nested;
          }

          break;
        case INamedTypeSymbol type:
          yield return type;
          foreach (INamedTypeSymbol nested in AllNested(type))
          {
            yield return nested;
          }

          break;
      }
    }
  }

  public static bool HasClientOnly(INamedTypeSymbol symbol)
    => symbol.GetAttributes().Any(static attr =>
      attr.AttributeClass?.Name == ClientOnlyContractAttributeSimpleName);

  /// <summary>
  /// True when the outer operation or its nested Query/Command carries [ClientOnlyContract].
  /// </summary>
  public static bool HasClientOnlyOnOperationOrRequest(INamedTypeSymbol operationType, INamedTypeSymbol? requestType)
    => HasClientOnly(operationType) || (requestType is not null && HasClientOnly(requestType));

  /// <summary>
  /// Hosted generation/ingress shape: outer [ApiEndpoint], nested Query|Command with [ApiRoute],
  /// not ClientOnly on outer or nested.
  /// </summary>
  public static bool TryGetHostedOperation(INamedTypeSymbol type, out HostedOperationInfo info)
  {
    info = default;

    if (!type.GetAttributes().Any(static attr => attr.AttributeClass?.Name == ApiEndpointAttributeSimpleName))
    {
      return false;
    }

    INamedTypeSymbol? requestClass = type.GetTypeMembers()
      .FirstOrDefault(static m => m.Name is "Query" or "Command");
    if (requestClass is null)
    {
      return false;
    }

    if (HasClientOnlyOnOperationOrRequest(type, requestClass))
    {
      return false;
    }

    AttributeData? apiRoute = requestClass.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == ApiRouteAttributeSimpleName);
    if (apiRoute is null || apiRoute.ConstructorArguments.Length < 2)
    {
      return false;
    }

    string? template = apiRoute.ConstructorArguments[0].Value?.ToString();
    if (string.IsNullOrWhiteSpace(template))
    {
      return false;
    }

    string? verb = ResolveHttpVerbName(apiRoute.ConstructorArguments[1]);
    info = new HostedOperationInfo(
      OperationType: type,
      RequestType: requestClass,
      RouteTemplate: template!,
      HttpVerbName: verb,
      ApiRouteAttribute: apiRoute);
    return true;
  }

  /// <summary>
  /// Coverage (TWA0006) shape: a type that itself carries [ApiRoute]. ClientOnly on the type or
  /// its containing type opts out.
  /// </summary>
  public static bool TryGetRoutedRequest(INamedTypeSymbol type, out RoutedRequestInfo info)
  {
    info = default;

    AttributeData? apiRoute = type.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == ApiRouteAttributeSimpleName);
    if (apiRoute is null || apiRoute.ConstructorArguments.Length < 2)
    {
      return false;
    }

    if (HasClientOnly(type))
    {
      return false;
    }

    if (type.ContainingType is INamedTypeSymbol containing && HasClientOnly(containing))
    {
      return false;
    }

    string? template = apiRoute.ConstructorArguments[0].Value?.ToString();
    string? verb = ResolveHttpVerbName(apiRoute.ConstructorArguments[1]);

    info = new RoutedRequestInfo(
      RequestType: type,
      RouteTemplate: template ?? "?",
      HttpVerbName: verb ?? apiRoute.ConstructorArguments[1].Value?.ToString() ?? "?",
      ApiRouteAttribute: apiRoute);
    return true;
  }

  /// <summary>
  /// Resolves an HttpVerb TypedConstant to its enum member name. Returns null when the value
  /// cannot be mapped to a named enum field (fail-closed — callers report TWE007 / skip).
  /// </summary>
  public static string? ResolveHttpVerbName(TypedConstant verbArgument)
  {
    if (verbArgument.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
    {
      IFieldSymbol? field = enumType.GetMembers().OfType<IFieldSymbol>()
        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, verbArgument.Value));
      if (field is not null)
      {
        return field.Name;
      }
    }

    // Non-enum or unresolved constant: only accept known verb name strings, never invent Get.
    string? raw = verbArgument.Value?.ToString();
    if (raw is not null && IsAllowedHttpVerbName(raw))
    {
      return raw;
    }

    return null;
  }

  public static bool IsAllowedHttpVerbName(string httpVerb)
    => httpVerb is "Get" or "Post" or "Put" or "Delete" or "Patch" or "Head" or "Options";

  /// <summary>
  /// Maps a resolved verb name to the FastEndpoints Configure method name. Returns null when
  /// the name is outside the allow-list (fail-closed).
  /// </summary>
  public static string? ConvertHttpVerbToMethodName(string httpVerb)
    => IsAllowedHttpVerbName(httpVerb) ? httpVerb : null;

  /// <summary>
  /// This compilation plus referenced *contracts* assemblies that share the server's first name
  /// segment (web-server ↔ web-contracts). A server referencing another family's contracts as a
  /// client must not vouch for them.
  /// </summary>
  public static IEnumerable<IAssemblySymbol> GetPairedContractAssemblies(Compilation compilation)
  {
    string sourcePrefix = FirstSegment(compilation.Assembly.Name);
    return compilation.SourceModule.ReferencedAssemblySymbols
      .Where(assembly => assembly.Name.Contains("contracts", StringComparison.OrdinalIgnoreCase)
        && string.Equals(FirstSegment(assembly.Name), sourcePrefix, StringComparison.OrdinalIgnoreCase))
      .Concat([(IAssemblySymbol)compilation.Assembly]);
  }

  /// <summary>"web-server" → "web"; "Web.Contracts" → "Web".</summary>
  private static string FirstSegment(string assemblyName)
  {
    int cut = assemblyName.IndexOfAny(['-', '.']);
    return cut < 0 ? assemblyName : assemblyName[..cut];
  }

  private static IEnumerable<INamedTypeSymbol> AllNested(INamedTypeSymbol type)
  {
    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
    {
      yield return nested;
      foreach (INamedTypeSymbol deeper in AllNested(nested))
      {
        yield return deeper;
      }
    }
  }
}

/// <summary>Hosted [ApiEndpoint] operation with a routed nested Query/Command.</summary>
internal readonly record struct HostedOperationInfo(
  INamedTypeSymbol OperationType,
  INamedTypeSymbol RequestType,
  string RouteTemplate,
  string? HttpVerbName,
  AttributeData ApiRouteAttribute);

/// <summary>A type carrying [ApiRoute] (coverage discovery).</summary>
internal readonly record struct RoutedRequestInfo(
  INamedTypeSymbol RequestType,
  string RouteTemplate,
  string HttpVerbName,
  AttributeData ApiRouteAttribute);
