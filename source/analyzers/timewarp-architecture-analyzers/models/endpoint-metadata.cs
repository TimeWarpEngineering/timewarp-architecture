#region Purpose
// Extracts everything the FastEndpoint generator needs from an [ApiEndpoint] contract symbol.
#endregion

#region Design
// One flat DTO decouples template emission from Roslyn symbol traversal, so the generated-code
// template can change without touching symbol-walking logic.
// OpenAPI summary/description come from the Query/Command XML docs — the contract is the single
// authoring point for API documentation.
// Tags default to the feature folder name (namespace segment above "Features"), keeping OpenAPI
// grouping aligned with the vertical-slice layout without per-endpoint annotation.
// HttpVerb is resolved via enum member name (metadata stores the underlying int — Value.ToString()
// would emit "1" for Post and fall through ConvertHttpVerbToMethodName to Get).
// RequestTypeName is the nested "Query" or "Command" so BaseFastEndpoint<TRequest,TResponse>
// binds the correct request type.
// Authorization (task 110 — fail-closed default): [EndpointAuthorize] drives Policies/Roles/
// AuthSchemes. [EndpointAllowAnonymous] (and ONLY that attribute) sets AllowAnonymous=true.
// Absence of BOTH markers leaves AllowAnonymous=false with no Policy/Roles/Schemes, so
// BuildAuthConfiguration emits nothing — FastEndpoints then requires authentication by default.
// This is the inverse of the pre-110 behavior (absence used to mean AllowAnonymous=true). When
// both markers are present, [EndpointAuthorize] wins (TWA0014 separately flags that as a
// contract-author error to resolve — the generator does not error, it picks a deterministic
// winner).
#endregion

namespace TimeWarp.Architecture.Analyzers.Models;

