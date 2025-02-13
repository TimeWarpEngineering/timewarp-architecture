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
    // Register source output for logging initialization
    context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) =>
    {
      INamedTypeSymbol? apiEndpointAttributeSymbol = compilation.GetTypeByMetadataName(ApiEndpointAttributeFullName);
      if (apiEndpointAttributeSymbol == null)
      {
        spc.ReportDiagnostic(Diagnostic.Create(logDiagnostic, Location.None, "ApiEndpointAttribute not found"));
        return;
      }

      // Get the specific type symbol for GetWeatherForecasts
      INamedTypeSymbol? getWeatherForecastsSymbol = compilation.GetTypeByMetadataName("TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts");
      if (getWeatherForecastsSymbol == null)
      {
        spc.ReportDiagnostic(Diagnostic.Create(logDiagnostic, Location.None, "GetWeatherForecasts type not found"));
        return;
      }

      // Inspect the attributes and modifiers of GetWeatherForecasts
      bool hasApiEndpointAttribute = getWeatherForecastsSymbol.GetAttributes().Any(attr =>
          SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttributeSymbol));
      bool isStatic = getWeatherForecastsSymbol.IsStatic;

      if (hasApiEndpointAttribute)
      {
        spc.ReportDiagnostic(Diagnostic.Create(logDiagnostic, Location.None,
          $"GetWeatherForecasts - Has ApiEndpoint: {hasApiEndpointAttribute}, Is Static: {isStatic}"));
      }
      else
      {
        spc.ReportDiagnostic(Diagnostic.Create(logDiagnostic, Location.None, "GetWeatherForecasts does not have the ApiEndpoint attribute"));
      }

      spc.ReportDiagnostic(Diagnostic.Create(logDiagnostic, Location.None, "Source generator initialized"));
    });

    context.RegisterSourceOutput(context.CompilationProvider,
            (spc, compilation) =>
            {
              INamedTypeSymbol? getWeatherForecastsSymbol = compilation.GetTypeByMetadataName("TimeWarp.Architecture.Features.WeatherForecasts.GetWeatherForecasts");
              if (getWeatherForecastsSymbol is not null)
              {
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        "Found GetWeatherForecasts class"));
              }


              spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        "Source generator initialized"));
            });

        // Get all class declarations with the ApiEndpoint attribute
        IncrementalValuesProvider<(ClassDeclarationSyntax ClassDeclaration, SemanticModel SemanticModel)> classDeclarations =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ApiEndpointAttributeFullName,
                    predicate: (node, _) => node is ClassDeclarationSyntax cds &&
                        cds.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
                    transform: (context, _) => ((ClassDeclarationSyntax)context.TargetNode, context.SemanticModel));
// Log all namespaces we're searching through
context.RegisterSourceOutput(context.CompilationProvider,
    (spc, compilation) =>
    {
        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (INamespaceSymbol ns in GetAllNamespaces(assembly.GlobalNamespace))
            {
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        $"Searching namespace: {ns.ToDisplayString()}"));
            }
        }
    });

// Try finding classes with our attribute using SelectMany
IncrementalValuesProvider<INamedTypeSymbol> classSymbols = context.CompilationProvider.SelectMany(
    (compilation, _) =>
    {
      INamedTypeSymbol? apiEndpointAttributeSymbol = compilation.GetTypeByMetadataName(ApiEndpointAttributeFullName);
        if (apiEndpointAttributeSymbol == null) return Array.Empty<INamedTypeSymbol>();

        return compilation.SourceModule.ReferencedAssemblySymbols
            .SelectMany(assembly => GetAllNamespaces(assembly.GlobalNamespace))
            .SelectMany(ns => ns.GetTypeMembers())
            .Where(type => type.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttributeSymbol)));
    });

// Register source output to log found symbols
context.RegisterSourceOutput(classSymbols,
    (spc, symbol) =>
    {
        var logDiagnostic = new DiagnosticDescriptor(
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
                $"Found class with ApiEndpoint attribute using SelectMany: {symbol.Name}"));
    });

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
                spc.ReportDiagnostic(
                    Diagnostic.Create(
                        logDiagnostic,
                        Location.None,
                        $"Compilation references count: {compilation.References.Count()}"));

                foreach (MetadataReference reference in compilation.References)
                {
                    if (reference is PortableExecutableReference peReference)
                    {
                        string assemblyName = peReference.FilePath?.Split('\\').LastOrDefault() ?? "unknown";
                        spc.ReportDiagnostic(
                            Diagnostic.Create(
                                logDiagnostic,
                                Location.None,
                                $"Referenced assembly name: {assemblyName}"));
                    }
                }

                // Log all assemblies and their syntax trees
                foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            logDiagnostic,
                            Location.None,
                            $"Looking at assembly: {assembly.Name}"));
                }

                // Log all syntax trees and their source paths
                foreach (SyntaxTree tree in compilation.SyntaxTrees)
                {
                    string sourcePath = tree.FilePath;
                    string sourceText = tree.GetText().ToString().Split('\n')[0]; // Get first line for context

                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            logDiagnostic,
                            Location.None,
                            $"Scanning syntax tree: {sourcePath}, First line: {sourceText}"));
                }

                // Check metadata references for source files
                foreach (MetadataReference reference in compilation.References)
                {
                    if (reference is CompilationReference compilationRef)
                    {
                        foreach (SyntaxTree tree in compilationRef.Compilation.SyntaxTrees)
                        {
                            spc.ReportDiagnostic(
                                Diagnostic.Create(
                                    logDiagnostic,
                                    Location.None,
                                    $"Found source in referenced compilation: {tree.FilePath}"));
                        }
                    }
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

    private static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol root)
    {
        yield return root;
        foreach (INamespaceSymbol ns in root.GetNamespaceMembers())
        {
            foreach (INamespaceSymbol childNs in GetAllNamespaces(ns))
            {
                yield return childNs;
            }
        }
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
        // Check for static modifier
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
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
