namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Shared harness for exercising the <see cref="FastEndpointSourceGenerator"/>.
///
/// The generator scans <c>compilation.SourceModule.ReferencedAssemblySymbols</c> for types carrying
/// <c>[ApiEndpoint]</c> (cross-assembly by design — see task 007: attributes live in contracts, the
/// endpoint is generated in the server). So the contract MUST be compiled into a referenced assembly,
/// not handed to the generator as source. The harness also supplies the stub types the generator now
/// hard-requires in the compilation: <c>FastEndpoints.IEndpoint</c> and
/// <c>TimeWarp.Architecture.Features.BaseFastEndpoint`2</c>, plus <c>RouteMixinAttribute</c>/
/// <c>HttpVerb</c>/<c>OpenApiTags</c>.
/// </summary>
internal static class GeneratorTestHarness
{
  // Stub types compiled INTO the contract assembly. The real ApiEndpointAttribute is referenced from
  // the attributes assembly. Note: the generator matches OpenApiTags by simple name "OpenApiTags",
  // so the attribute type must be named exactly that (no "Attribute" suffix).
  public const string SupportStubs = """
    namespace TimeWarp.Architecture
    {
        public enum HttpVerb { Get, Post, Put, Delete, Patch }

        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class RouteMixinAttribute : System.Attribute
        {
            public RouteMixinAttribute(string route, HttpVerb httpVerb) { }
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
    namespace FastEndpoints
    {
        public interface IEndpoint { }
    }
    namespace TimeWarp.Architecture.Features
    {
        public abstract class BaseFastEndpoint<TRequest, TResponse> : FastEndpoints.IEndpoint { }
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
  /// </summary>
  public static MetadataReference CompileContractAssembly(string contractSource)
  {
    CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);

    CSharpCompilation compilation = CSharpCompilation.Create(
      "Test.Contracts",
      new[]
      {
        CSharpSyntaxTree.ParseText(contractSource, parseOptions),
        CSharpSyntaxTree.ParseText(SupportStubs, parseOptions),
      },
      FrameworkReferences,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    using var peStream = new MemoryStream();
    using var xmlStream = new MemoryStream();
    Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(peStream, xmlDocumentationStream: xmlStream);

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
  /// </summary>
  public static GeneratorDriverRunResult Run(MetadataReference contractReference, bool enabled)
  {
    var compilation = CSharpCompilation.Create(
      "Test.Server",
      syntaxTrees: Array.Empty<SyntaxTree>(),
      references: new[]
      {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location),
        contractReference,
      },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new FastEndpointSourceGenerator();

    var options = new Dictionary<string, string>();
    if (enabled)
    {
      options["build_property.EnableApiEndpointGeneration"] = "true";
    }

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(generator.AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    return driver.RunGenerators(compilation).GetRunResult();
  }
}