internal sealed class EndpointMetadata
{
  public string Namespace { get; set; } = string.Empty;
  public string ClassName { get; set; } = string.Empty;
  public string Route { get; set; } = string.Empty;
  public string HttpVerb { get; set; } = "Get";
  /// <summary>Nested request type name: "Query" or "Command".</summary>
  public string RequestTypeName { get; set; } = "Query";
  public string Summary { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string[] Tags { get; set; } = Array.Empty<string>();
  public string? AuthorizationPolicy { get; set; }
  public string? AuthenticationSchemes { get; set; }
  public string? Roles { get; set; }
  /// <summary>
  /// True ONLY when <c>[EndpointAllowAnonymous]</c> is present and <c>[EndpointAuthorize]</c> is
  /// not (task 110: fail-closed default — absence of BOTH markers leaves this false, so
  /// BuildAuthConfiguration emits nothing and FastEndpoints requires authentication by default).
  /// </summary>
  public bool AllowAnonymous { get; set; }
  /// <summary>
  /// True when the request DTO has no public instance properties (FE default binder rejects these).
  /// </summary>
  public bool IsEmptyRequest { get; set; }
  public Type? CustomEndpointType { get; set; }

  public static EndpointMetadata FromSymbol(INamedTypeSymbol symbol)
  {
    EndpointMetadata metadata = new()
    {
      ClassName = symbol.Name,
      Namespace = symbol.ContainingNamespace.ToDisplayString()
    };

    // Find Query/Command class
    INamedTypeSymbol? requestClass = symbol.GetTypeMembers()
      .FirstOrDefault(m => m.Name is "Query" or "Command");

    if (requestClass != null)
    {
      metadata.RequestTypeName = requestClass.Name;

      // Extract route and HTTP verb from ApiRoute attribute. Match by simple name: the attribute is
      // emitted into each consumer's RootNamespace by the contracts generator, so the namespace
      // varies per generated app (a full display-string match would pin one root namespace).
      AttributeData? apiRouteAttribute = requestClass.GetAttributes()
        .FirstOrDefault(attr => attr.AttributeClass?.Name == "ApiRouteAttribute");

      if (apiRouteAttribute != null && apiRouteAttribute.ConstructorArguments.Length >= 2)
      {
        metadata.Route = apiRouteAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        metadata.HttpVerb = ConvertHttpVerbToMethodName(ResolveHttpVerbName(apiRouteAttribute.ConstructorArguments[1]));
      }

      // FE RequestBinder refuses DTOs with zero public properties. Count declared + inherited
      // public instance properties (route segments generated onto the partial count too).
      metadata.IsEmptyRequest = !requestClass
        .GetMembers()
        .OfType<IPropertySymbol>()
        .Any(property =>
          property.DeclaredAccessibility == Accessibility.Public &&
          !property.IsStatic);

      // Extract documentation
      string? xmlDoc = requestClass.GetDocumentationCommentXml();
      if (xmlDoc != null)
      {
        metadata.Summary = ExtractXmlContent(xmlDoc, "summary");
        metadata.Description = ExtractXmlContent(xmlDoc, "remarks");
      }
    }

    // Authorization from [EndpointAuthorize]/[EndpointAllowAnonymous] on the contract class
    // (simple name match — matches every other attribute lookup in this file, so the marker works
    // regardless of which consuming assembly's root namespace it was generated/declared in).
    AttributeData? endpointAuthorize = symbol.GetAttributes()
      .FirstOrDefault(attr => attr.AttributeClass?.Name == "EndpointAuthorizeAttribute");
    AttributeData? endpointAllowAnonymous = symbol.GetAttributes()
      .FirstOrDefault(attr => attr.AttributeClass?.Name == "EndpointAllowAnonymousAttribute");

    if (endpointAuthorize is not null)
    {
      // [EndpointAuthorize] wins when both markers are present — see this file's Design region.
      metadata.AllowAnonymous = false;
      metadata.AuthorizationPolicy = GetNamedStringArgument(endpointAuthorize, "Policy");
      metadata.AuthenticationSchemes = GetNamedStringArgument(endpointAuthorize, "AuthenticationSchemes");
      metadata.Roles = GetNamedStringArgument(endpointAuthorize, "Roles");
    }
    else if (endpointAllowAnonymous is not null)
    {
      metadata.AllowAnonymous = true;
    }
    // else: fail-closed default already set by the field initializer (AllowAnonymous = false) —
    // no explicit marker means BuildAuthConfiguration emits nothing and FastEndpoints requires
    // authentication by default. Unreachable in a clean build once TWA0013 is wired (every
    // [ApiEndpoint] contract must carry one marker), but stays fail-closed even under a suppressed
    // analyzer.

    // Extract custom endpoint type
    AttributeData? apiEndpointAttribute = symbol.GetAttributes()
      .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "TimeWarp.Architecture.Attributes.ApiEndpointAttribute");

    if (apiEndpointAttribute != null)
    {
      KeyValuePair<string, TypedConstant> endpointTypeArg = apiEndpointAttribute.NamedArguments
        .FirstOrDefault(arg => arg.Key == "EndpointType");

      if (!endpointTypeArg.Equals(default))
      {
        metadata.CustomEndpointType = null;
      }
    }

    // Generate tags from namespace structure
    List<string> tags = new();
    INamespaceSymbol? containingNamespace = symbol.ContainingNamespace;

    while (containingNamespace != null)
    {
      if (containingNamespace.Name == "Features" && containingNamespace.ContainingNamespace != null)
      {
        tags.Add(containingNamespace.ContainingNamespace.Name);
        break;
      }

      containingNamespace = containingNamespace.ContainingNamespace;
    }

    // Add tags from OpenApiTags attribute
    AttributeData? openApiTagsAttribute = symbol.GetAttributes()
      .FirstOrDefault(attr => attr.AttributeClass?.Name == "OpenApiTags");

    if (openApiTagsAttribute != null)
    {
      foreach (TypedConstant arg in openApiTagsAttribute.ConstructorArguments)
      {
        if (arg.Values.Length > 0)
        {
          tags.AddRange(arg.Values.Select(v => v.Value?.ToString() ?? string.Empty));
        }
      }
    }

    metadata.Tags = tags.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToArray();

    return metadata;
  }

  /// <summary>
  /// Resolves an HttpVerb TypedConstant to its enum member name.
  /// Metadata stores the underlying int, so <c>Value.ToString()</c> yields "1" for Post — wrong.
  /// </summary>
  private static string ResolveHttpVerbName(TypedConstant verbArgument)
  {
    if (verbArgument.Type is INamedTypeSymbol enumType && enumType.TypeKind == TypeKind.Enum)
    {
      IFieldSymbol? field = enumType.GetMembers().OfType<IFieldSymbol>()
        .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, verbArgument.Value));
      if (field is not null) return field.Name;
    }

    return verbArgument.Value?.ToString() ?? "Get";
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

  private static string ConvertHttpVerbToMethodName(string httpVerb)
  {
    return httpVerb switch
    {
      "Get" => "Get",
      "Post" => "Post",
      "Put" => "Put",
      "Delete" => "Delete",
      "Patch" => "Patch",
      _ => "Get"
    };
  }
}
