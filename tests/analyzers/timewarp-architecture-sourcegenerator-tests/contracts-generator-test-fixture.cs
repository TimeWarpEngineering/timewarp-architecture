#region Purpose
// Shared arrange helpers for the ContractsGenerator test classes: contract source snippets, the
// compilation and driver builders, and the generated-output readers (hint names, step reasons).
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using TimeWarp.Foundation.Contracts.Generators;

internal static class ContractsGeneratorTestFixture
{
  internal const string HttpVerbStub = """
    namespace TimeWarp.Foundation.Features
    {
        public enum HttpVerb { Get, Post, Put, Delete, Patch, Head, Options }
    }
    """;

  internal const string Source = """
    using TimeWarp.Foundation.Features;

    namespace Test.Features.Admin.Roles;

    public static partial class GetRole
    {
        [ApiRoute("api/Roles/{RoleId:min(1)}", HttpVerb.Get)]
        public sealed partial class Query { }
    }

    public static partial class GetRoles
    {
        [OpenDataQueryParameters]
        [AuthApiRequest]
        public sealed partial class Query { }
    }
    """;

  internal static MetadataReference[] RealOperationReferences()
  {
    string runtime = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .First(static path => Path.GetFileName(path).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase));
    return
    [
      MetadataReference.CreateFromFile(runtime),
      MetadataReference.CreateFromFile(typeof(TimeWarp.Contracts.OperationAttribute).Assembly.Location)
    ];
  }

  internal static bool IsRealOperation(AttributeData attribute) =>
    attribute.AttributeClass is { TypeKind: not TypeKind.Error } symbol
    && symbol.ToDisplayString() == "TimeWarp.Contracts.OperationAttribute";

  internal static CSharpCompilation CreateCompilation(string source, MetadataReference[]? extraReferences = null)
  {
    List<MetadataReference> references = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
    if (extraReferences is not null)
    {
      references.AddRange(extraReferences);
    }

    return CSharpCompilation.Create(
      "Test.Contracts",
      [
        CSharpSyntaxTree.ParseText(HttpVerbStub),
        CSharpSyntaxTree.ParseText(source)
      ],
      references,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  }

  internal static GeneratorDriver CreateDriver(string rootNamespace = "TimeWarp.Architecture", bool trackSteps = false)
  {
    Dictionary<string, string> options = new() { ["build_property.RootNamespace"] = rootNamespace };
    return CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new ContractsGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options),
      driverOptions: trackSteps
        ? new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        : default);
  }

  internal static GeneratorDriverRunResult RunResult
  (
    string source,
    string rootNamespace = "TimeWarp.Architecture",
    MetadataReference[]? extraReferences = null
  )
  {
    GeneratorDriver driver = CreateDriver(rootNamespace);
    return driver.RunGenerators(CreateCompilation(source, extraReferences)).GetRunResult();
  }

  internal static string Run
  (
    string source,
    string rootNamespace = "TimeWarp.Architecture",
    MetadataReference[]? extraReferences = null
  )
  {
    GeneratorDriverRunResult result = RunResult(source, rootNamespace, extraReferences);
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));
  }

  internal static ImmutableArray<string> HintNames(GeneratorDriverRunResult result) =>
  [
    .. result.Results
      .SelectMany(static r => r.GeneratedSources)
      .Select(static s => s.HintName)
      .Where(static name => name != "ContractsGeneratorAttributes.g.cs")
  ];

  internal static ImmutableArray<IncrementalStepRunReason> OutputReasons(GeneratorDriverRunResult result) =>
  [
    .. result.Results
      .SelectMany(static r => r.TrackedOutputSteps)
      .SelectMany(static pair => pair.Value)
      .SelectMany(static step => step.Outputs)
      .Select(static output => output.Reason)
  ];

  internal static void AssertGeneratedCompilesWithoutErrors(string source)
  {
    CSharpCompilation compilation = CreateCompilation(source);
    GeneratorDriver driver = CreateDriver();
    _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updated, out _);
    ImmutableArray<Diagnostic> errors =
    [
      .. updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)
    ];
    errors.ShouldBeEmpty(
      string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
  }

  internal static string RunAndConcat(string rootNamespace) => Run(Source, rootNamespace);

  internal static string RouteContract(string template) => $$"""
    using TimeWarp.Foundation.Features;

    namespace Test.Features.Ccc;

    public static partial class ValidateExport
    {
        [ApiRoute("{{template}}", HttpVerb.Post)]
        public sealed partial class Command { }
    }
    """;
}
