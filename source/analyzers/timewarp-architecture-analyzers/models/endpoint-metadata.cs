namespace TimeWarp.Architecture.Analyzers.Models;

internal sealed class EndpointMetadata
{
  public string Namespace { get; set; } = string.Empty;
  public string ClassName { get; set; } = string.Empty;
  public string Route { get; set; } = string.Empty;
  public string HttpVerb { get; set; } = "Get";
  public string Summary { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string[] Tags { get; set; } = Array.Empty<string>();
  public bool RequiresAuthorization { get; set; }
  public Type? CustomEndpointType { get; set; }

  public static EndpointMetadata FromSymbol(INamedTypeSymbol symbol)
  {
    EndpointMetadata metadata = new()
    {
      ClassName = symbol.Name,
      Namespace = symbol.ContainingNamespace.ToDisplayString()
    };

    // Find Query/Command class
    INamedTypeSymbol? queryClass = symbol.GetTypeMembers()
      .FirstOrDefault(m => m.Name is "Query" or "Command");

    if (queryClass != null)
    {
      // Extract route and HTTP verb from RouteMixin attribute
      AttributeData? routeMixinAttribute = queryClass.GetAttributes()
        .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "TimeWarp.Architecture.RouteMixinAttribute");

      if (routeMixinAttribute != null && routeMixinAttribute.ConstructorArguments.Length >= 2)
      {
        metadata.Route = routeMixinAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        string httpVerb = routeMixinAttribute.ConstructorArguments[1].Value?.ToString() ?? "Get";
        metadata.HttpVerb = ConvertHttpVerbToMethodName(httpVerb);
      }

      // Extract documentation
      string? xmlDoc = queryClass.GetDocumentationCommentXml();
      if (xmlDoc != null)
      {
        metadata.Summary = ExtractXmlContent(xmlDoc, "summary");
        metadata.Description = ExtractXmlContent(xmlDoc, "remarks");
      }
    }

    // Extract authorization requirements
    metadata.RequiresAuthorization = symbol.GetAttributes()
      .Any(attr => attr.AttributeClass?.Name.Contains("Authorize", StringComparison.Ordinal) == true);

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
