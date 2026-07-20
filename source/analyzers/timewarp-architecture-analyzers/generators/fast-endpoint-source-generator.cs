#region Purpose
// Generates FastEndpoint classes from [ApiEndpoint] contract types in referenced assemblies.
#endregion

#region Design
// Opt-in via the EnableApiEndpointGeneration MSBuild property (default false) so any project can
// reference the analyzers package without silently emitting endpoints.
// Scans referenced-assembly symbols rather than the current compilation's syntax: contracts live
// in a separate project from the server that hosts the generated endpoints.
// Optional ApiEndpointContractAssemblies (semicolon-separated assembly names) restricts which
// referenced assemblies are scanned — needed when the host transitively references other contract
// assemblies (e.g. web-server → web-spa → api-contracts) and must not emit foreign endpoints.
// Empty/unset = scan all referenced assemblies (api-server default).
// Reports SG002 instead of failing when FastEndpoints/BaseFastEndpoint are absent — feature flags
// can strip those references while the generator package remains attached.
// Catches all exceptions (CA1031): a throwing generator would break the entire compilation.
// Request type is Query or Command per metadata.RequestTypeName; HTTP verb comes from resolved
// enum member name (not the underlying int). Auth: [EndpointAuthorize] → Policies/Roles/AuthSchemes;
// no attribute → AllowAnonymous. Do not emit RequireAuthorization() (not on EndpointDefinition).
// Empty request DTOs (no public properties) get EmptyRequestBinder — FE's default binder rejects them.
// Summary/Description only — no weather-specific ExampleRequest.
#endregion

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
    var logDiagnostic = new DiagnosticDescriptor
    (
      "SG001",
      "Source Generator Log",
      "{0}",
      "SourceGenerator",
      DiagnosticSeverity.Warning,
      true
    );

    // Diagnostic when generation is requested but required FastEndpoints types are missing
    var missingFastEndpointsDescriptor = new DiagnosticDescriptor
    (
      "SG002",
      "Missing FastEndpoints dependencies",
      "EnableApiEndpointGeneration is set to true, but FastEndpoints or BaseFastEndpoint could not be found in the compilation. Ensure the api feature and required packages are referenced.",
      "SourceGenerator",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true
    );
