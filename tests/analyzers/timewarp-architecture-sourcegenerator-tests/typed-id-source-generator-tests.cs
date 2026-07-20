namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

// Verifies TypedIdSourceGenerator: internal [TypedId] attribute injection, BCL surface
// (New/From/Json/Parse), TWE006 on invalid shapes, the [assembly: TypedIdsEmbedded] marker, and EF
// ValueConverter emission — both for same-compilation ids and for ids in referenced assemblies
// (internal attribute applications and record-struct shape must survive the metadata round-trip).
public class TypedIdSourceGenerator_Tests
{
  private const string SampleSource = """
    namespace Sample;

    [TimeWarp.Architecture.TypedId]
    public readonly partial record struct OrderId;
    """;

  private const string EfStubs = """
    namespace Microsoft.EntityFrameworkCore.Storage.ValueConversion
    {
        public abstract class ValueConverter<TModel, TProvider>
        {
            protected ValueConverter(
                System.Linq.Expressions.Expression<System.Func<TModel, TProvider>> convertToProviderExpression,
                System.Linq.Expressions.Expression<System.Func<TProvider, TModel>> convertFromProviderExpression)
            {
            }
        }
    }
    namespace Microsoft.EntityFrameworkCore
    {
        public class ModelConfigurationBuilder
        {
            public PropertiesBuilder<TProperty> Properties<TProperty>() => null!;
        }
        public class PropertiesBuilder<TProperty>
        {
            public PropertiesBuilder<TProperty> HaveConversion<TConverter>() => this;
        }
    }
    """;

  private static readonly MetadataReference[] FrameworkReferences =
    ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .Where(static path => path.Length > 0)
      .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
      .ToArray();

