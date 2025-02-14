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

// Combine symbols with compilation to get semantic model
IncrementalValuesProvider<(INamedTypeSymbol Symbol, Compilation Compilation)> symbolsWithCompilation =
    classSymbols.Combine(context.CompilationProvider);

// Register source output to generate endpoints from found symbols
context.RegisterSourceOutput(symbolsWithCompilation,
    (spc, data) =>
    {
        (INamedTypeSymbol symbol, Compilation compilation) = data;
        spc.ReportDiagnostic(
            Diagnostic.Create(
                logDiagnostic,
                Location.None,
                $"Found class with ApiEndpoint attribute using SelectMany: {symbol.Name}"));

        try
        {
            // Extract metadata directly from symbol
            var metadata = EndpointMetadata.FromSymbol(symbol);

            // Check for route conflicts
            if (!RouteRegistry.TryRegisterRoute(metadata.Route, metadata.HttpVerb, metadata.ClassName, spc))
            {
                return;
            }

            // Generate the endpoint class
            string endpointClass = GenerateEndpointClass(metadata);
            string fileName = $"{metadata.ClassName}Endpoint.g.cs";

            spc.ReportDiagnostic(
                Diagnostic.Create(
                    logDiagnostic,
                    Location.None,
                    $"Generating endpoint file: {fileName}"));

            spc.AddSource(fileName, SourceText.From(endpointClass, Encoding.UTF8));

            spc.ReportDiagnostic(
                Diagnostic.Create(
                    logDiagnostic,
                    Location.None,
                    $"Successfully generated endpoint file: {fileName}"));
        }
        catch (Exception ex)
        {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    logDiagnostic,
                    Location.None,
                    $"Error generating endpoint: {ex.Message}"));
        }
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

    private static string GenerateEndpointClass(EndpointMetadata metadata)
    {
        string tags = metadata.Tags.Any()
            ? $"""
                Tags({string.Join(", ", metadata.Tags.Select(t => $"\"{t}\""))});
              """
            : "";

        string auth = metadata.RequiresAuthorization
            ? """
                RequireAuthorization();
              """
            : "";

        string summary = !string.IsNullOrEmpty(metadata.Summary)
            ? $$"""
                Summary(s =>
                {
                    s.Summary = "{{metadata.Summary}}";
                    s.Description = "{{metadata.Description}}";
                    s.ExampleRequest = new {{metadata.ClassName}}.Query { Days = 5 };
                });

                Description(d => d
                    .Produces<{{metadata.ClassName}}.Response>(200, "Success")
                    .ProducesProblem(400, "Bad Request")
                );
              """
            : "";

        return $$"""
            using FastEndpoints;
            using OneOf;
            using System.Threading;
            using System.Threading.Tasks;

            namespace {{metadata.Namespace}};

            /// <summary>
            /// {{metadata.Summary}}
            /// </summary>
            /// <remarks>
            /// {{metadata.Description}}
            /// </remarks>
            public class {{metadata.ClassName}}Endpoint : {{metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint"}}<{{metadata.ClassName}}.Query, {{metadata.ClassName}}.Response>
            {
                public override void Configure()
                {
                    {{metadata.HttpVerb}}("{{metadata.Route}}");
                    {{auth}}
                    {{tags}}
                    {{summary}}
                }

                public override async Task HandleAsync({{metadata.ClassName}}.Query request, CancellationToken ct)
                {
                    // Implementation will be provided by the user
                    throw new NotImplementedException();
                }
            }
            """;
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
