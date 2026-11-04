#region Purpose
// Multi-route (task 096): [Page("/primary", "/alias", …)] emits one [Route] per path; the primary
// owns GetPageUrl/registry/TWE009; duplicates are TWE010, conflicting declarations TWE011.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using static TimeWarp.Architecture.SourceGenerator.Tests.PageSourceGeneratorTestFixture;

public class PageSourceGenerator_MultiRoute_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageSourceGenerator_MultiRoute_Tests>();

  public static Task Should_Emit_Additional_Static_Routes_With_Primary_Url()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public const string ClientsRead = "clients.read";
      }
      [Page("/clients", "/clients/revenue", "/clients/me-close", Policy = Policies.ClientsRead, Navigable = true)]
      public partial class ClientsPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/clients\")]\n  [Route(\"/clients/revenue\")]\n  [Route(\"/clients/me-close\")]\n  partial class ClientsPage");
    generated.ShouldContain("partial class ClientsPage : INavigableComponent, IStaticRoute, INavigationDestination");
    generated.ShouldContain("public static string GetPageUrl() => global::System.FormattableString.Invariant($\"/clients\");");
    // Policy is emitted once per page, not per route.
    generated.Split("public static string Policy").Length.ShouldBe(2);
    generated.ShouldContain("public static string Policy { get; } = Policies.ClientsRead;");
    // Only the primary route is a registry row. Aliases stay on that row's init-only list.
    string registry = RegistryOf(generated);
    generated.ShouldContain("RouteTemplates { get; init; }");
    generated.ShouldNotContain("RouteTemplates { get; set; }");
    registry.ShouldNotContain("RouteTemplatesByPage");
    registry.ShouldNotContain("static PageRegistry()");
    registry.Split("new(typeof(global::Test.Pages.ClientsPage)", StringSplitOptions.None).Length.ShouldBe(2);
    registry.ShouldContain("new(typeof(global::Test.Pages.ClientsPage), \"/clients\"");
    registry.ShouldContain("RouteTemplates = [\"/clients\", \"/clients/revenue\", \"/clients/me-close\"]");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Root_Alias()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/dashboard", "/", Navigable = true)]
      public partial class DashboardPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/dashboard\")]\n  [Route(\"/\")]\n  partial class DashboardPage");
    generated.ShouldContain("public static string GetPageUrl() => global::System.FormattableString.Invariant($\"/dashboard\");");
    RegistryOf(generated).ShouldContain("typeof(global::Test.Pages.DashboardPage), \"/dashboard\"");
    return Task.CompletedTask;
  }

  public static Task Should_Inherit_Primary_Token_Type_In_Parameterized_Alias()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/clients/{ClientId:string}", "/clients/{ClientId}/revenue")]
      public partial class ClientDetailPage { }
      [Page("/orders/{OrderId:Guid}", "/orders/{OrderId}/lines")]
      public partial class OrderPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/clients/{ClientId}\")]\n  [Route(\"/clients/{ClientId}/revenue\")]\n  partial class ClientDetailPage : INavigableComponent\n");
    generated.ShouldContain("public static string GetPageUrl(string ClientId) => global::System.FormattableString.Invariant($\"/clients/{ClientId}\");");
    generated.Split("[Parameter] public string ClientId").Length.ShouldBe(2);
    // Untyped alias token inherits the primary's Guid constraint.
    generated.ShouldContain("[Route(\"/orders/{OrderId:guid}\")]\n  [Route(\"/orders/{OrderId:guid}/lines\")]");
    generated.ShouldContain("public static string GetPageUrl(Guid OrderId)");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Report_TWE009_For_Parameterized_Alias_Of_Static_Navigable_Primary()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/clients", "/clients/{ClientId}/revenue", Navigable = true)]
      public partial class ClientsPage { }
      """);

    diagnostics.ShouldBeEmpty();
    // The parameterized alias is still emitted, and its token still binds.
    generated.ShouldContain("[Route(\"/clients/{ClientId}/revenue\")]");
    generated.ShouldContain("[Parameter] public string ClientId { get; set; }");
    generated.ShouldContain("partial class ClientsPage : INavigableComponent, IStaticRoute, INavigationDestination");
    generated.ShouldContain("public static string GetPageUrl() =>");
    RegistryOf(generated).ShouldContain("typeof(global::Test.Pages.ClientsPage), \"/clients\"");
    RegistryOf(generated).ShouldContain("RouteTemplates = [\"/clients\", \"/clients/{ClientId}/revenue\"]");
    RegistryOf(generated).ShouldNotContain("RouteTemplatesByPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE009_When_Primary_Is_Parameterized_Even_With_Static_Alias()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/clients/{ClientId}", "/clients", Navigable = true)]
      public partial class ClientDetailPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE009");
    RegistryOf(generated).ShouldNotContain("ClientDetailPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE010_For_Duplicate_Routes()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/clients", "/Clients")]
      public partial class CaseDuplicatePage { }
      [Page("/clients/{ClientId}", "/clients/{Id}")]
      public partial class TokenDuplicatePage { }
      [Page("/a", "/b", "/b")]
      public partial class AliasDuplicatePage { }
      """);

    string[] messages =
    [
      .. diagnostics.Where(d => d.Id == "TWE010")
        .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)),
    ];
    // Exactly one report per offending page, naming the offending route text.
    messages.Length.ShouldBe(3);
    messages.Count(m => m.Contains("CaseDuplicatePage", StringComparison.Ordinal) && m.Contains("'/Clients'", StringComparison.Ordinal)).ShouldBe(1);
    messages.Count(m => m.Contains("TokenDuplicatePage", StringComparison.Ordinal) && m.Contains("'/clients/{Id}'", StringComparison.Ordinal)).ShouldBe(1);
    messages.Count(m => m.Contains("AliasDuplicatePage", StringComparison.Ordinal) && m.Contains("'/b'", StringComparison.Ordinal)).ShouldBe(1);
    generated.ShouldNotContain("partial class CaseDuplicatePage");
    generated.ShouldNotContain("partial class TokenDuplicatePage");
    generated.ShouldNotContain("partial class AliasDuplicatePage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE010_For_Hand_Written_Route_Repeating_Page_Route_But_Allow_Distinct_Alias()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      using Microsoft.AspNetCore.Components;
      [Page("/clients")]
      [Route("/clients")]
      public partial class RepeatPage { }
      [Page("/dashboard")]
      [Route("/")]
      public partial class LegacyWorkaroundPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE010" && d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("RepeatPage"));
    generated.ShouldNotContain("partial class RepeatPage");
    // Pre-096 workaround (hand-written distinct alias) still generates.
    diagnostics.ShouldNotContain(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("LegacyWorkaroundPage"));
    generated.ShouldContain("partial class LegacyWorkaroundPage : INavigableComponent, IStaticRoute");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Report_TWE010_For_Routes_Differing_Only_By_Constraint_Type()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/a/{x:int}", "/a/{y:guid}")]
      public partial class TypedShapePage { }
      """);

    diagnostics.Where(d => d.Id == "TWE010").ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/a/{x:int}\")]");
    generated.ShouldContain("[Route(\"/a/{y:guid}\")]");
    generated.ShouldContain("partial class TypedShapePage");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Typed_Alias_Only_Token_As_Parameter_Without_Changing_GetPageUrl()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/orders", "/orders/{OrderId:Guid}")]
      public partial class OrdersPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/orders/{OrderId:guid}\")]");
    generated.ShouldContain("[Parameter] public Guid OrderId { get; set; }");
    generated.ShouldContain("public static string GetPageUrl() => global::System.FormattableString.Invariant($\"/orders\");");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE011_When_Two_Aliases_Type_One_Alias_Only_Token_Differently()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/p", "/a/{x:int}", "/b/{x:Guid}")]
      public partial class AliasTypeConflictPage { }
      """);

    string[] messages =
    [
      .. diagnostics.Where(d => d.Id == "TWE011")
        .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)),
    ];
    messages.Length.ShouldBe(1);
    messages[0].ShouldContain("AliasTypeConflictPage");
    messages[0].ShouldContain("x");
    messages[0].ShouldContain("an earlier route declares it as");
    generated.ShouldNotContain("partial class AliasTypeConflictPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE005_Once_For_Multi_Route_Page()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/settings", "/prefs", "/preferences", Policy = "SettingsEdit")]
      public partial class MultiPolicyPage { }
      """);

    diagnostics.Count(d => d.Id == "TWE005").ShouldBe(1);
    generated.ShouldNotContain("partial class MultiPolicyPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE011_For_Conflicting_Declarations()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Routes
      {
        public const string Revenue = "/clients/revenue";
      }
      [Page("/clients")]
      [Page("/clients/revenue")]
      public partial class StackedPage { }
      [Page("/clients", Routes.Revenue)]
      public partial class NonLiteralPage { }
      [Page("/orders/{OrderId:Guid}", "/orders/{OrderId:int}/lines")]
      public partial class TypeConflictPage { }
      """);

    string[] messages =
    [
      .. diagnostics.Where(d => d.Id == "TWE011")
        .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)),
    ];
    messages.ShouldContain(m => m.Contains("StackedPage") && m.Contains("more than once"));
    messages.ShouldContain(m => m.Contains("NonLiteralPage") && m.Contains("non-literal"));
    messages.ShouldContain(m => m.Contains("TypeConflictPage") && m.Contains("OrderId"));
    generated.ShouldNotContain("partial class StackedPage");
    generated.ShouldNotContain("partial class NonLiteralPage");
    generated.ShouldNotContain("partial class TypeConflictPage");
    return Task.CompletedTask;
  }
}
