using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public static EndpointMetadata FromSyntax(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        var metadata = new EndpointMetadata
        {
            ClassName = classDeclaration.Identifier.Text,
            Namespace = GetNamespace(classDeclaration)
        };

        // Extract route and HTTP verb from RouteMixin attribute
        var queryClass = classDeclaration.Members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text is "Query" or "Command");

        if (queryClass != null)
        {
            foreach (var attributeList in queryClass.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsRouteMixinAttribute(attribute, semanticModel))
                    {
                        ExtractRouteMixinInfo(attribute, metadata);
                    }
                }
            }

            // Extract documentation from XML comments
            var xmlTrivia = queryClass.GetLeadingTrivia()
                .Select(t => t.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (xmlTrivia != null)
            {
                metadata.Summary = GetXmlCommentContent(xmlTrivia, "summary");
                metadata.Description = GetXmlCommentContent(xmlTrivia, "remarks");
            }
        }

        // Extract authorization requirements
        metadata.RequiresAuthorization = HasAuthorizationAttribute(classDeclaration, semanticModel);

        // Extract custom endpoint type if specified
        metadata.CustomEndpointType = GetCustomEndpointType(classDeclaration, semanticModel);

        // Generate tags from folder structure and attributes
        metadata.Tags = GetTags(classDeclaration);

        return metadata;
    }

    private static string GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        var candidate = classDeclaration.Parent;
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

        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
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
        var element = xmlTrivia.ChildNodes()
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(x => x.StartTag.Name.LocalName.Text == elementName);

        return element?.Content.ToString().Trim() ?? string.Empty;
    }

    private static bool HasAuthorizationAttribute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                {
                    var fullName = attributeSymbol.ContainingType.ToDisplayString();
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
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == "TimeWarp.Architecture.SourceGenerator.ApiEndpointAttribute")
                {
                    // Check for EndpointType property assignment
                    var endpointTypeArg = attribute.ArgumentList?.Arguments
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

    private static string[] GetTags(ClassDeclarationSyntax classDeclaration)
    {
        var tags = new List<string>();

        // Add tag from folder structure
        var filePath = classDeclaration.SyntaxTree.FilePath;
        if (!string.IsNullOrEmpty(filePath))
        {
            var folders = filePath.Split(Path.DirectorySeparatorChar);
            var featuresIndex = Array.IndexOf(folders, "Features");
            if (featuresIndex >= 0 && featuresIndex < folders.Length - 1)
            {
                tags.Add(folders[featuresIndex + 1]);
            }
        }

        // Add tags from OpenApiTags attribute (if present)
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.Name.ToString() == "OpenApiTags")
                {
                    foreach (var arg in attribute.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())
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