namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Verifies PageSourceGenerator: [Page("/route")] routing + Policy pit-of-success (task 094).
// Policy must be a const field reference (Policies.X); literals and nameof are TWE005 errors.
public class PageSourceGenerator_Tests
{
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
    generated.ShouldContain("public PageAttribute(string RouteTemplate)");
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
}
