#region Purpose
// TWA0025: Operation-outcome message bars (Error/Success) must render only in the shell's
// MessageBars.razor host, not scattered across pages.
#endregion

#region Design
// The rule is gated on Blazor WASM SDK (same as TWA0022) and only analyzes razor/cshtml-
// generated trees (user @code) plus ordinary .cs trees. Other .g.cs trees are exempt.
// Silence bias: if Intent cannot be statically resolved to a single Error/Success member,
// do not report (false negatives are preferable to false positives in edge cases).
// Opt-out: [PageLocalMessageBar("non-empty-reason")] on the component suppresses all
// diagnostics in that type hierarchy. Empty reason does not opt out.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Ban_Page_Local_Outcome_Message_Bars
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Page_Local_Outcome_Message_Bars>();

  private const string BlazorWasmGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string FluentUIStubs =
    """
    #region Purpose
    // Minimal FluentUI and Blazor stubs for TWA0025 tests.
    #endregion
    namespace Microsoft.FluentUI.AspNetCore.Components
    {
      public enum MessageBarIntent
      {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3
      }

      public class FluentMessageBar
      {
        public MessageBarIntent? Intent { get; set; }
        public string? Title { get; set; }
      }
    }

    namespace Microsoft.AspNetCore.Components.Rendering
    {
      public class RenderTreeBuilder
      {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddComponentParameter(int sequence, string parameterName, object? parameterValue) { }
        public void AddAttribute(int sequence, string attributeName, object? attributeValue) { }
        public void CloseComponent() { }
      }
    }

    namespace Microsoft.AspNetCore.Components.CompilerServices
    {
      public static class RuntimeHelpers
      {
        public static T TypeCheck<T>(T value) => value;
      }
    }

    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class PageLocalMessageBarAttribute : System.Attribute
      {
        public string Reason { get; }
        public PageLocalMessageBarAttribute(string reason) => Reason = reason;
      }
    }
    """;

  private static CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature_razor.g.cs",
    string? globalConfig = BlazorWasmGlobalConfig
  )
  {
    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", FluentUIStubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string intentMember) =>
    new DiagnosticResult(id: "TWA0025", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(intentMember);

  public static async Task Given_Error_Intent_In_Razor_Generated_Tree_Flags()
  {
    const string source =
      """
      #region Purpose
      // Razor page with a local Error message bar.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class SettingsPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "SettingsPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("SettingsPage_razor.g.cs", 17, 50, 72, "Error"));
    await test.RunAsync();
  }

  public static async Task Given_Success_Intent_In_Razor_Generated_Tree_Flags()
  {
    const string source =
      """
      #region Purpose
      // Razor page with a local Success message bar.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class SuccessPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Success));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Success");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "SuccessPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("SuccessPage_razor.g.cs", 17, 50, 74, "Success"));
    await test.RunAsync();
  }

  public static async Task Given_Warning_Intent_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Razor page with a static Warning message (allowed).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class WarningPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Warning));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Warning");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "WarningPage_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Info_Intent_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Razor page with a static Info message (allowed).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class InfoPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Info));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Info");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "InfoPage_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Error_Intent_In_MessageBars_Host_File_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // The shell's MessageBars host (exempted by file name).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class MessageBars
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "components_MessageBars_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Error_Intent_With_Line_Mapping_To_MessageBars_Razor_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Generated tree with #line mapping to MessageBars.razor.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class OtherComponent
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
      #line 20 "components/MessageBars.razor"
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error)
      #line default
      );
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "Other_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Component_With_OptOut_Attribute_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Component with PageLocalMessageBar opt-out (style guide showcase).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using TimeWarp.Architecture.Attributes;

      [PageLocalMessageBar("Style guide showcases the component itself")]
      public class StyleGuidePage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "StyleGuidePage_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Component_With_Empty_OptOut_Reason_Still_Flags()
  {
    const string source =
      """
      #region Purpose
      // Component with empty PageLocalMessageBar reason (does not opt out).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using TimeWarp.Architecture.Attributes;

      [PageLocalMessageBar("")]
      public class BadPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "BadPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("BadPage_razor.g.cs", 19, 50, 72, "Error"));
    await test.RunAsync();
  }

  public static async Task Given_Non_Constant_Intent_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Dynamic intent that cannot be statically resolved (silence bias).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class DynamicPage
      {
        private bool IsRequired { get; set; }
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(
              IsRequired ? MessageBarIntent.Success : MessageBarIntent.Info));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Dynamic");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "DynamicPage_razor.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_No_Blazor_WebAssembly_Config_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Server-side code (not a Blazor WASM compilation).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class ServerEndpoint
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "Endpoint.cs", globalConfig: null);
    await test.RunAsync();
  }

  public static async Task Given_Non_Razor_Generated_Tree_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // A .g.cs tree that is not razor-generated (other generator).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class GeneratedOther
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, nameof(FluentMessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(FluentMessageBar.Title), "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "Foo.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_Error_Intent_With_AddAttribute_Instead_Of_AddComponentParameter_Flags()
  {
    const string source =
      """
      #region Purpose
      // Older razor-generated shape using AddAttribute instead of AddComponentParameter.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class OldStylePage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddAttribute(2, "Intent",
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddAttribute(3, "Title", "Error");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "OldStylePage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("OldStylePage_razor.g.cs", 17, 50, 72, "Error"));
    await test.RunAsync();
  }

  public static async Task Given_Nullable_MessageBarIntent_TypeCheck_With_Line_Mapping_Flags()
  {
    // Real shape from web-spa build: TypeCheck<MessageBarIntent?> with #line mapping split
    // across nameof and value expressions (nullable variant).
    const string source =
      """
      #region Purpose
      // Real generated shape with nullable Intent and split #line mapping.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class StyleGuidePage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(155);
          builder.AddComponentParameter(156, nameof(FluentMessageBar.
      #line 125 "StyleGuidePage.razor"
      Intent
      #line default
      #line hidden
      ), RuntimeHelpers.TypeCheck<MessageBarIntent?>(
      #line 125 "StyleGuidePage.razor"
      MessageBarIntent.Success
      #line default
      #line hidden
      ));
          builder.AddComponentParameter(157, nameof(FluentMessageBar.Title), "Success");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "StyleGuidePage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("StyleGuidePage_razor.g.cs", 23, 1, 25, "Success"));
    await test.RunAsync();
  }
}
