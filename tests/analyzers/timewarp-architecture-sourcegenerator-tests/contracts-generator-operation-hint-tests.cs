#region Purpose
// Verifies the contracts generator copies title and tool hints (read-only, destructive, and so
// on) from [Operation] and omits them when unset or when the operation name is whitespace.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using static TimeWarp.Architecture.SourceGenerator.Tests.ContractsGeneratorTestFixture;

public class ContractsGenerator_OperationHint_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractsGenerator_OperationHint_Tests>();

  public static Task Should_Copy_Title_And_Tool_Hints_Without_Prefixing_The_Route()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class AppendNote
          {
              [TimeWarp.Contracts.Operation("notes.append", Description = "Append a note to the log.", Title = "Append note")]
              [TimeWarp.Contracts.Destructive(false)]
              [TimeWarp.Contracts.OpenWorld(false)]
              [ApiRoute("api/Roles", HttpVerb.Post)]
              public sealed partial class Command { }
          }
      }
      """;

    MetadataReference[] operation = RealOperationReferences();
    string generated = Run(source, extraReferences: operation);

    generated.ShouldContain("public const string OperationName = \"notes.append\";");
    generated.ShouldContain("public const string OperationDescription = \"Append a note to the log.\";");
    generated.ShouldContain("public const string OperationTitle = \"Append note\";");
    generated.ShouldContain("public const bool OperationDestructive = false;");
    generated.ShouldContain("public const bool OperationOpenWorld = false;");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    generated.ShouldNotContain("Append note notes.append");
    generated.ShouldNotContain("Append note/api");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_A_Bare_Hint_Attribute_As_True()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class PurgeCache
          {
              [TimeWarp.Contracts.Operation("purge_cache")]
              [TimeWarp.Contracts.Destructive]
              [TimeWarp.Contracts.OpenWorld]
              [ApiRoute("api/Roles", HttpVerb.Delete)]
              public sealed partial class Command { }
          }
      }
      """;

    string generated = Run(source, extraReferences: RealOperationReferences());

    generated.ShouldContain("public const string OperationName = \"purge_cache\";");
    generated.ShouldContain("public const bool OperationDestructive = true;");
    generated.ShouldContain("public const bool OperationOpenWorld = true;");
    generated.ShouldNotContain("OperationTitle");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }

  public static Task Should_Omit_Unset_Tool_Hints_And_A_Whitespace_Title()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class GetRoles
          {
              [TimeWarp.Contracts.Operation("roles.list", Description = "Lists roles.", Title = "  ")]
              [ApiRoute("api/Roles", HttpVerb.Get)]
              public sealed partial class Query { }
          }
      }
      """;

    string generated = Run(source, extraReferences: RealOperationReferences());

    generated.ShouldContain("public const string OperationName = \"roles.list\";");
    generated.ShouldContain("public const string OperationDescription = \"Lists roles.\";");
    generated.ShouldNotContain("OperationTitle");
    generated.ShouldNotContain("OperationDestructive");
    generated.ShouldNotContain("OperationOpenWorld");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }

  public static Task Should_Omit_Hints_When_The_Operation_Name_Is_Whitespace()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class PurgeCache
          {
              [TimeWarp.Contracts.Operation("  ", Description = "Purge the cache.", Title = "Purge cache")]
              [TimeWarp.Contracts.Destructive]
              [TimeWarp.Contracts.OpenWorld]
              [ApiRoute("api/Roles", HttpVerb.Delete)]
              public sealed partial class Command { }
          }
      }
      """;

    string generated = Run(source, extraReferences: RealOperationReferences());
    generated.ShouldNotContain("OperationName");
    generated.ShouldNotContain("OperationTitle");
    generated.ShouldNotContain("OperationDestructive");
    generated.ShouldNotContain("OperationOpenWorld");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }

  public static Task Should_Copy_An_Explicit_True_Destructive_Hint()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles
      {
          public static partial class PurgeCache
          {
              [TimeWarp.Contracts.Operation("purge_cache")]
              [TimeWarp.Contracts.Destructive(true)]
              [ApiRoute("api/Roles", HttpVerb.Delete)]
              public sealed partial class Command { }
          }
      }
      """;

    string generated = Run(source, extraReferences: RealOperationReferences());
    generated.ShouldContain("public const string OperationName = \"purge_cache\";");
    generated.ShouldContain("public const bool OperationDestructive = true;");
    generated.ShouldNotContain("OperationOpenWorld");
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    return Task.CompletedTask;
  }
}
