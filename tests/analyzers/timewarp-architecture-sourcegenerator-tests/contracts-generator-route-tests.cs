#region Purpose
// Task 053-006: static GetRoute returns RouteTemplate; parameterized GetRoute() forwards to
// GetRoute(...); GetAuthQueryParameters only on query-string contracts; generated Guid/DateTime
// members use global::.
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

  public static Task Should_Emit_GetAuthQueryParameters_Only_For_Query_String_Contracts()
  {
    const string authOnly = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles;

      public static partial class CreateRole
      {
          [AuthApiRequest]
          public sealed partial class Command { }
      }
      """;

    const string authAndOpenData = """
      using TimeWarp.Foundation.Features;

      namespace Test.Features.Admin.Roles;

      public static partial class GetRoles
      {
          [OpenDataQueryParameters]
          [AuthApiRequest]
          public sealed partial class Query { }
      }
      """;

    const string authAndQueryString = """
      using TimeWarp.Foundation.Features;

      namespace TimeWarp.Foundation.Features
      {
          public interface IQueryStringRouteProvider { string GetRouteWithQueryString(); }
      }

      namespace Test.Features.Admin.Principals;

      public static partial class ListPrincipals
      {
          [AuthApiRequest]
          public sealed partial class Query : IQueryStringRouteProvider { }
      }
      """;

    string authOnlyGenerated = Run(authOnly);
    authOnlyGenerated.ShouldContain("public global::System.Guid UserId { get; set; }");
    authOnlyGenerated.ShouldContain("global::TimeWarp.Foundation.Features.IAuthApiRequest");
    authOnlyGenerated.ShouldNotContain("GetAuthQueryParameters");

    string openDataGenerated = Run(authAndOpenData);
    openDataGenerated.ShouldContain("GetAuthQueryParameters()");
    openDataGenerated.ShouldContain("GetOpenDataQueryParameters()");

    string queryStringGenerated = Run(authAndQueryString);
    queryStringGenerated.ShouldContain("GetAuthQueryParameters()");
    queryStringGenerated.ShouldNotContain("GetOpenDataQueryParameters()");
    return Task.CompletedTask;
  }
}
