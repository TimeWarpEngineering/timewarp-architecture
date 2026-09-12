namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// <para>Shared harness for exercising the <see cref="FastEndpointSourceGenerator"/>.</para>
/// <para>
/// The generator scans <c>compilation.SourceModule.ReferencedAssemblySymbols</c> for types carrying
/// <c>[ApiEndpoint]</c> (cross-assembly by design — see task 007: attributes live in contracts, the
/// endpoint is generated in the server). So the contract MUST be compiled into a referenced assembly,
/// not handed to the generator as source. The harness also supplies the stub types the generator now
/// hard-requires in the compilation: <c>FastEndpoints.IEndpoint</c> and
/// <c>TimeWarp.Foundation.Features.BaseFastEndpoint`2</c>, plus FQN-matched
/// <c>TimeWarp.Foundation.Features.ApiRouteAttribute</c> / <c>HttpVerb</c> / <c>OpenApiTags</c>.
/// </para>
/// </summary>
internal static class GeneratorTestHarness
{
  // Stub types compiled INTO the contract assembly. The real ApiEndpointAttribute is referenced from
  // the attributes assembly. Note: the generator matches OpenApiTags by simple name "OpenApiTags",
  // so the attribute type must be named exactly that (no "Attribute" suffix).
  public const string SupportStubs = """
    global using TimeWarp.Foundation.Features;

    namespace TimeWarp.Architecture
    {
        public enum HttpVerb { Get, Post, Put, Delete, Patch, Head, Options, Trace }

        // Simple-name matched by the ingress generator (ClientOnlyContractAttribute) — namespace is
        // irrelevant to that match, so a stub here stands in for the foundation-contracts attribute.
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class ClientOnlyContractAttribute : System.Attribute
        {
            public ClientOnlyContractAttribute(string reason) { }
        }
    }
    namespace TimeWarp.Foundation.Features
    {
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class ApiRouteAttribute : System.Attribute
        {
            public ApiRouteAttribute(string route, TimeWarp.Architecture.HttpVerb httpVerb) { }
        }

    }
    namespace TimeWarp.Architecture.Attributes
    {
        [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
        public sealed class OpenApiTags : System.Attribute
        {
            public OpenApiTags(params string[] tags) { }
        }
    }
    """;

  // FastEndpoints types belong on the HOST compilation only. Putting them in every contract
  // assembly makes GetTypeByMetadataName return null (duplicate metadata names → SG002) as
  // soon as two contracts are referenced.
  public const string HostSupportStubs = """
    namespace TimeWarp.Foundation.Features
    {
        public abstract class BaseFastEndpoint<TRequest, TResponse> : FastEndpoints.IEndpoint { }
    }
    namespace FastEndpoints
    {
        public interface IEndpoint { }
    }
    """;

