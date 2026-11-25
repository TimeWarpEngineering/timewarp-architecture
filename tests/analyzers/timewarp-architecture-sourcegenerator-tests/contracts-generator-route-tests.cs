#region Purpose
// Task 053-006: static GetRoute returns RouteTemplate; parameterized GetRoute() forwards to
// GetRoute(...); generated Guid/DateTime members use global::.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using static TimeWarp.Architecture.SourceGenerator.Tests.ContractsGeneratorTestFixture;

public class ContractsGenerator_Route_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractsGenerator_Route_Tests>();

  public static Task Should_Emit_Static_GetRoute_As_RouteTemplate_Return()
  {
    const string source = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles;

      public static partial class GetRoles
      {
          [ApiRoute("api/Roles", HttpVerb.Get)]
          public sealed partial class Query { }
      }

      public static partial class CreateRole
      {
          [ApiRoute("api/Roles", HttpVerb.Post)]
          public sealed partial class Command { }
      }
      """;

    string generated = Run(source);

    generated.ShouldContain("""public const string RouteTemplate = "api/Roles";""");
    generated.ShouldContain("public string GetRoute() => RouteTemplate;");
    generated.ShouldNotContain("FormattableString.Invariant");
    generated.ShouldNotContain("public string GetRoute(int");
    return Task.CompletedTask;
  }
}
