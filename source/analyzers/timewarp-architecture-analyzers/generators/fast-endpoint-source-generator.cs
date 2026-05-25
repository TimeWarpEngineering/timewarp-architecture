namespace TimeWarp.Architecture.Analyzers;

using TimeWarp.Architecture.Analyzers.Models;

[Generator]
public class FastEndpointSourceGenerator : IIncrementalGenerator
{
    // Generator is disabled by default.
    // Consumers must explicitly set <EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>
    // (or via their feature flag system) in their .csproj / Directory.Build.props.

  private const string ApiEndpointAttributeFullName = "TimeWarp.Architecture.Attributes.ApiEndpointAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Reset route registry at the start of each generation
    RouteRegistry.Reset();

    // Create diagnostic descriptor for logging
    DiagnosticDescriptor logDiagnostic = new DiagnosticDescriptor
    (
      "SG001",
      "Source Generator Log",
      "{0}",
      "SourceGenerator",
      DiagnosticSeverity.Warning,
      true
    );

    // Diagnostic when generation is requested but required FastEndpoints types are missing
    DiagnosticDescriptor missingFastEndpointsDescriptor = new DiagnosticDescriptor
    (
      "SG002",
      "Missing FastEndpoints dependencies",
      "EnableApiEndpointGeneration is set to true, but FastEndpoints or BaseFastEndpoint could not be found in the compilation. Ensure the api feature and required packages are referenced.",
      "SourceGenerator",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true
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

// Read MSBuild property to control whether this generator should run.
// Default is false (opt-in), as requested.
    IncrementalValueProvider<bool> enableApiEndpointGeneration = context.AnalyzerConfigOptionsProvider.Select(
        static (options, _) =>
        {
            if (options.GlobalOptions.TryGetValue("build_property.EnableApiEndpointGeneration", out var value) &&
                bool.TryParse(value, out var enabled))
            {
                return enabled;
            }

            return false; // default to false
        });

// Combine symbols with compilation and the enable flag so we can check for required types
    IncrementalValuesProvider<(INamedTypeSymbol Symbol, Compilation Compilation, bool Enabled)> symbolsWithCompilationAndFlag =
      classSymbols
        .Combine(context.CompilationProvider)
        .Combine(enableApiEndpointGeneration)
        .Select(static (tuple, _) =>
        {
            var ((symbol, compilation), enabled) = tuple;
            return (symbol, compilation, enabled);
        });

// Register source output to generate endpoints from found symbols
    context.RegisterSourceOutput(symbolsWithCompilationAndFlag,
      (spc, data) =>
      {
        (INamedTypeSymbol symbol, Compilation compilation, bool enabled) = data;

        if (!enabled)
        {
            return; // Generator is disabled for this project (default)
        }

        // Check that required FastEndpoints types are available before generating
        INamedTypeSymbol? fastEndpointsSymbol = compilation.GetTypeByMetadataName("FastEndpoints.IEndpoint");
        INamedTypeSymbol? baseFastEndpointSymbol = compilation.GetTypeByMetadataName("TimeWarp.Architecture.Features.BaseFastEndpoint");

        if (fastEndpointsSymbol == null || baseFastEndpointSymbol == null)
        {
            spc.ReportDiagnostic(
                Diagnostic.Create(
                    missingFastEndpointsDescriptor,
                    Location.None));
            return;
        }

        try
        {
          // Extract metadata directly from symbol
          EndpointMetadata metadata = EndpointMetadata.FromSymbol(symbol);

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
        catch (Exception ex) // CA1031: Source generators must be resilient
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
    string tags = metadata.Tags.Length > 0
      ? $"""
         Tags({string.Join(", ", metadata.Tags.Select(t => $"\"{t}\""))});
         """
      : "";

    string auth = metadata.RequiresAuthorization
      ? """
          RequireAuthorization();
        """
      : "AllowAnonymous();";

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
