#region Purpose
// TWA0025 intent cases: which MessageBarIntent values and host files flag.
#endregion

#region Design
// The rule is gated on Blazor WASM SDK (same as TWA0022) and only analyzes razor/cshtml-
// generated trees (user @code) plus ordinary .cs trees. Other .g.cs trees are exempt.
// Silence bias: if Intent cannot be statically resolved to a single Error/Success member,
// do not report (false negatives are preferable to false positives in edge cases).
// Opt-out: [PageLocalMessageBar("non-empty-reason")] on the component suppresses all
// diagnostics in that type hierarchy. Empty reason does not opt out.
// The shared MessageBar control (namespace ending in Components) is checked like
// FluentMessageBar; a same-named type in another namespace is not.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using static TimeWarp.Architecture.Analyzers.Tests.PageLocalMessageBarTestFixture;

public class Should_Ban_Page_Local_Outcome_Message_Bars_By_Intent
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Page_Local_Outcome_Message_Bars_By_Intent>();

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
}
