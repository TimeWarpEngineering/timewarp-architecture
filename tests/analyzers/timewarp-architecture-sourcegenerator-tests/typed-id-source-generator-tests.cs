namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

// Verifies TypedIdSourceGenerator: [TypedId] attribute injection, BCL surface (New/From/Json/Parse),
// and optional EF ValueConverter emission when ValueConverter is present in the compilation.
public class TypedIdSourceGenerator_Tests
{
  private const string SampleSource = """
    namespace Sample;

    [TimeWarp.Architecture.TypedId]
    public readonly partial record struct OrderId;
    """;

  private static readonly MetadataReference[] FrameworkReferences =
    ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .Where(static path => path.Length > 0)
      .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
      .ToArray();

  private static string RunAndConcat(string source, params string[] additionalSources)
  {
    SyntaxTree[] trees = new[] { CSharpSyntaxTree.ParseText(source) }
      .Concat(additionalSources.Select(static s => CSharpSyntaxTree.ParseText(s)))
      .ToArray();

    CSharpCompilation compilation = CSharpCompilation.Create(
      "Test.TypedIds",
      trees,
      FrameworkReferences,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new TypedIdSourceGenerator().AsSourceGenerator()));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));
  }

  public static Task Should_Emit_Public_TypedIdAttribute()
  {
    string generated = RunAndConcat(SampleSource);

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("public sealed class TypedIdAttribute : System.Attribute");
    generated.ShouldContain("AttributeTargets.Struct");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Bcl_Surface_For_Partial_Readonly_Record_Struct()
  {
    string generated = RunAndConcat(SampleSource);

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

  public static Task Should_Skip_Non_Partial_Struct()
  {
    string generated = RunAndConcat("""
      namespace Sample;

      [TimeWarp.Architecture.TypedId]
      public readonly record struct BadId;
      """);

    generated.ShouldContain("public sealed class TypedIdAttribute");
    generated.ShouldNotContain("public readonly partial record struct BadId");
    generated.ShouldNotContain("public static BadId New()");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Emit_Ef_Without_ValueConverter()
  {
    string generated = RunAndConcat(SampleSource);

    generated.ShouldNotContain("ValueConverter");
    generated.ShouldNotContain("ConfigureTypedIdConventions");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Ef_Converters_When_ValueConverter_Present()
  {
    const string efStubs = """
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

    string generated = RunAndConcat(SampleSource, efStubs);

    generated.ShouldContain("internal sealed class OrderIdValueConverter");
    generated.ShouldContain("ValueConverter<global::Sample.OrderId, Guid>");
    generated.ShouldContain("global::Sample.OrderId.From(v)");
    generated.ShouldContain("ConfigureTypedIdConventions");
    generated.ShouldContain("Properties<global::Sample.OrderId>().HaveConversion<OrderIdValueConverter>()");
    generated.ShouldContain("namespace TimeWarp.Architecture.TypedIds.Ef");
    return Task.CompletedTask;
  }
}
