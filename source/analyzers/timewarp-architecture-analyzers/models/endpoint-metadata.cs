#region Purpose
// Equatable emit model for the FastEndpoint generator: everything needed to emit (or diagnose)
// an [ApiEndpoint] contract without retaining Roslyn symbols across Collect.
#endregion

#region Design
// One flat DTO decouples template emission from Roslyn symbol traversal, so the generated-code
// template can change without touching symbol-walking logic.
// OpenAPI summary/description come from the Query/Command XML docs — the contract is the single
// authoring point for API documentation.
// Tags default to the leaf namespace under Features (…Features.Admin.Roles → "Roles"), keeping
// OpenAPI grouping aligned with the vertical-slice Id without per-endpoint annotation.
// [OpenApiTags] is additive; Distinct() de-dupes.
// HttpVerb is resolved via enum member name (metadata stores the underlying int — Value.ToString()
// would emit "1" for Post). Fail-closed (F-008): unknown/unresolvable verbs leave VerbUnresolved
// true and never default to Get — the generator reports TWE007 and skips emission.
// RequestTypeName is the nested "Query" or "Command" so BaseFastEndpoint<TRequest,TResponse>
// binds the correct request type.
// Authorization (task 110 — fail-closed default): [EndpointAuthorize] drives Policies/Roles/
// AuthSchemes. [EndpointAllowAnonymous] (and ONLY that attribute) sets AllowAnonymous=true.
// Absence of BOTH markers leaves AllowAnonymous=false with no Policy/Roles/Schemes, so
// BuildAuthConfiguration emits nothing — FastEndpoints then requires authentication by default.
// When both markers are present, [EndpointAuthorize] wins (TWA0014 separately flags that as a
// contract-author error — the generator picks a deterministic winner).
// F-005: no CustomEndpointType — emission always uses BaseFastEndpoint.
// Record shape supports Collect() + in-batch route-conflict detection without static
// cross-compilation state (F-003). Honest caveat: ImmutableArray<T>'s IEquatable is reference
// equality of the backing array (not element-wise), so two models with identical Tags content
// but distinct arrays compare unequal — acceptable here because conflict detection keys on
// Route/HttpVerb and does not require Tags content-equality across independently built arrays.
#endregion

namespace TimeWarp.Architecture.Analyzers.Models;

using System.Collections.Generic;
using TimeWarp.Architecture.Analyzers;

