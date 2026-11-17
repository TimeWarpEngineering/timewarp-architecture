#region Purpose
// Verifies the contracts generator copies [Operation] name, description, and group onto the
// generated contract without prefixing the route.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using static TimeWarp.Architecture.SourceGenerator.Tests.ContractsGeneratorTestFixture;

public class ContractsGenerator_Operation_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractsGenerator_Operation_Tests>();

  public static Task Should_Copy_Operation_Name_And_Description_Once()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace TimeWarp.Contracts
      {
          [System.AttributeUsage(System.AttributeTargets.Class)]
          public sealed class OperationAttribute : System.Attribute
          {
              public OperationAttribute(string name) { Name = name; }
              public string Name { get; }
              public string? Description { get; set; }
          }
      }

      namespace TimeWarp.Foundation.Features
      {
          public interface IAuthApiRequest { }
      }

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation("roles.list", Description = "Lists roles.")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              [AuthApiRequest]
              public sealed partial class Query { }
          }
      }
      """;

    string generated = Run(source);

    generated.Split("public const string OperationName = \"roles.list\";").Length.ShouldBe(2);
    generated.ShouldContain("public const string OperationDescription = \"Lists roles.\";");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    generated.ShouldContain("public global::System.Guid UserId { get; set; }");
    AssertGeneratedCompilesWithoutErrors(source);
    return Task.CompletedTask;
  }

  public static Task Should_Escape_Operation_Literals()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace TimeWarp.Contracts
      {
          [System.AttributeUsage(System.AttributeTargets.Class)]
          public sealed class OperationAttribute : System.Attribute
          {
              public OperationAttribute(string name) { Name = name; }
              public string Name { get; }
              public string? Description { get; set; }
          }
      }

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRole
          {
              [TimeWarp.Contracts.Operation("roles.\"quoted\"", Description = "line\nbreak")]
              [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    string generated = Run(source);

    generated.ShouldContain("public const string OperationName = \"roles.\\\"quoted\\\"\";");
    generated.ShouldContain("public const string OperationDescription = \"line\\nbreak\";");
    AssertGeneratedCompilesWithoutErrors(source);
    return Task.CompletedTask;
  }

  public static Task Should_Ignore_Operation_Attribute_In_Another_Namespace()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Other
      {
          [System.AttributeUsage(System.AttributeTargets.Class)]
          public sealed class OperationAttribute : System.Attribute
          {
              public OperationAttribute(string name) { }
          }
      }

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRole
          {
              [Other.Operation("roles.get")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    string generated = Run(source);

    generated.ShouldNotContain("OperationName");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_Operation_From_The_Real_Attribute()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation("roles.list", Description = "Lists roles.")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    CSharpCompilation compilation = CreateCompilation(source, operation);
    INamedTypeSymbol? query = compilation.GetTypeByMetadataName("Test.Features.Admin.Roles.GetRoles+Query");
    query.ShouldNotBeNull();
    query.GetAttributes().Any(IsRealOperation).ShouldBeTrue();

    string generated = Run(source, extraReferences: operation);
    generated.ShouldContain("public const string OperationName = \"roles.list\";");
    generated.ShouldContain("public const string OperationDescription = \"Lists roles.\";");
    generated.ShouldNotContain("OperationGroup");
    return Task.CompletedTask;
  }

  public static Task Should_Omit_Operation_When_The_Real_Attribute_Name_Is_Whitespace()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation(" ", Description = "Lists roles.")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    CSharpCompilation compilation = CreateCompilation(source, operation);
    INamedTypeSymbol query = compilation.GetTypeByMetadataName("Test.Features.Admin.Roles.GetRoles+Query")!;
    query.GetAttributes().Any(IsRealOperation).ShouldBeTrue();

    string generated = Run(source, extraReferences: operation);
    generated.ShouldNotContain("OperationName");
    generated.ShouldNotContain("OperationDescription");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }

  public static Task Should_Omit_A_Whitespace_Operation_Description()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation("roles.list", Description = "  ")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    INamedTypeSymbol query = CreateCompilation(source, operation)
      .GetTypeByMetadataName("Test.Features.Admin.Roles.GetRoles+Query")!;
    query.GetAttributes().Any(IsRealOperation).ShouldBeTrue();

    string generated = Run(source, extraReferences: operation);
    generated.ShouldContain("public const string OperationName = \"roles.list\";");
    generated.ShouldNotContain("OperationDescription");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_Operation_Group_Without_Prefixing_The_Route()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetSymbol
          {
              [TimeWarp.Contracts.Operation("get_symbol", Description = "Resolve one symbol.", Group = "symbols")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    string generated = Run(source, extraReferences: operation);

    generated.ShouldContain("public const string OperationName = \"get_symbol\";");
    generated.ShouldContain("public const string OperationDescription = \"Resolve one symbol.\";");
    generated.ShouldContain("public const string OperationGroup = \"symbols\";");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    generated.ShouldContain("public string GetRoute() => RouteTemplate;");
    generated.ShouldNotContain("symbols get_symbol");
    generated.ShouldNotContain("symbols/api");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_Dotted_And_Spaced_Operation_Groups()
  {
    const string dotted = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class RenameSymbol
          {
              [TimeWarp.Contracts.Operation("rename_symbol", Group = "edit.symbol")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    const string spaced = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class RenameParameter
          {
              [TimeWarp.Contracts.Operation("rename_parameter", Group = "edit symbol")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    string dottedGenerated = Run(dotted, extraReferences: operation);
    string spacedGenerated = Run(spaced, extraReferences: operation);

    dottedGenerated.ShouldContain("public const string OperationName = \"rename_symbol\";");
    dottedGenerated.ShouldContain("public const string OperationGroup = \"edit.symbol\";");
    dottedGenerated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    spacedGenerated.ShouldContain("public const string OperationName = \"rename_parameter\";");
    spacedGenerated.ShouldContain("public const string OperationGroup = \"edit symbol\";");
    spacedGenerated.ShouldNotContain("edit rename_parameter");
    return Task.CompletedTask;
  }

  public static Task Should_Omit_A_Whitespace_Operation_Group()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation("roles.list", Description = "Lists roles.", Group = "  ")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    INamedTypeSymbol query = CreateCompilation(source, operation)
      .GetTypeByMetadataName("Test.Features.Admin.Roles.GetRoles+Query")!;
    query.GetAttributes().Any(IsRealOperation).ShouldBeTrue();

    string generated = Run(source, extraReferences: operation);
    generated.ShouldContain("public const string OperationName = \"roles.list\";");
    generated.ShouldContain("public const string OperationDescription = \"Lists roles.\";");
    generated.ShouldNotContain("OperationGroup");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }
}
