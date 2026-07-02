#region Purpose
// Verifies the mock-factory registry generator: contracts with GetMockResponseFactory register,
// others don't, and nothing is emitted without a MockWebApiService host.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

public class MockResponseFactoryRegistryGenerator_Tests
{
  private const string ContractSource = """
    namespace TimeWarp.Foundation.Features
    {
      public interface IApiRequest { }
    }
    namespace TimeWarp.Foundation.Types
    {
      public delegate TResponse MockResponseFactory<out TResponse>(TimeWarp.Foundation.Features.IApiRequest request) where TResponse : class;
    }
    namespace MyApp.Features.Widgets
    {
      public static class GetWidget
      {
        public sealed class Query : TimeWarp.Foundation.Features.IApiRequest { }
        public sealed class Response { }
        public static TimeWarp.Foundation.Types.MockResponseFactory<Response> GetMockResponseFactory() => _ => new Response();
      }

      public static class NoFactory
      {
        public sealed class Query : TimeWarp.Foundation.Features.IApiRequest { }
        public sealed class Response { }
      }
    }
    """;

  private const string HostSource = """
    namespace TimeWarp.Architecture.Services
    {
      public class MockWebApiService { }
    }
    """;

  public static void Should_Register_Contract_Factories()
  {
    string generated = RunGenerator(ContractSource, HostSource);

    generated.ShouldContain("namespace TimeWarp.Architecture.Services;");
    generated.ShouldContain("typeof(global::MyApp.Features.Widgets.GetWidget.Query)");
    generated.ShouldContain("global::MyApp.Features.Widgets.GetWidget.GetMockResponseFactory()");
    generated.ShouldNotContain("NoFactory");
  }

  public static void Should_Emit_Nothing_Without_MockWebApiService_Host()
  {
    string generated = RunGenerator(ContractSource, consumerSource: "namespace App { public class NotAMockService { } }");

    generated.ShouldBe(string.Empty);
  }

  private static string RunGenerator(string contractSource, string consumerSource)
  {
    // The generator scans REFERENCED *contracts* assemblies, so compile the contract separately
    // (assembly name "Test.Contracts" satisfies the name filter) and reference it.
    Microsoft.CodeAnalysis.MetadataReference contractReference = CompileContracts(contractSource);

    var compilation = CSharpCompilation.Create(
      "Test.Spa",
      syntaxTrees: [CSharpSyntaxTree.ParseText(consumerSource)],
      references:
      [
        Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        contractReference,
      ],
      new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

    var generator = new Analyzers.MockResponseFactoryRegistryGenerator();
    Microsoft.CodeAnalysis.GeneratorDriver driver =
      CSharpGeneratorDriver.Create(generator.AsSourceGenerator());

    Microsoft.CodeAnalysis.GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString()));
  }

  private static Microsoft.CodeAnalysis.MetadataReference CompileContracts(string source)
  {
    var compilation = CSharpCompilation.Create(
      "Test.Contracts",
      syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
      references: [Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
      new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

    using var peStream = new System.IO.MemoryStream();
    Microsoft.CodeAnalysis.Emit.EmitResult emitResult = compilation.Emit(peStream);
    if (!emitResult.Success)
    {
      string errors = string.Join(Environment.NewLine, emitResult.Diagnostics);
      throw new InvalidOperationException($"Contract assembly failed to compile:{Environment.NewLine}{errors}");
    }

    peStream.Position = 0;
    return Microsoft.CodeAnalysis.MetadataReference.CreateFromStream(peStream);
  }
}