internal sealed record EndpointEmitModel(
  string Namespace,
  string ClassName,
  string Route,
  string HttpVerb,
  string RequestTypeName,
  string Summary,
  string Description,
  ImmutableArray<string> Tags,
  string? AuthorizationPolicy,
  string? AuthenticationSchemes,
  string? Roles,
  bool AllowAnonymous,
  bool IsEmptyRequest,
  bool MissingQueryOrCommand,
  bool VerbUnresolved,
  string UnresolvedVerbDisplay)
{
  /// <summary>
  /// Builds an emit model from an [ApiEndpoint] outer type. ClientOnly contracts should be
  /// filtered by the caller via <see cref="HostedRouteDiscovery"/> before calling this.
  /// </summary>
  public static EndpointEmitModel FromSymbol(INamedTypeSymbol symbol)
  {
    string className = symbol.Name;
    string ns = symbol.ContainingNamespace.ToDisplayString();

    INamedTypeSymbol? requestClass = symbol.GetTypeMembers()
      .FirstOrDefault(static m => m.Name is "Query" or "Command");

    if (requestClass is null)
    {
      return new EndpointEmitModel(
        Namespace: ns,
        ClassName: className,
        Route: string.Empty,
        HttpVerb: string.Empty,
        RequestTypeName: string.Empty,
        Summary: string.Empty,
        Description: string.Empty,
        Tags: ImmutableArray<string>.Empty,
        AuthorizationPolicy: null,
        AuthenticationSchemes: null,
        Roles: null,
        AllowAnonymous: false,
        IsEmptyRequest: false,
        MissingQueryOrCommand: true,
        VerbUnresolved: false,
        UnresolvedVerbDisplay: string.Empty);
    }

    string requestTypeName = requestClass.Name;
    string route = string.Empty;
    string httpVerb = string.Empty;
    bool verbUnresolved = false;
    string unresolvedVerbDisplay = string.Empty;

    AttributeData? apiRouteAttribute = requestClass.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == HostedRouteDiscovery.ApiRouteAttributeSimpleName);

    if (apiRouteAttribute?.ConstructorArguments.Length >= 2)
    {
      route = apiRouteAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
      if (string.IsNullOrWhiteSpace(route))
      {
        // Align with HostedRouteDiscovery (whitespace route is not hosted) — report TWE007, never silent skip.
        verbUnresolved = true;
        unresolvedVerbDisplay = "<empty route>";
        route = string.Empty;
      }
      else
      {
        string? resolvedName = HostedRouteDiscovery.ResolveHttpVerbName(apiRouteAttribute.ConstructorArguments[1]);
        string? methodName = resolvedName is null
          ? null
          : HostedRouteDiscovery.ConvertHttpVerbToMethodName(resolvedName);

        if (methodName is null)
        {
          verbUnresolved = true;
          unresolvedVerbDisplay = resolvedName
            ?? apiRouteAttribute.ConstructorArguments[1].Value?.ToString()
            ?? "<missing>";
        }
        else
        {
          httpVerb = methodName;
        }
      }
    }
    else
    {
      // No [ApiRoute] or incomplete ctor args — TWE007 (never emit Get, never silent skip).
      verbUnresolved = true;
      unresolvedVerbDisplay = "<missing ApiRoute>";
    }

    bool isEmptyRequest = !requestClass
      .GetMembers()
      .OfType<IPropertySymbol>()
      .Any(static property =>
        property.DeclaredAccessibility == Accessibility.Public &&
        !property.IsStatic);

    string summary = string.Empty;
    string description = string.Empty;
    string? xmlDoc = requestClass.GetDocumentationCommentXml();
    if (xmlDoc is not null)
    {
      summary = ExtractXmlContent(xmlDoc, "summary");
      description = ExtractXmlContent(xmlDoc, "remarks");
    }

    string? authorizationPolicy = null;
    string? authenticationSchemes = null;
    string? roles = null;
    bool allowAnonymous = false;

    AttributeData? endpointAuthorize = symbol.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == "EndpointAuthorizeAttribute");
    AttributeData? endpointAllowAnonymous = symbol.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == "EndpointAllowAnonymousAttribute");

    if (endpointAuthorize is not null)
    {
      allowAnonymous = false;
      authorizationPolicy = GetNamedStringArgument(endpointAuthorize, "Policy");
      authenticationSchemes = GetNamedStringArgument(endpointAuthorize, "AuthenticationSchemes");
      roles = GetNamedStringArgument(endpointAuthorize, "Roles");
    }
    else if (endpointAllowAnonymous is not null)
    {
      allowAnonymous = true;
    }

    ImmutableArray<string> tags = CollectTags(symbol);

    return new EndpointEmitModel(
      Namespace: ns,
      ClassName: className,
      Route: route,
      HttpVerb: httpVerb,
      RequestTypeName: requestTypeName,
      Summary: summary,
      Description: description,
      Tags: tags,
      AuthorizationPolicy: authorizationPolicy,
      AuthenticationSchemes: authenticationSchemes,
      Roles: roles,
      AllowAnonymous: allowAnonymous,
      IsEmptyRequest: isEmptyRequest,
      MissingQueryOrCommand: false,
      VerbUnresolved: verbUnresolved,
      UnresolvedVerbDisplay: unresolvedVerbDisplay);
  }

  private static ImmutableArray<string> CollectTags(INamedTypeSymbol symbol)
  {
    var tags = new List<string>();

    // Default OpenAPI tag = leaf namespace when Features is an ancestor
    // (…Features.WeatherForecast → WeatherForecast; …Features.Admin.Roles → Roles).
    INamespaceSymbol? walk = symbol.ContainingNamespace;
    bool underFeatures = false;
    while (walk is not null)
    {
      if (walk.Name == "Features")
      {
        underFeatures = true;
        break;
      }

      walk = walk.ContainingNamespace;
    }

    if (underFeatures
        && symbol.ContainingNamespace is { IsGlobalNamespace: false } leaf
        && !string.IsNullOrEmpty(leaf.Name))
    {
      tags.Add(leaf.Name);
    }

    AttributeData? openApiTagsAttribute = symbol.GetAttributes()
      .FirstOrDefault(static attr => attr.AttributeClass?.Name == "OpenApiTags");

    if (openApiTagsAttribute is not null)
    {
      foreach (TypedConstant arg in openApiTagsAttribute.ConstructorArguments)
      {
        if (arg.Values.Length > 0)
        {
          tags.AddRange(arg.Values.Select(static v => v.Value?.ToString() ?? string.Empty));
        }
      }
    }

    return tags.Where(static t => !string.IsNullOrEmpty(t)).Distinct().ToImmutableArray();
  }

  private static string? GetNamedStringArgument(AttributeData attribute, string name)
  {
    foreach (KeyValuePair<string, TypedConstant> arg in attribute.NamedArguments)
    {
      if (arg.Key == name)
      {
        string? value = arg.Value.Value?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
      }
    }

    return null;
  }

  private static string ExtractXmlContent(string xmlDoc, string elementName)
  {
    string startTag = $"<{elementName}>";
    string endTag = $"</{elementName}>";
    int startIndex = xmlDoc.IndexOf(startTag, StringComparison.Ordinal);
    if (startIndex == -1) return string.Empty;

    startIndex += startTag.Length;
    int endIndex = xmlDoc.IndexOf(endTag, startIndex, StringComparison.Ordinal);
    if (endIndex == -1) return string.Empty;

    return xmlDoc.Substring(startIndex, endIndex - startIndex).Trim();
  }
}
