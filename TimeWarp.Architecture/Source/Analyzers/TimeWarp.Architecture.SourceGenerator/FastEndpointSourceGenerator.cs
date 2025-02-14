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

          spc.AddSource(fileName, SourceText.From(endpointClass, Encoding.UTF8));
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

            Description(d => d.Produces<{{metadata.ClassName}}.Response>(200, "Success").ProducesProblem(400, "Bad Request")
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
             }
             """;
  }
}
