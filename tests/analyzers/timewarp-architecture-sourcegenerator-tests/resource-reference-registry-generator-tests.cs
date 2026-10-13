#region Purpose
// Verifies the Ask @ registry generator: marked list queries become entries (policy, id type,
// search, UserId stamp), bad shapes and repeated prefixes are skipped, and nothing is emitted
// without the ResourceReferenceType host.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;

public class ResourceReferenceRegistryGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ResourceReferenceRegistryGenerator_Tests>();

  private const string ContractSource = """
    namespace TimeWarp.Foundation.Features
    {
      public interface IApiRequest { }
      public interface IAuthApiRequest : IApiRequest { System.Guid UserId { get; set; } }
    }
    namespace TimeWarp.Architecture.Attributes
    {
      public sealed class ApiEndpointAttribute : System.Attribute { }
      public sealed class EndpointAuthorizeAttribute : System.Attribute
      {
        public string? Policy { get; set; }
      }
      public sealed class ReferenceableResourceAttribute : System.Attribute
      {
        public ReferenceableResourceAttribute(string prefix, string displayName) { }
        public string IdProperty { get; set; } = "";
        public string LabelProperty { get; set; } = "";
        public string ItemsProperty { get; set; } = "Items";
      }
    }
    namespace MyApp.Ids
    {
      public readonly record struct WidgetId(System.Guid Value)
      {
        public static WidgetId From(System.Guid value) => new(value);
      }
    }
    namespace MyApp.Features.Widgets
    {
      using TimeWarp.Architecture.Attributes;

      [ApiEndpoint]
      [EndpointAuthorize(Policy = "widgets.read")]
      [ReferenceableResource("widget", "Widgets", IdProperty = "WidgetId", LabelProperty = "Name")]
      public static class ListWidgets
      {
        public sealed class Query : TimeWarp.Foundation.Features.IApiRequest
        {
          public string? Search { get; set; }
        }
        public sealed class Response
        {
          public Item[] Items { get; } = new Item[0];
        }
        public sealed class Item
        {
          public MyApp.Ids.WidgetId WidgetId { get; }
          public string? Name { get; }
        }
      }

      [ApiEndpoint]
      [EndpointAuthorize]
      [ReferenceableResource("gadget", "Gadgets", ItemsProperty = "Gadgets", IdProperty = "Id", LabelProperty = "Title")]
      public static class ListGadgets
      {
        public sealed class Query : TimeWarp.Foundation.Features.IAuthApiRequest
        {
          public string? Search { get; set; }
          public System.Guid UserId { get; set; }
        }
        public sealed class Response
        {
          public System.Collections.Generic.List<Item> Gadgets { get; } = new();
        }
        public sealed class Item
        {
          public System.Guid Id { get; }
          public string Title { get; } = "";
        }
      }

      [ApiEndpoint]
      [EndpointAuthorize(Policy = "widgets.read")]
      [ReferenceableResource("broken", "Broken", IdProperty = "Id", LabelProperty = "Title")]
      public static class ListBroken
      {
        public sealed class Query : TimeWarp.Foundation.Features.IApiRequest { }
        public sealed class Response
        {
          public Item[] Items { get; } = new Item[0];
        }
        public sealed class Item
        {
          public System.Guid Id { get; }
          public string Title { get; } = "";
        }
      }

      [ApiEndpoint]
      [EndpointAuthorize(Policy = "widgets.read")]
      [ReferenceableResource("widget", "More widgets", IdProperty = "Id", LabelProperty = "Title")]
      public static class SearchWidgets
      {
        public sealed class Query : TimeWarp.Foundation.Features.IApiRequest
        {
          public string? Search { get; set; }
        }
        public sealed class Response
        {
          public Item[] Items { get; } = new Item[0];
        }
        public sealed class Item
        {
          public System.Guid Id { get; }
          public string Title { get; } = "";
        }
      }
    }
    """;

  private const string HostSource = """
    namespace TimeWarp.Architecture.Features.Applications
    {
      public sealed class ResourceReferenceType { }
    }
    """;

  public static Task Should_Emit_An_Entry_Per_Marked_Contract()
  {
    string generated = RunGenerator(ContractSource, HostSource);

    generated.ShouldContain("namespace TimeWarp.Architecture.Features.Applications;");
    generated.ShouldContain("Create<global::MyApp.Features.Widgets.ListWidgets.Query, global::MyApp.Features.Widgets.ListWidgets.Response, global::MyApp.Features.Widgets.ListWidgets.Item>");
    generated.ShouldContain("\"widget\",\n      \"Widgets\",\n      \"WidgetId\",\n      \"widgets.read\",");
    generated.ShouldContain("static search => new global::MyApp.Features.Widgets.ListWidgets.Query { Search = search },");
    generated.ShouldContain("static item => item.WidgetId.ToString(),");
    generated.ShouldContain("static item => item.Name");

    // No policy means any signed-in user; an IAuthApiRequest query gets the mock UserId stamp.
    generated.ShouldContain("\"gadget\",\n      \"Gadgets\",\n      \"Guid\",\n      null,");
    generated.ShouldContain("{ Search = search, UserId = global::System.Guid.NewGuid() }");
    generated.ShouldContain("static response => response.Gadgets,");

    // Sorted by prefix.
    generated.IndexOf("\"gadget\"", StringComparison.Ordinal)
      .ShouldBeLessThan(generated.IndexOf("\"widget\"", StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  public static Task Should_Skip_Bad_Shapes_And_Repeated_Prefixes()
  {
    string generated = RunGenerator(ContractSource, HostSource);

    generated.ShouldNotContain("ListBroken");
    // The repeated "widget" prefix keeps the first contract by name (ListWidgets).
    generated.ShouldNotContain("SearchWidgets");
    generated.ShouldNotContain("More widgets");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Nothing_Without_ResourceReferenceType_Host()
  {
    string generated = RunGenerator(ContractSource, consumerSource: "namespace App { public class Other { } }");

    generated.ShouldBe(string.Empty);
    return Task.CompletedTask;
  }

  private static string RunGenerator(string contractSource, string consumerSource)
  {
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

    var generator = new Analyzers.ResourceReferenceRegistryGenerator();
    Microsoft.CodeAnalysis.GeneratorDriver driver =
      CSharpGeneratorDriver.Create(generator.AsSourceGenerator());

    Microsoft.CodeAnalysis.GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join("\n", result.GeneratedTrees.Select(tree => tree.ToString()));
  }

  private const string MarkerStubs = """
    [assembly: TimeWarp.Architecture.ApiEndpointsEmbedded]
    namespace TimeWarp.Architecture
    {
      [System.AttributeUsage(System.AttributeTargets.Assembly, Inherited = false)]
      internal sealed class ApiEndpointsEmbeddedAttribute : System.Attribute
      {
      }
    }
    """;

  private static Microsoft.CodeAnalysis.MetadataReference CompileContracts(string source)
  {
    string[] trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(System.IO.Path.PathSeparator);
    var compilation = CSharpCompilation.Create(
      "Test.Contracts",
      syntaxTrees:
      [
        CSharpSyntaxTree.ParseText(source),
        CSharpSyntaxTree.ParseText(MarkerStubs),
      ],
      references:
      [
        .. trustedAssemblies
          .Where(static path => path.Length > 0)
          .Select(static path => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(path)),
      ],
      new CSharpCompilationOptions(
        Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Enable));

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