  private static CSharpCompilation CreateCompilation(
    string assemblyName,
    string[] sources,
    MetadataReference[]? extraReferences = null)
  {
    return CSharpCompilation.Create(
      assemblyName,
      sources.Select(static s => CSharpSyntaxTree.ParseText(s)),
      FrameworkReferences.Concat(extraReferences ?? []),
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  }

  private static GeneratorDriverRunResult Run(CSharpCompilation compilation)
  {
    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new TypedIdSourceGenerator().AsSourceGenerator()));
    return driver.RunGenerators(compilation).GetRunResult();
  }

  private static string Concat(GeneratorDriverRunResult result) =>
    string.Join(
      Environment.NewLine,
      result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));

  /// <summary>Runs the generator on the sources and returns the referenceable compiled assembly.</summary>
  private static MetadataReference CompileWithGeneratorToReference(string assemblyName, params string[] sources)
  {
    CSharpCompilation compilation = CreateCompilation(assemblyName, sources);
    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new TypedIdSourceGenerator().AsSourceGenerator()));
    _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updated, out _);

    using var stream = new MemoryStream();
    Microsoft.CodeAnalysis.Emit.EmitResult emit = updated.Emit(stream);
    emit.Success.ShouldBeTrue(
      string.Join(Environment.NewLine, emit.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
    return MetadataReference.CreateFromImage(stream.ToArray());
  }

  public static Task Should_Emit_Internal_TypedIdAttribute()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", [SampleSource])));

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class TypedIdAttribute : System.Attribute");
    generated.ShouldNotContain("public sealed class TypedIdAttribute");
    generated.ShouldContain("AttributeTargets.Struct");
    generated.ShouldContain("internal sealed class TypedIdsEmbeddedAttribute : System.Attribute");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Bcl_Surface_For_Partial_Readonly_Record_Struct()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", [SampleSource])));

    generated.ShouldContain("public readonly partial record struct OrderId");
    generated.ShouldContain("public static OrderId New()");
    generated.ShouldContain("Guid.CreateVersion7()");
    generated.ShouldContain("public static OrderId From(Guid value)");
    generated.ShouldContain("OrderId cannot be empty.");
    generated.ShouldContain("IComparable<OrderId>");
    generated.ShouldContain("IParsable<OrderId>");
    generated.ShouldContain("ISpanParsable<OrderId>");
    generated.ShouldContain("class OrderIdJsonConverter");
    generated.ShouldContain("WriteStringValue(value.Value)");
    generated.ShouldContain("OrderId cannot be empty or invalid.");
    generated.ShouldContain("ReadAsPropertyName");
    generated.ShouldContain("WriteAsPropertyName");
    generated.ShouldContain("class OrderIdTypeConverter");
    generated.ShouldContain("[JsonConverter(typeof(OrderIdJsonConverter))]");
    generated.ShouldContain("[TypeConverter(typeof(OrderIdTypeConverter))]");
    return Task.CompletedTask;
  }

  public static Task Should_Stamp_Assembly_Marker_When_Ids_Exist()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", [SampleSource])));
    generated.ShouldContain("[assembly: TimeWarp.Architecture.TypedIdsEmbedded]");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Stamp_Assembly_Marker_Without_Ids()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", ["namespace Sample; public class Nothing;"])));
    generated.ShouldNotContain("[assembly: TimeWarp.Architecture.TypedIdsEmbedded]");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE006_For_Non_Partial_Record_Struct()
  {
    GeneratorDriverRunResult result = Run(CreateCompilation("Test.TypedIds", ["""
      namespace Sample;

      [TimeWarp.Architecture.TypedId]
      public readonly record struct BadId;
      """]));

    result.Diagnostics.ShouldContain(static d => d.Id == "TWE006" && d.Severity == DiagnosticSeverity.Error);
    Concat(result).ShouldNotContain("public static BadId New()");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE006_For_Plain_Struct()
  {
    GeneratorDriverRunResult result = Run(CreateCompilation("Test.TypedIds", ["""
      namespace Sample;

      [TimeWarp.Architecture.TypedId]
      public readonly partial struct BadId;
      """]));

    result.Diagnostics.ShouldContain(static d => d.Id == "TWE006" && d.Severity == DiagnosticSeverity.Error);
    Concat(result).ShouldNotContain("public static BadId New()");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Emit_Ef_Without_ValueConverter()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", [SampleSource])));

    generated.ShouldNotContain("ValueConverter");
    generated.ShouldNotContain("ConfigureTypedIdConventions");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Ef_Converters_For_Source_Ids_When_ValueConverter_Present()
  {
    string generated = Concat(Run(CreateCompilation("Test.TypedIds", [SampleSource, EfStubs])));

    generated.ShouldContain("internal sealed class OrderIdValueConverter");
    generated.ShouldContain("ValueConverter<global::Sample.OrderId, Guid>");
    generated.ShouldContain("global::Sample.OrderId.From(v)");
    generated.ShouldContain("ConfigureTypedIdConventions");
    generated.ShouldContain("Properties<global::Sample.OrderId>().HaveConversion<OrderIdValueConverter>()");
    generated.ShouldContain("namespace TimeWarp.Architecture.TypedIds.Ef");
    return Task.CompletedTask;
  }

  // The 104-027 review scenarios: an EF host referencing an id assembly must (a) not hit CS0436 from
  // the injected attribute and (b) still discover the referenced ids through metadata (internal
  // attribute applications and record-struct shape both survive compilation to metadata).
  public static Task Should_Emit_Ef_Converters_For_Ids_In_Referenced_Assembly()
  {
    MetadataReference idAssembly = CompileWithGeneratorToReference("Sample.Ids", SampleSource);

    CSharpCompilation host = CreateCompilation(
      "Sample.EfHost",
      ["namespace Host; public class Anchor;", EfStubs],
      [idAssembly]);

    string generated = Concat(Run(host));

    generated.ShouldContain("internal sealed class OrderIdValueConverter");
    generated.ShouldContain("ValueConverter<global::Sample.OrderId, Guid>");
    generated.ShouldContain("Properties<global::Sample.OrderId>().HaveConversion<OrderIdValueConverter>()");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Collide_When_Host_Declares_Own_Ids_And_References_Id_Assembly()
  {
    MetadataReference idAssembly = CompileWithGeneratorToReference("Sample.Ids", SampleSource);

    CSharpCompilation host = CreateCompilation(
      "Sample.Host",
      ["""
      namespace Host;

      [TimeWarp.Architecture.TypedId]
      public readonly partial record struct SessionId;
      """],
      [idAssembly]);

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new TypedIdSourceGenerator().AsSourceGenerator()));
    _ = driver.RunGeneratorsAndUpdateCompilation(host, out Compilation updated, out _);

    // CS0436 (source type conflicts with imported type) must not appear now that the injected
    // attribute is internal and therefore not exported by the referenced id assembly.
    ImmutableArray<Diagnostic> diagnostics = updated.GetDiagnostics();
    diagnostics.ShouldNotContain(static d => d.Id == "CS0436");
    diagnostics.ShouldNotContain(static d => d.Severity == DiagnosticSeverity.Error);
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Once_For_Multiple_Attributed_Partial_Declarations()
  {
    GeneratorDriverRunResult result = Run(CreateCompilation("Test.TypedIds", ["""
      namespace Sample;

      [TimeWarp.Architecture.TypedId]
      public readonly partial record struct OrderId;

      [TimeWarp.Architecture.TypedId]
      public readonly partial record struct OrderId;
      """]));

    result.Results
      .SelectMany(static r => r.GeneratedSources)
      .Count(static s => s.HintName.Contains("OrderId.TypedId", StringComparison.Ordinal))
      .ShouldBe(1);
    return Task.CompletedTask;
  }
}
