#region Purpose
// Verifies PageRegistry (task 239-001): Navigable = true opt-in, static routes only (TWE009), policy
// carried, navigation placement, descriptions, and agent actions.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using static TimeWarp.Architecture.SourceGenerator.Tests.PageSourceGeneratorTestFixture;

public class PageSourceGenerator_Registry_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageSourceGenerator_Registry_Tests>();

  public static Task Should_List_Only_Navigable_Pages_In_Registry()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public const string SettingsEdit = "settings.edit";
      }
      [Page("/Counter", Navigable = true)]
      public partial class CounterPage { }
      [Page("/settings", Policy = Policies.SettingsEdit, Navigable = true)]
      public partial class SettingsPage { }
      [Page("/Login")]
      public partial class LoginPage { }
      [Page("/Logout", Navigable = false)]
      public partial class LogoutPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("public sealed record PageRegistryEntry(");
    string registry = RegistryOf(generated);
    registry.ShouldContain(
      "new(typeof(global::Test.Pages.CounterPage), \"/Counter\", global::Test.Pages.CounterPage.GetPageUrl(), "
      + "global::Test.Pages.CounterPage.Title, global::Test.Pages.CounterPage.NavIcon, global::Test.Pages.CounterPage.Policy, \"\", \"\", 0) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("new(typeof(global::Test.Pages.SettingsPage), \"/settings\"");
    registry.ShouldNotContain("LoginPage");
    registry.ShouldNotContain("LogoutPage");
    // Equal NavOrder (0), so ordinal route sort: "/Counter" before "/settings".
    registry.IndexOf("CounterPage", StringComparison.Ordinal)
      .ShouldBeLessThan(registry.IndexOf("SettingsPage", StringComparison.Ordinal));

    generated.ShouldContain("partial class CounterPage : INavigableComponent, IStaticRoute, INavigationDestination");
    generated.ShouldContain("partial class LoginPage : INavigableComponent, IStaticRoute\n");
    generated.ShouldContain("partial class LogoutPage : INavigableComponent, IStaticRoute\n");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_A_Description_Literal()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/Counter", Navigable = true, Description = "Demo counter.")]
      public partial class CounterPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("public string Description { get; set; }");
    RegistryOf(generated).ShouldContain("\"Demo counter.\"");
    return Task.CompletedTask;
  }

  public static Task Should_Sort_The_Registry_By_NavOrder_And_Emit_NavGroup()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/Alpha", Navigable = true, NavGroup = "Admin", NavOrder = 200)]
      public partial class AlphaPage { }
      [Page("/Zulu", Navigable = true, NavOrder = 10)]
      public partial class ZuluPage { }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("public string NavGroup { get; set; }");
    generated.ShouldContain("public int NavOrder { get; set; }");
    string registry = RegistryOf(generated);
    registry.ShouldContain("global::Test.Pages.AlphaPage.Policy, \"\", \"Admin\", 200) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.ZuluPage.Policy, \"\", \"\", 10) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    // NavOrder wins over the route: "/Zulu" (10) before "/Alpha" (200).
    registry.IndexOf("ZuluPage", StringComparison.Ordinal)
      .ShouldBeLessThan(registry.IndexOf("AlphaPage", StringComparison.Ordinal));
    return Task.CompletedTask;
  }

  public static Task Should_Read_NavGroup_And_Description_From_Constants()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Groups { public const string Admin = "Admin"; public const string About = "Admin page."; }
      [Page("/Alpha", Navigable = true, NavGroup = Groups.Admin, Description = Groups.About, NavOrder = 200)]
      public partial class AlphaPage { }
      """);

    diagnostics.ShouldBeEmpty();
    RegistryOf(generated).ShouldContain("global::Test.Pages.AlphaPage.Policy, \"Admin page.\", \"Admin\", 200) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_NavPlacement_Bottom_From_The_Member_Name()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages
      {
        [Page("/Alpha", Navigable = true, NavGroup = "Developer", NavOrder = 310, NavPlacement = TimeWarp.Architecture.NavPlacement.Bottom)]
        public partial class AlphaPage { }
        [Page("/Home", Navigable = true, NavOrder = 10, NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top)]
        public partial class HomePage { }
      }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("public NavPlacement NavPlacement { get; set; }");
    string registry = RegistryOf(generated);
    registry.ShouldContain("global::Test.Pages.AlphaPage.Policy, \"\", \"Developer\", 310) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.HomePage.Policy, \"\", \"\", 10) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_NavPlacement_Bottom_From_A_Compile_Time_Constant()
  {
    // The enum is not declared here. An app does not have it in the generator's compilation.
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages
      {
        using static TimeWarp.Architecture.NavPlacement;
        public static class Placements
        {
          public const TimeWarp.Architecture.NavPlacement Pin = TimeWarp.Architecture.NavPlacement.Bottom;
          public const TimeWarp.Architecture.NavPlacement Also = Pin;
          public const int One = 1;
        }
        [Page("/Const", Navigable = true, NavGroup = "Developer", NavOrder = 310, NavPlacement = Placements.Pin)]
        public partial class ConstPage { }
        [Page("/Chain", Navigable = true, NavOrder = 11, NavPlacement = Placements.Also)]
        public partial class ChainPage { }
        [Page("/Static", Navigable = true, NavOrder = 10, NavPlacement = Bottom)]
        public partial class StaticPage { }
        [Page("/Cast", Navigable = true, NavOrder = 20, NavPlacement = (TimeWarp.Architecture.NavPlacement)1)]
        public partial class CastPage { }
        [Page("/CastConst", Navigable = true, NavOrder = 21, NavPlacement = (TimeWarp.Architecture.NavPlacement)Placements.One)]
        public partial class CastConstPage { }
        [Page("/Literal", Navigable = true, NavOrder = 22, NavPlacement = 1)]
        public partial class LiteralPage { }
      }
      """);

    diagnostics.ShouldBeEmpty();
    string registry = RegistryOf(generated);
    registry.ShouldContain("global::Test.Pages.ConstPage.Policy, \"\", \"Developer\", 310) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.ChainPage.Policy, \"\", \"\", 11) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.StaticPage.Policy, \"\", \"\", 10) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.CastPage.Policy, \"\", \"\", 20) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.CastConstPage.Policy, \"\", \"\", 21) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.LiteralPage.Policy, \"\", \"\", 22) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_NavPlacement_Bottom_When_The_Generated_Enum_Is_Unbound()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages
      {
        [Page("/Alpha", Navigable = true, NavGroup = "Developer", NavOrder = 310, NavPlacement = NavPlacement.Bottom)]
        public partial class AlphaPage { }
        [Page("/Home", Navigable = true, NavOrder = 10, NavPlacement = Bottom)]
        public partial class HomePage { }
      }
      """);

    diagnostics.ShouldBeEmpty();
    string registry = RegistryOf(generated);
    registry.ShouldContain("global::Test.Pages.AlphaPage.Policy, \"\", \"Developer\", 310) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.HomePage.Policy, \"\", \"\", 10) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Bottom, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Keep_NavPlacement_Top_For_Any_Value_Other_Than_Bottom()
  {
    // The enum is not declared here. A known value other than 1, and a foreign Bottom name, stay Top.
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages
      {
        public static class Placements
        {
          public const TimeWarp.Architecture.NavPlacement Pin = TimeWarp.Architecture.NavPlacement.Top;
          public const int Two = 2;
        }
        [Page("/TopMember", Navigable = true, NavGroup = "Developer", NavOrder = 310, NavPlacement = TimeWarp.Architecture.NavPlacement.Top)]
        public partial class TopMemberPage { }
        [Page("/TopConst", Navigable = true, NavOrder = 11, NavPlacement = Placements.Pin)]
        public partial class TopConstPage { }
        [Page("/CastZero", Navigable = true, NavOrder = 12, NavPlacement = (TimeWarp.Architecture.NavPlacement)0)]
        public partial class CastZeroPage { }
        [Page("/CastTwo", Navigable = true, NavOrder = 13, NavPlacement = (TimeWarp.Architecture.NavPlacement)Placements.Two)]
        public partial class CastTwoPage { }
        [Page("/Foreign", Navigable = true, NavOrder = 14, NavPlacement = Foo.Bottom)]
        public partial class ForeignPage { }
      }
      """);

    diagnostics.ShouldBeEmpty();
    string registry = RegistryOf(generated);
    registry.ShouldContain("global::Test.Pages.TopMemberPage.Policy, \"\", \"Developer\", 310) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.TopConstPage.Policy, \"\", \"\", 11) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.CastZeroPage.Policy, \"\", \"\", 12) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.CastTwoPage.Policy, \"\", \"\", 13) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    registry.ShouldContain("global::Test.Pages.ForeignPage.Policy, \"\", \"\", 14) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_AgentActions_Fully_Qualified_With_Every_Route()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.States
      {
        public partial class CounterState
        {
          public static class IncrementActionSet
          {
            public sealed class Action { }
          }
        }
      }

      namespace Test.Pages
      {
        using Test.States;

        [Page("/Counter", "/Counter/{Amount:int}", AgentActions = [typeof(CounterState.IncrementActionSet.Action)])]
        public partial class CounterPage { }

        [Page("/Plain", Navigable = true)]
        public partial class PlainPage { }
      }
      """);

    diagnostics.ShouldBeEmpty();
    generated.ShouldContain("public System.Type[] AgentActions { get; set; }");
    string registry = RegistryOf(generated);
    registry.ShouldContain(
      "new(typeof(global::Test.Pages.CounterPage), [\"/Counter\", \"/Counter/{Amount:int}\"], "
      + "[typeof(global::Test.States.CounterState.IncrementActionSet.Action)]),");
    // A page without AgentActions is not an action scope; a non-navigable page with them is.
    generated.ShouldContain("RouteTemplates { get; init; }");
    generated.ShouldNotContain("RouteTemplates { get; set; }");
    registry.ShouldNotContain("RouteTemplatesByPage");
    registry.ShouldNotContain("static PageRegistry()");
    registry.ShouldContain("RouteTemplates = [\"/Plain\"]");
    string scopes = registry.Substring(registry.IndexOf("ActionScopes", StringComparison.Ordinal));
    scopes.ShouldNotContain("PlainPage");
    return Task.CompletedTask;
  }

  public static Task Should_Escape_A_Description_With_Quotes_And_A_Newline()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/Counter", Navigable = true, Description = "Line one.\nSay \"hi\" \\ done.")]
      public partial class CounterPage { }
      """);

    diagnostics.ShouldBeEmpty();
    RegistryOf(generated).ShouldContain("""
      "Line one.\nSay \"hi\" \\ done.", "", 0) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [
      """.Trim());
    return Task.CompletedTask;
  }

  public static Task Should_Carry_Policy_Through_Registry_Entry()
  {
    (string generated, _) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public const string SettingsEdit = "settings.edit";
      }
      [Page("/settings", Policy = Policies.SettingsEdit, Navigable = true)]
      public partial class SettingsPage { }
      """);

    // The entry reads the page's generated Policy member, which is the const expression.
    generated.ShouldContain("public static string Policy { get; } = Policies.SettingsEdit;");
    RegistryOf(generated).ShouldContain("global::Test.Pages.SettingsPage.Policy, \"\", \"\", 0) { NavPlacement = global::TimeWarp.Architecture.NavPlacement.Top, RouteTemplates = [");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE009_And_Exclude_Navigable_Parameterized_Route()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/todoitems/{TodoItemId:Guid}", Navigable = true)]
      public partial class TodoItemPage { }
      [Page("/todoitems", Navigable = true)]
      public partial class TodoItemsPage { }
      """);

    // Run() concatenates driver + per-generator diagnostics, so the one report appears twice.
    Diagnostic twe009 = diagnostics.First(d => d.Id == "TWE009");
    twe009.Severity.ShouldBe(DiagnosticSeverity.Error);
    twe009.GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("TodoItemPage");
    RegistryOf(generated).ShouldNotContain("TodoItemPage)");
    RegistryOf(generated).ShouldContain("typeof(global::Test.Pages.TodoItemsPage)");
    // The page surface itself still generates (only registry membership is refused).
    generated.ShouldContain("public static string GetPageUrl(Guid TodoItemId)");
    generated.ShouldNotContain("partial class TodoItemPage : INavigableComponent, INavigationDestination");
    return Task.CompletedTask;
  }

  public static Task Should_Exclude_Parameterized_Routes_Without_Opt_In()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/todoitems/{TodoItemId:Guid}")]
      public partial class TodoItemPage { }
      """);

    diagnostics.ShouldBeEmpty();
    RegistryOf(generated).ShouldNotContain("TodoItemPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE009_For_Non_Literal_Navigable()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Flags
      {
        public const bool On = true;
      }
      [Page("/Counter", Navigable = Flags.On)]
      public partial class CounterPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE009");
    RegistryOf(generated).ShouldNotContain("CounterPage");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Emit_Registry_Without_Pages()
  {
    (string generated, _) = Run("""
      namespace Test.Pages;
      public partial class NotAPage { }
      """);

    generated.ShouldNotContain("class PageRegistry");
    return Task.CompletedTask;
  }
}
