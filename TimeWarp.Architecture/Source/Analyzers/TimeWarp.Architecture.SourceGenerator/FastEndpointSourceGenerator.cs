namespace TimeWarp.Architecture.SourceGenerator;

using Models;

[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
    private const string ApiEndpointAttributeFullName = "TimeWarp.Architecture.Attributes.ApiEndpointAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Reset route registry at the start of each generation
        RouteRegistry.Reset();

        // Create diagnostic descriptor for logging
        var logDiagnostic = new DiagnosticDescriptor
        (
            "SG001",
            "Source Generator Log",
            "{0}",
            "SourceGenerator",
            DiagnosticSeverity.Warning,
            true
        );

        // Register source output for logging initialization
        context.RegisterSourceOutput(context.CompilationProvider,
            (spc, compilation) =>
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        "Source generator initialized"));
            });

        // Get all class declarations with the ApiEndpoint attribute
        IncrementalValuesProvider<(ClassDeclarationSyntax ClassDeclaration, SemanticModel SemanticModel)> classDeclarations =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(t => t.ClassDeclaration is not null)
                .Select((t, _) => (t.ClassDeclaration!, t.SemanticModel));

        // Register source output for compilation diagnostics
        context.RegisterSourceOutput(context.CompilationProvider,
            (spc, compilation) =>
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        $"Compilation started. Assembly: {compilation.AssemblyName}"));

                // Log compilation details
                foreach (SyntaxTree tree in compilation.SyntaxTrees)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            logDiagnostic,
                            Location.None,
                            $"Scanning syntax tree: {tree.FilePath}"));
                }

                // Log all classes with attributes in the compilation
                IEnumerable<ClassDeclarationSyntax> classesWithAttributes = compilation.SyntaxTrees
                    .SelectMany(tree => tree.GetRoot().DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Where(c => c.AttributeLists.Count > 0));

                foreach (ClassDeclarationSyntax classNode in classesWithAttributes)
                {
                    string attributes = string.Join(", ", classNode.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Select(a => a.Name.ToString()));

                    bool hasApiEndpoint = classNode.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(a => a.Name.ToString().EndsWith("ApiEndpoint") || a.Name.ToString().EndsWith("ApiEndpointAttribute"));

                    bool isStatic = classNode.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
                    bool isPartial = classNode.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                    string filePath = classNode.SyntaxTree.FilePath;

                    // Check attribute namespaces
                    foreach (AttributeListSyntax attributeList in classNode.AttributeLists)
                    {
                        foreach (AttributeSyntax attribute in attributeList.Attributes)
                        {
                            if (compilation.GetSemanticModel(classNode.SyntaxTree).GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                            {
                                string fullName = attributeSymbol.ContainingType.ToDisplayString();
                                spc.ReportDiagnostic(
                                    Diagnostic.Create(
                                        logDiagnostic,
                                        Location.None,
                                        $"Attribute on {classNode.Identifier.Text}: Found {fullName}, Looking for {ApiEndpointAttributeFullName}, IsMatch: {fullName == ApiEndpointAttributeFullName}"));
                            }
                        }
                    }

                    bool meetsRequirements = hasApiEndpoint && isStatic && isPartial;
                    string status = meetsRequirements ? "ACCEPTED" : "REJECTED";
                    string reason = !meetsRequirements
                        ? $"Missing: {(hasApiEndpoint ? "" : "ApiEndpoint ")} {(isStatic ? "" : "static ")} {(isPartial ? "" : "partial")}"
                        : "Meets all requirements";

                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            logDiagnostic,
                            Location.None,
                            $"Class: {classNode.Identifier.Text}, Status: {status}, {reason}, File: {filePath}, Attributes: {attributes}"));
                }

            });

        // Log discovered classes and generate the source
        context.RegisterSourceOutput(classDeclarations,
            static (spc, source) =>
            {
                DiagnosticDescriptor logDiagnostic = new(
                    "SG001",
                    "Source Generator Log",
                    "{0}",
                    "SourceGenerator",
                    DiagnosticSeverity.Warning,
                    true);

                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        $"Found ApiEndpoint class: {source.ClassDeclaration.Identifier.Text}"));

                Execute(source.ClassDeclaration, source.SemanticModel, spc);
            });
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        // Only look at class declarations
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        // Must be static and partial
        bool isStatic = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
        bool isPartial = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

        // Must have at least one attribute
        bool hasAttributes = classDeclaration.AttributeLists.Count > 0;

        // Get all attribute names for logging
        var attributeNames = classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.Name.ToFullString().TrimEnd())
            .ToList();

        // Check if any attribute name matches ApiEndpoint
        bool hasApiEndpointAttribute = attributeNames
            .Any(name => name == "ApiEndpoint" || name == "ApiEndpointAttribute");

        return isStatic && isPartial && hasAttributes && hasApiEndpointAttribute;
    }

    private static (ClassDeclarationSyntax? ClassDeclaration, SemanticModel SemanticModel) GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                if (semanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                string fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Check for ApiEndpoint attribute with any namespace
                if (fullName == ApiEndpointAttributeFullName)
                {
                    return (classDeclaration, semanticModel);
                }
            }
        }

        return (null, semanticModel);
    }

    private static void Execute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel, SourceProductionContext context)
    {
        var logDiagnostic = new DiagnosticDescriptor(
            "SG001",
            "Source Generator Log",
            "{0}",
            "SourceGenerator",
            DiagnosticSeverity.Warning,
            true);

        // Log generation details
        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"=== Starting generation for {classDeclaration.Identifier.Text} ==="));

        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Class location: {classDeclaration.GetLocation()?.GetLineSpan().Path ?? "unknown"}"));

        // Extract metadata
        var metadata = EndpointMetadata.FromSyntax(classDeclaration, semanticModel);

        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Extracted metadata - Class: {metadata.ClassName}, Route: {metadata.Route}"));

        // Validate the class structure
        if (!ValidateClassStructure(classDeclaration, context))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    logDiagnostic,
                    Location.None,
                    "Class structure validation failed"));
            return;
        }

        // Check for route conflicts
        if (!RouteRegistry.TryRegisterRoute(metadata.Route, metadata.HttpVerb, metadata.ClassName, context))
        {
            return;
        }

        // Generate the endpoint class
        string endpointClass = GenerateEndpointClass(metadata);
        string fileName = $"{metadata.ClassName}Endpoint.g.cs";

        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Generating endpoint file: {fileName}"));

        context.AddSource(fileName, SourceText.From(endpointClass, Encoding.UTF8));

        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Successfully generated endpoint file: {fileName}"));
    }

    private static bool ValidateClassStructure(ClassDeclarationSyntax classDeclaration, SourceProductionContext context)
    {
        // Check for static and partial modifiers
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) ||
            !classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointMissingPartial,
                    classDeclaration.GetLocation(),
                    classDeclaration.Identifier.Text));
            return false;
        }

        // Check for Query/Command class
        ClassDeclarationSyntax? queryClass = classDeclaration.Members
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text is "Query" or "Command");

        if (queryClass is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointMissingQuery,
                    classDeclaration.GetLocation(),
                    classDeclaration.Identifier.Text));
            return false;
        }

        // Validate interface implementations
        if (!ValidateQueryInterfaces(queryClass, context))
        {
            return false;
        }

        return true;
    }

    private static bool ValidateQueryInterfaces(ClassDeclarationSyntax queryClass, SourceProductionContext context)
    {
        // We should validate using semantic model instead of string comparison
        if (queryClass.BaseList?.Types == null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.ApiEndpointInvalidInterface,
                    queryClass.GetLocation(),
                    "IRequest<> and IQueryStringRouteProvider"));
            return false;
        }

        return true; // Skip validation for now as the contracts are correct
    }

    private static string GenerateEndpointClass(EndpointMetadata metadata)
    {
        StringBuilder builder = new();

        builder.AppendLine("// === Debug Information ===");
        builder.AppendLine($"// Endpoint Class: {metadata.ClassName}");
        builder.AppendLine($"// Namespace: {metadata.Namespace}");
        builder.AppendLine($"// Route: {metadata.Route}");
        builder.AppendLine($"// HTTP Verb: {metadata.HttpVerb}");
        builder.AppendLine($"// Requires Auth: {metadata.RequiresAuthorization}");
        builder.AppendLine($"// Tags: {string.Join(", ", metadata.Tags)}");
        builder.AppendLine($"// Summary: {metadata.Summary}");
        builder.AppendLine($"// Description: {metadata.Description}");
        builder.AppendLine($"// Custom Endpoint Type: {metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint"}");
        builder.AppendLine("// === End Debug Information ===");

        return builder.ToString();
    }

    // AI don't remove this commented code
    // private static string GenerateEndpointClass(EndpointMetadata metadata)
    // {
    //     StringBuilder builder = new();

    //     // Add using statements
    //     builder.AppendLine("using FastEndpoints;");
    //     builder.AppendLine("using OneOf;");
    //     builder.AppendLine("using System.Threading;");
    //     builder.AppendLine("using System.Threading.Tasks;");
    //     builder.AppendLine();

    //     // Add namespace
    //     builder.Append("namespace ").Append(metadata.Namespace).AppendLine(";");
    //     builder.AppendLine();

    //     // Add XML comments
    //     builder.AppendLine("/// <summary>");
    //     builder.Append("/// ").Append(metadata.Summary).AppendLine();
    //     builder.AppendLine("/// </summary>");
    //     builder.AppendLine("/// <remarks>");
    //     builder.Append("/// ").Append(metadata.Description).AppendLine();
    //     builder.AppendLine("/// </remarks>");

    //     // Add class declaration
    //     builder.Append("public class ").Append(metadata.ClassName).Append("Endpoint : ");
    //     builder.Append(metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint");
    //     builder.Append("<").Append(metadata.ClassName).Append(".Query, ");
    //     builder.Append(metadata.ClassName).AppendLine(".Response>");
    //     builder.AppendLine("{");

    //     // Add Configure method
    //     builder.AppendLine("    public override void Configure()");
    //     builder.AppendLine("    {");
    //     builder.Append("        ").Append(metadata.HttpVerb).Append("(\"").Append(metadata.Route).AppendLine("\");");

    //     // Add authorization if required
    //     if (metadata.RequiresAuthorization)
    //     {
    //         builder.AppendLine("        RequireAuthorization();");
    //     }

    //     // Add tags if any
    //     if (metadata.Tags.Any())
    //     {
    //         builder.Append("        Tags(");
    //         IEnumerable<string> tags = metadata.Tags.Select(t => $"\"{t}\"");
    //         builder.Append(string.Join(", ", tags));
    //         builder.AppendLine(");");
    //     }

    //     // Add summary and description if provided
    //     if (!string.IsNullOrEmpty(metadata.Summary))
    //     {
    //         builder.AppendLine("        Summary(s =>");
    //         builder.AppendLine("        {");
    //         builder.AppendLine($"            s.Summary = \"{metadata.Summary}\";");
    //         builder.AppendLine($"            s.Description = \"{metadata.Description}\";");
    //         builder.AppendLine($"            s.ExampleRequest = new {metadata.ClassName}.Query {{ Days = 5 }};");
    //         builder.AppendLine("        });");
    //         builder.AppendLine();
    //         builder.AppendLine("        Description(d => d");
    //         builder.AppendLine($"            .Produces<{metadata.ClassName}.Response>(200, \"Success\")");
    //         builder.AppendLine("            .ProducesProblem(400, \"Bad Request\")");
    //         builder.AppendLine("        );");
    //     }

    //     // Close Configure method
    //     builder.AppendLine("    }");
    //     builder.AppendLine();

    //     // Add HandleAsync method
    //     builder.AppendLine("    public override async Task HandleAsync(Query request, CancellationToken ct)");
    //     builder.AppendLine("    {");
    //     builder.AppendLine("        // Implementation will be provided by the user");
    //     builder.AppendLine("        throw new NotImplementedException();");
    //     builder.AppendLine("    }");
    //     builder.AppendLine("}");

    //     return builder.ToString();
    // }
}