// MSBuild properties controlling generation. Default Enable=false (opt-in). Optional
    // ApiEndpointContractAssemblies restricts which referenced assemblies contribute contracts.
    IncrementalValueProvider<(bool Enabled, HashSet<string>? ContractAssemblies)> generationOptions =
      context.AnalyzerConfigOptionsProvider.Select(
        static (options, _) =>
        {
          bool enabled = false;
          if (options.GlobalOptions.TryGetValue("build_property.EnableApiEndpointGeneration", out string? enableValue) &&
              bool.TryParse(enableValue, out bool parsed))
          {
            enabled = parsed;
          }

          HashSet<string>? contractAssemblies = null;
          if (options.GlobalOptions.TryGetValue("build_property.ApiEndpointContractAssemblies", out string? assembliesValue) &&
              !string.IsNullOrWhiteSpace(assembliesValue))
          {
            contractAssemblies = new HashSet<string>(
              assembliesValue.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
              StringComparer.OrdinalIgnoreCase);
          }

          return (enabled, contractAssemblies);
        });

    // Find [ApiEndpoint] types in referenced assemblies (optionally filtered by assembly name).
    IncrementalValuesProvider<(INamedTypeSymbol Symbol, Compilation Compilation, bool Enabled)> symbolsWithCompilationAndFlag =
      context.CompilationProvider
        .Combine(generationOptions)
        .SelectMany(
          static (tuple, _) =>
          {
            (Compilation compilation, (bool enabled, HashSet<string>? contractAssemblies) genOptions) = tuple;
            if (!genOptions.enabled)
            {
              return Array.Empty<(INamedTypeSymbol, Compilation, bool)>();
            }

            INamedTypeSymbol? apiEndpointAttributeSymbol = compilation.GetTypeByMetadataName(ApiEndpointAttributeFullName);
            if (apiEndpointAttributeSymbol is null)
            {
              return Array.Empty<(INamedTypeSymbol, Compilation, bool)>();
            }

            IEnumerable<IAssemblySymbol> assemblies = compilation.SourceModule.ReferencedAssemblySymbols;
            if (genOptions.contractAssemblies is { Count: > 0 } allowed)
            {
              assemblies = assemblies.Where(assembly => allowed.Contains(assembly.Name));
            }

            return assemblies
              .SelectMany(assembly => GetAllNamespaces(assembly.GlobalNamespace))
              .SelectMany(ns => ns.GetTypeMembers())
              .Where(type => type.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, apiEndpointAttributeSymbol)))
              .Select(type => (type, compilation, true));
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
        INamedTypeSymbol? baseFastEndpointSymbol = compilation.GetTypeByMetadataName("TimeWarp.Foundation.Features.BaseFastEndpoint`2");

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

    string auth = BuildAuthConfiguration(metadata);

    // FE default RequestBinder TypeInit-fails on DTOs with zero public properties.
    string emptyBinder = metadata.IsEmptyRequest
      ? $"RequestBinder(new EmptyRequestBinder<{metadata.ClassName}.{metadata.RequestTypeName}>());"
      : "";

    string summary = !string.IsNullOrEmpty(metadata.Summary)
      ? $$"""
            Summary(s =>
            {
              s.Summary = "{{EscapeForStringLiteral(metadata.Summary)}}";
              s.Description = "{{EscapeForStringLiteral(metadata.Description)}}";
            });

            Description(d => d.Produces<{{metadata.ClassName}}.Response>(200, "Success").ProducesProblem(400, "Bad Request")
            );
          """
      : "";

    string requestType = metadata.RequestTypeName;
    string baseType = metadata.CustomEndpointType?.FullName ?? "BaseFastEndpoint";

    return $$"""
             using FastEndpoints;
             using OneOf;
             using System.Threading;
             using System.Threading.Tasks;
             using TimeWarp.Foundation.Features;

             namespace {{metadata.Namespace}};

             /// <summary>
             /// {{metadata.Summary}}
             /// </summary>
             /// <remarks>
             /// {{metadata.Description}}
             /// </remarks>
             public class {{metadata.ClassName}}Endpoint : {{baseType}}<{{metadata.ClassName}}.{{requestType}}, {{metadata.ClassName}}.Response>
             {
               public override void Configure()
               {
                 {{metadata.HttpVerb}}("{{metadata.Route}}");
                 {{auth}}
                 {{emptyBinder}}
                 {{tags}}
                 {{summary}}
               }
             }
             """;
  }

  /// <summary>
  /// Builds FastEndpoints Configure() auth lines from [EndpointAuthorize] metadata.
  /// No attribute → AllowAnonymous. Policy → Policies(...). Roles → Roles(...).
  /// Schemes → AuthSchemes(...). FE requires auth by default when AllowAnonymous is not called.
  /// </summary>
  private static string BuildAuthConfiguration(EndpointMetadata metadata)
  {
    if (metadata.AllowAnonymous)
    {
      return "AllowAnonymous();";
    }

    var lines = new List<string>();

    if (!string.IsNullOrEmpty(metadata.AuthenticationSchemes))
    {
      lines.Add($"AuthSchemes({FormatCsvStringArgs(metadata.AuthenticationSchemes)});");
    }

    if (!string.IsNullOrEmpty(metadata.AuthorizationPolicy))
    {
      lines.Add($"Policies(\"{EscapeForStringLiteral(metadata.AuthorizationPolicy)}\");");
    }
    else if (!string.IsNullOrEmpty(metadata.Roles))
    {
      lines.Add($"Roles({FormatCsvStringArgs(metadata.Roles)});");
    }

    // [EndpointAuthorize] with no Policy/Roles (and optional AuthSchemes only): FE defaults to
    // requiring authentication — emit no AllowAnonymous and no non-existent RequireAuthorization().
    return lines.Count > 0
      ? string.Join("\n         ", lines)
      : string.Empty;
  }

  private static string FormatCsvStringArgs(string csv)
  {
    IEnumerable<string> parts = csv
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(part => $"\"{EscapeForStringLiteral(part)}\"");
    return string.Join(", ", parts);
  }

  private static string EscapeForStringLiteral(string value)
    => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
