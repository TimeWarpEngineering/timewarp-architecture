namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

// Verifies PageSourceGenerator: [Page("/route")] routing + Policy pit-of-success (task 094).
// Policy must be a const field reference (Policies.X); literals and nameof are TWE005 errors.
// PageRegistry (task 239-001): Navigable = true opt-in, static routes only (TWE009), policy carried,
// and page/registry outputs stay cached across unrelated edits (value-equatable models).
// Multi-route (task 096): [Page("/primary", "/alias", …)] emits one [Route] per path; the primary
// owns GetPageUrl/registry/TWE009; duplicates are TWE010, conflicting declarations TWE011.
public class PageSourceGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageSourceGenerator_Tests>();

  private const string RootNamespace = "TimeWarp.Architecture";

  private static (string Generated, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[] { CSharpSyntaxTree.ParseText(source) },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var options = new Dictionary<string, string> { ["build_property.RootNamespace"] = RootNamespace };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new PageSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    string generated = string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics
      .Concat(result.Results.SelectMany(r => r.Diagnostics))
      .Distinct()
      .ToImmutableArray();
    return (generated, diagnostics);
  }

  public static Task Should_Emit_PageAttribute_In_RootNamespace()
  {
    (string generated, _) = Run("""
      namespace Test.Pages;
      [Page("/Counter")]
      public partial class CounterPage { }
      """);

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class PageAttribute : System.Attribute");
    generated.ShouldContain("public PageAttribute(string RouteTemplate, params string[] AdditionalRoutes)");
    generated.ShouldContain("AllowMultiple = false");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Static_Route_Page_With_Anonymous_Policy_When_Omitted()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/Counter")]
      public partial class CounterPage { }
      """);

    diagnostics.Where(d => d.Id == "TWE005").ShouldBeEmpty();
    generated.ShouldContain("[Route(\"/Counter\")]");
    generated.ShouldContain("partial class CounterPage : INavigableComponent, IStaticRoute");
    generated.ShouldContain("public static string GetPageUrl() => global::System.FormattableString.Invariant($\"/Counter\");");
    generated.ShouldContain("public static string Policy { get; } = Policies.Anonymous;");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Parameterized_Page()
  {
    (string generated, _) = Run("""
      namespace Test.Pages;
      [Page("/todoitems/{TodoItemId:Guid}")]
      public partial class TodoItemPage { }
      """);

    generated.ShouldContain("[Route(\"/todoitems/{TodoItemId:guid}\")]");
    generated.ShouldContain("partial class TodoItemPage : INavigableComponent");
    generated.ShouldNotContain("partial class TodoItemPage : INavigableComponent, IStaticRoute");
    generated.ShouldContain("public static string GetPageUrl(Guid TodoItemId) => global::System.FormattableString.Invariant($\"/todoitems/{TodoItemId}\");");
    generated.ShouldContain("[Parameter] public Guid TodoItemId { get; set; }");
    generated.ShouldContain("public static string Policy { get; } = Policies.Anonymous;");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Policy_Const_Member_Access_Expression()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public const string SettingsEdit = "settings.edit";
      }
      [Page("/settings", Policy = Policies.SettingsEdit)]
      public partial class SettingsPage { }
      """);

    diagnostics.Where(d => d.Id == "TWE005").ShouldBeEmpty();
    generated.ShouldContain("public static string Policy { get; } = Policies.SettingsEdit;");
    // Emit expression passthrough — not identifier-glue Policies.{value}
    generated.ShouldNotContain("= Policies.\"settings.edit\"");
    generated.ShouldNotContain("= Policies.Anonymous;");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Qualified_Policy_Const_Member_Access()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class AuthorizationConstants
      {
        public static class Policies
        {
          public const string CanViewAdminPage = nameof(CanViewAdminPage);
        }
      }
      [Page("/admin", Policy = AuthorizationConstants.Policies.CanViewAdminPage)]
      public partial class AdminPage { }
      """);

    diagnostics.Where(d => d.Id == "TWE005").ShouldBeEmpty();
    generated.ShouldContain("public static string Policy { get; } = AuthorizationConstants.Policies.CanViewAdminPage;");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE005_For_String_Literal_Policy()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      [Page("/settings", Policy = "SettingsEdit")]
      public partial class SettingsPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE005");
    generated.ShouldNotContain("partial class SettingsPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE005_For_Nameof_Policy()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public const string SettingsEdit = "settings.edit";
      }
      [Page("/settings", Policy = nameof(Policies.SettingsEdit))]
      public partial class SettingsPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE005");
    generated.ShouldNotContain("partial class SettingsPage");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE005_For_Unsupported_Policy_Expression()
  {
    (string generated, ImmutableArray<Diagnostic> diagnostics) = Run("""
      namespace Test.Pages;
      public static class Policies
      {
        public static string SettingsEdit => "settings.edit";
      }
      [Page("/settings", Policy = Policies.SettingsEdit + "")]
      public partial class SettingsPage { }
      """);

    diagnostics.ShouldContain(d => d.Id == "TWE005");
    generated.ShouldNotContain("partial class SettingsPage");
    return Task.CompletedTask;
  }

  private static string RegistryOf(string generated)
  {
    int start = generated.IndexOf("public static class PageRegistry", StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0, "PageRegistry was not generated");
    return generated.Substring(start);
  }

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
      + "global::Test.Pages.CounterPage.Title, global::Test.Pages.CounterPage.NavIcon, global::Test.Pages.CounterPage.Policy, \"\"),");
    registry.ShouldContain("new(typeof(global::Test.Pages.SettingsPage), \"/settings\"");
    registry.ShouldNotContain("LoginPage");
    registry.ShouldNotContain("LogoutPage");
    // Ordinal route sort: "/Counter" before "/settings".
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
    RegistryOf(generated).ShouldContain("global::Test.Pages.SettingsPage.Policy, \"\")");
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

  public static Task Should_Emit_Additional_Static_Routes_With_Primary_Url_Crunchit_Clients()
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
    // Only the primary route is a registry row.
    string registry = RegistryOf(generated);
    registry.ShouldContain("new(typeof(global::Test.Pages.ClientsPage), \"/clients\"");
    registry.ShouldNotContain("revenue");
    registry.ShouldNotContain("me-close");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_Root_Alias_Crunchit_Dashboard()
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

  public static Task Should_Inherit_Primary_Token_Type_In_Parameterized_Alias_Crunchit_ClientDetail()
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

  public static Task Should_Cache_Page_And_Registry_Outputs_When_Unrelated_Tree_Is_Added()
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[]
      {
        CSharpSyntaxTree.ParseText("""
          namespace Test.Pages;
          [Page("/Counter", Navigable = true)]
          public partial class CounterPage { }
          [Page("/todoitems/{TodoItemId:Guid}", Navigable = true)]
          public partial class TodoItemPage { }
          [Page("/clients", "/clients/revenue", "/clients/{ClientId}/revenue", Navigable = true)]
          public partial class ClientsPage { }
          [Page("/a", "/A")]
          public partial class DuplicatePage { }
          [Page("/tabs", "/tabs/one")]
          public partial class TabsPage { }
          """),
      },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new PageSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(
        new Dictionary<string, string> { ["build_property.RootNamespace"] = RootNamespace }),
      driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    driver = driver.RunGenerators(compilation);

    compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
      namespace Unrelated;
      [System.Obsolete]
      public sealed class Other { }
      """));
    driver = driver.RunGenerators(compilation);

    GeneratorRunResult run = driver.GetRunResult().Results.Single();
    IncrementalStepRunReason[] reasons =
    [
      .. run.TrackedOutputSteps
        .SelectMany(static pair => pair.Value)
        .SelectMany(static step => step.Outputs)
        .Select(static output => output.Reason),
    ];

    reasons.ShouldNotBeEmpty();
    reasons.ShouldAllBe(static r => r == IncrementalStepRunReason.Cached || r == IncrementalStepRunReason.Unchanged);
    run.Diagnostics.ShouldContain(static d => d.Id == "TWE009");
    run.Diagnostics.ShouldContain(static d => d.Id == "TWE010");

    // Editing a multi-route page's alias string must invalidate its model and emit the new route.
    SyntaxTree original = compilation.SyntaxTrees.ToList()[0];
    SyntaxTree edited = original.WithChangedText(
      Microsoft.CodeAnalysis.Text.SourceText.From(original.ToString().Replace("\"/tabs/one\"", "\"/tabs/two\"", StringComparison.Ordinal)));
    compilation = compilation.ReplaceSyntaxTree(original, edited);
    driver = driver.RunGenerators(compilation);

    GeneratorRunResult editedRun = driver.GetRunResult().Results.Single();
    string editedOutput = string.Join(
      Environment.NewLine,
      editedRun.GeneratedSources.Select(static s => s.SourceText.ToString()));
    editedOutput.ShouldContain("[Route(\"/tabs/two\")]");
    editedOutput.ShouldNotContain("/tabs/one");
    editedRun.TrackedOutputSteps
      .SelectMany(static pair => pair.Value)
      .SelectMany(static step => step.Outputs)
      .Select(static output => output.Reason)
      .ShouldContain(static r => r != IncrementalStepRunReason.Cached && r != IncrementalStepRunReason.Unchanged);
    return Task.CompletedTask;
  }
}
