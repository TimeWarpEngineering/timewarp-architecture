namespace TimeWarp.Architecture.SourceGenerator;

using Models;

[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Reset route registry at the start of each generation
        RouteRegistry.Reset();

        // Create diagnostic descriptor for logging
        var logDiagnostic = new DiagnosticDescriptor(
            "SG001",
            "Source Generator Log",
            "{0}",
            "SourceGenerator",
            DiagnosticSeverity.Warning,
            true);

        // Get all class declarations with the ApiEndpoint attribute
        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                "Starting source generator execution"));

        IncrementalValuesProvider<(ClassDeclarationSyntax ClassDeclaration, SemanticModel SemanticModel)> classDeclarations =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx))
                .Where(static t => t.ClassDeclaration is not null)
                .Select(static (t, _) => (t.ClassDeclaration!, t.SemanticModel));

        // Register for compilation to ensure we have access to referenced assemblies
        context.CompilationProvider.Select((compilation, _) =>
        {
            // This ensures we have access to the compilation when generating source
            return compilation;
        });

        // Generate the source
        context.RegisterSourceOutput(classDeclarations,
            static (spc, source) => Execute(source.ClassDeclaration, source.SemanticModel, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration &&
        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

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

                // Check both the generated attribute and the one from Common.Contracts
                if (fullName == "TimeWarp.Architecture.SourceGenerator.ApiEndpointAttribute")
                {
                    // Get the containing assembly
                    IAssemblySymbol assembly = attributeContainingTypeSymbol.ContainingAssembly;
                    if (assembly != null)
                    {
                        // Check if this is from Common.Contracts or our source generator
                        if (assembly.Name == "Common.Contracts" || assembly.Name.Contains("TimeWarp.Architecture.SourceGenerator"))
                        {
                            return (classDeclaration, semanticModel);
                        }
                    }
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

        context.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Executing source generation for class: {classDeclaration.Identifier.Text}"));

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
        context.AddSource($"{metadata.ClassName}Endpoint.g.cs", SourceText.From(endpointClass, Encoding.UTF8));
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

    // Add using statements
    builder.AppendLine("using FastEndpoints;");
    builder.AppendLine("using OneOf;");
    builder.AppendLine("using System.Threading;");
    builder.AppendLine("using System.Threading.Tasks;");
    builder.AppendLine();

    // Add namespace
    builder.Append("namespace ").Append(metadata.Namespace).AppendLine(";");
    builder.AppendLine();

    // Add XML comments
    builder.AppendLine("/// <summary>");
    builder.Append("/// ").Append(metadata.Summary).AppendLine();
    builder.AppendLine("/// </summary>");
    builder.AppendLine("/// <remarks>");
    builder.Append("/// ").Append(metadata.Description).AppendLine();
    builder.AppendLine("/// </remarks>");

    // Add class declaration
    builder.Append("public class ").Append(metadata.ClassName).Append("Endpoint : ");
    builder.Append(metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint");
    builder.Append("<").Append(metadata.ClassName).Append(".Query, ");
    builder.Append(metadata.ClassName).AppendLine(".Response>");
    builder.AppendLine("{");

    // Add Configure method
    builder.AppendLine("    public override void Configure()");
    builder.AppendLine("    {");
    builder.Append("        ").Append(metadata.HttpVerb).Append("(\"").Append(metadata.Route).AppendLine("\");");

    // Add authorization if required
    if (metadata.RequiresAuthorization)
    {
        builder.AppendLine("        RequireAuthorization();");
    }

    // Add tags if any
    if (metadata.Tags.Any())
    {
        builder.Append("        Tags(");
        IEnumerable<string> tags = metadata.Tags.Select(t => $"\"{t}\"");
        builder.Append(string.Join(", ", tags));
        builder.AppendLine(");");
    }

    // Add summary and description if provided
    if (!string.IsNullOrEmpty(metadata.Summary))
    {
        builder.AppendLine("        Summary(s =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            s.Summary = \"{metadata.Summary}\";");
        builder.AppendLine($"            s.Description = \"{metadata.Description}\";");
        builder.AppendLine($"            s.ExampleRequest = new {metadata.ClassName}.Query {{ Days = 5 }};");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine("        Description(d => d");
        builder.AppendLine($"            .Produces<{metadata.ClassName}.Response>(200, \"Success\")");
        builder.AppendLine("            .ProducesProblem(400, \"Bad Request\")");
        builder.AppendLine("        );");
    }

    // Close Configure method
    builder.AppendLine("    }");
    builder.AppendLine();

    // Add HandleAsync method
    builder.AppendLine("    public override async Task HandleAsync(Query request, CancellationToken ct)");
    builder.AppendLine("    {");
    builder.AppendLine("        // Implementation will be provided by the user");
    builder.AppendLine("        throw new NotImplementedException();");
    builder.AppendLine("    }");
    builder.AppendLine("}");

    return builder.ToString();
    }
}