  private static readonly MetadataReference[] FrameworkReferences =
    ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .Where(path => path.Length > 0)
      .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
      .Append(MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location))
      .ToArray();

  /// <summary>
  /// Compiles the given contract source (plus the shared stubs) into a referenceable assembly,
  /// carrying XML documentation so the generator can read summary/remarks cross-assembly.
  /// Runs <see cref="FastEndpointSourceGenerator"/> so <c>[assembly: ApiEndpointsEmbedded]</c>
  /// is stamped when the source declares <c>[ApiEndpoint]</c>.
  /// </summary>
  public static MetadataReference CompileContractAssembly(string contractSource)
    => CompileContractAssembly(contractSource, "Test.Contracts");

  /// <summary>
  /// Compiles a contract assembly under an explicit name — host generators filter referenced
  /// assemblies by AssemblyName (ApiEndpointContractAssemblies / IngressWebContractAssemblies).
  /// </summary>
  public static MetadataReference CompileContractAssembly(string contractSource, string assemblyName)
    => CompileContractAssembly(contractSource, assemblyName, stampMarker: true);

  /// <summary>
  /// Compiles a contract assembly. Pass <paramref name="stampMarker"/> false to omit
  /// <c>[assembly: ApiEndpointsEmbedded]</c> (unmarked-ref tests).
  /// </summary>
  public static MetadataReference CompileContractAssembly(string contractSource, string assemblyName, bool stampMarker)
  {
    CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);

    CSharpCompilation compilation = CSharpCompilation.Create(
      assemblyName,
      new[]
      {
        CSharpSyntaxTree.ParseText(contractSource, parseOptions),
        CSharpSyntaxTree.ParseText(SupportStubs, parseOptions),
      },
      FrameworkReferences,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    Compilation emitCompilation = compilation;
    if (stampMarker)
    {
      GeneratorDriver markerDriver = CSharpGeneratorDriver.Create(
        ImmutableArray.Create(new FastEndpointSourceGenerator().AsSourceGenerator()));
      markerDriver = markerDriver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updated, out _);
      emitCompilation = updated;
    }

    using MemoryStream peStream = new();
    using MemoryStream xmlStream = new();
    Microsoft.CodeAnalysis.Emit.EmitResult emitResult = emitCompilation.Emit(peStream, xmlDocumentationStream: xmlStream);

    if (!emitResult.Success)
    {
      string errors = string.Join(
        Environment.NewLine,
        emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
      throw new InvalidOperationException($"Contract assembly failed to compile:{Environment.NewLine}{errors}");
    }

    peStream.Position = 0;
    xmlStream.Position = 0;
    return MetadataReference.CreateFromStream(
      peStream,
      documentation: XmlDocumentationProvider.CreateFromBytes(xmlStream.ToArray()));
  }

  /// <summary>
  /// Runs the FastEndpoint generator against a compilation that references the contract assembly.
  /// When enabled, <c>ApiEndpointContractAssemblies</c> defaults to <c>Test.Contracts</c>.
  /// </summary>
  public static GeneratorDriverRunResult Run(MetadataReference contractReference, bool enabled)
    => Run([contractReference], enabled, enabled ? "Test.Contracts" : null);

  /// <summary>
  /// Runs the FastEndpoint generator against a host compilation that references the supplied
  /// contract assemblies. <paramref name="contractAssemblies"/> is the
  /// <c>ApiEndpointContractAssemblies</c> allow-list (null/empty when enabled reports TWE008).
  /// </summary>
  public static GeneratorDriverRunResult Run(
    IEnumerable<MetadataReference> contractReferences,
    bool enabled,
    string? contractAssemblies)
  {
    List<MetadataReference> references =
    [
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location),
    ];
    references.AddRange(contractReferences);

    CSharpCompilation compilation = CSharpCompilation.Create(
      "Test.Server",
      syntaxTrees: [CSharpSyntaxTree.ParseText(HostSupportStubs)],
      references: references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    Dictionary<string, string> options = new();
    if (enabled)
    {
      options["build_property.EnableApiEndpointGeneration"] = "true";
    }

    if (!string.IsNullOrWhiteSpace(contractAssemblies))
    {
      options["build_property.ApiEndpointContractAssemblies"] = contractAssemblies;
    }

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new FastEndpointSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    return driver.RunGenerators(compilation).GetRunResult();
  }

  /// <summary>
  /// Runs the <see cref="IngressRoutePrefixGenerator"/> against a host compilation that references
  /// the supplied contract assemblies, with the given build properties (short keys — the
  /// "build_property." prefix is added here). The generator scans referenced assemblies for hosted
  /// [ApiRoute] templates, so the contracts must be metadata references, not source.
  /// </summary>
  public static GeneratorDriverRunResult RunIngress(
    IEnumerable<MetadataReference> contractReferences,
    IReadOnlyDictionary<string, string> buildProperties)
  {
    var references = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location),
    };
    references.AddRange(contractReferences);

    var compilation = CSharpCompilation.Create(
      "Test.IngressHost",
      syntaxTrees: Array.Empty<SyntaxTree>(),
      references: references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new IngressRoutePrefixGenerator();

    var options = buildProperties.ToDictionary(pair => $"build_property.{pair.Key}", pair => pair.Value);

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(generator.AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    return driver.RunGenerators(compilation).GetRunResult();
  }
}
