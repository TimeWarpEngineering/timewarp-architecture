#region Purpose
// TWA0025 opt-out, gating, and edge cases.
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

public class Should_Ban_Page_Local_Outcome_Message_Bars_With_Opt_Out_And_Edge_Cases
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Page_Local_Outcome_Message_Bars_With_Opt_Out_And_Edge_Cases>();

  public static async Task Given_Component_With_OptOut_Attribute_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Component with PageLocalMessageBar opt-out (design system showcase).
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using TimeWarp.Architecture.Attributes;

      [PageLocalMessageBar("Design system showcases the component itself")]
      public class DesignSystemPage
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

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "DesignSystemPage_razor.g.cs");
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

      public class DesignSystemPage
      {
        protected RenderTreeBuilder __builder;

        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<FluentMessageBar>(155);
          builder.AddComponentParameter(156, nameof(FluentMessageBar.
      #line 125 "DesignSystemPage.razor"
      Intent
      #line default
      #line hidden
      ), RuntimeHelpers.TypeCheck<MessageBarIntent?>(
      #line 125 "DesignSystemPage.razor"
      MessageBarIntent.Success
      #line default
      #line hidden
      ));
          builder.AddComponentParameter(157, nameof(FluentMessageBar.Title), "Success");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "DesignSystemPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("DesignSystemPage_razor.g.cs", 23, 1, 25, "Success"));
    await test.RunAsync();
  }

  public static async Task Given_Error_Intent_On_Shared_MessageBar_Flags()
  {
    const string source =
      """
      #region Purpose
      // Razor page with a local Error bar through the shared MessageBar control.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using TimeWarp.Architecture.Components;

      public class SharedBarPage
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<MessageBar>(1);
          builder.AddComponentParameter(2, nameof(MessageBar.Intent),
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.AddComponentParameter(3, nameof(MessageBar.Title), "Save failed");
          builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<PageLocalMessageBarAnalyzer, RoslynTestVerifier> test = Test(source, "SharedBarPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("SharedBarPage_razor.g.cs", 16, 50, 72, "Error"));
    await test.RunAsync();
  }

  public static async Task Given_Same_Named_Type_Outside_Components_Namespace_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // A feature type that happens to be called MessageBar is not the shared control.
      #endregion
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;

      public class NotesPage
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
          builder.OpenComponent<TimeWarp.Architecture.Features.Notes.MessageBar>(1);
          builder.AddComponentParameter(2, "Intent",
            RuntimeHelpers.TypeCheck<MessageBarIntent>(MessageBarIntent.Error));
          builder.CloseComponent();
        }
      }
      """;

    await Test(source, "NotesPage_razor.g.cs").RunAsync();
  }
}
