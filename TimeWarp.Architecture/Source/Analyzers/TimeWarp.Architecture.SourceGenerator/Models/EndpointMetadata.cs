namespace TimeWarp.Architecture.SourceGenerator.Models;

internal class EndpointMetadata
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
                // Simple XML parsing - in real code you'd want to use proper XML parsing
                metadata.Summary = ExtractXmlContent(xmlDoc, "summary");
                metadata.Description = ExtractXmlContent(xmlDoc, "remarks");
            }
        }

        // Extract authorization requirements
        metadata.RequiresAuthorization = symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name.Contains("Authorize") == true);

        // Extract custom endpoint type
        AttributeData? apiEndpointAttribute = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "TimeWarp.Architecture.SourceGenerator.ApiEndpointAttribute");

        if (apiEndpointAttribute != null)
        {
            KeyValuePair<string, TypedConstant> endpointTypeArg = apiEndpointAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "EndpointType");

            if (!endpointTypeArg.Equals(default))
            {
                // Note: In a real implementation, you'd need to resolve the type
                // This is simplified for demonstration
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
                if (arg.Values.Any())
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
        // Simple XML parsing - just look for <elementName> content </elementName>
        string startTag = $"<{elementName}>";
        string endTag = $"</{elementName}>";
        int startIndex = xmlDoc.IndexOf(startTag);
        if (startIndex == -1) return string.Empty;

        startIndex += startTag.Length;
        int endIndex = xmlDoc.IndexOf(endTag, startIndex);
        if (endIndex == -1) return string.Empty;

        return xmlDoc.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        SyntaxNode? candidate = classDeclaration.Parent;

        while (candidate is not null and not NamespaceDeclarationSyntax and not FileScopedNamespaceDeclarationSyntax)
        {
            candidate = candidate.Parent;
        }

        if (candidate is BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            return namespaceDeclaration.Name.ToString();
        }

        return string.Empty;
    }

    private static bool IsRouteMixinAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
        {
            return false;
        }

        INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
        string fullName = attributeContainingTypeSymbol.ToDisplayString();

        return fullName == "TimeWarp.Architecture.RouteMixinAttribute";
    }

    private static void ExtractRouteMixinInfo(AttributeSyntax attribute, EndpointMetadata metadata)
    {
        if (attribute.ArgumentList?.Arguments.Count >= 2)
        {
            // First argument is route template
            if (attribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax routeLiteral)
            {
                metadata.Route = routeLiteral.Token.ValueText;
            }

            // Second argument is HTTP verb
            if (attribute.ArgumentList.Arguments[1].Expression is MemberAccessExpressionSyntax verbExpression)
            {
                metadata.HttpVerb = verbExpression.Name.Identifier.Text;
            }
        }
    }

    private static string GetXmlCommentContent(DocumentationCommentTriviaSyntax xmlTrivia, string elementName)
    {
        XmlElementSyntax? element = xmlTrivia.ChildNodes()
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(x => x.StartTag.Name.LocalName.Text == elementName);

        return element?.Content.ToString().Trim() ?? string.Empty;
    }

    private static bool HasAuthorizationAttribute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                {
                    string fullName = attributeSymbol.ContainingType.ToDisplayString();
                    if (fullName.Contains("Authorize"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static Type? GetCustomEndpointType(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == "TimeWarp.Architecture.SourceGenerator.ApiEndpointAttribute")
                {
                    // Check for EndpointType property assignment
                    AttributeArgumentSyntax? endpointTypeArg = attribute.ArgumentList?.Arguments
                        .FirstOrDefault(a => (a.NameEquals?.Name.Identifier.Text ?? "") == "EndpointType");

                    if (endpointTypeArg != null)
                    {
                        // Note: In a real implementation, you'd need to resolve the type from the syntax
                        // This is simplified for demonstration
                        return null;
                    }
                }
            }
        }

        return null;
    }

    private static string ConvertHttpVerbToMethodName(string httpVerb)
    {
        // Convert HttpVerb enum value to FastEndpoints method name
        return httpVerb switch
        {
            "Get" => "Get",
            "Post" => "Post",
            "Put" => "Put",
            "Delete" => "Delete",
            "Patch" => "Patch",
            _ => "Get" // Default to Get if unknown
        };
    }

    private static string[] GetTags(ClassDeclarationSyntax classDeclaration)
    {
        List<string> tags = new();

        // Add tag from folder structure
        string filePath = classDeclaration.SyntaxTree.FilePath;

        if (!string.IsNullOrEmpty(filePath))
        {
            string[] folders = filePath.Split(Path.DirectorySeparatorChar);
            int featuresIndex = Array.IndexOf(folders, "Features");

            if (featuresIndex >= 0 && featuresIndex < folders.Length - 1)
            {
                tags.Add(folders[featuresIndex + 1]);
            }
        }

        // Add tags from OpenApiTags attribute (if present)
        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == "OpenApiTags")
                {
                    foreach (AttributeArgumentSyntax arg in attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
                    {
                        if (arg.Expression is LiteralExpressionSyntax literal)
                        {
                            tags.Add(literal.Token.ValueText);
                        }
                    }
                }
            }
        }

        return tags.Distinct().ToArray();
    }
}
