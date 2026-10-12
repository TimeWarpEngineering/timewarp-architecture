#region Purpose
// TWA0032: FluentTextArea renders only inside the shared components/forms/TextArea.razor.
#endregion

#region Design
// Gated on the Blazor WASM SDK like TWA0025. Razor-generated trees and ordinary .cs trees are
// analyzed; other .g.cs trees are exempt. The host is matched by file name (generated tree or
// #line-mapped path). [DirectFluentTextArea("non-empty-reason")] opts a component out; an empty
// reason does not.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Require_The_Shared_TextArea
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Require_The_Shared_TextArea>();

  private const string BlazorWasmGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string Stubs =
    """
    #region Purpose
    // Minimal FluentUI and Blazor stubs for TWA0032 tests.
    #endregion
    namespace Microsoft.FluentUI.AspNetCore.Components
    {
      public class FluentTextArea { }
      public class FluentTextInput { }
    }

    namespace Microsoft.AspNetCore.Components.Rendering
    {
      public class RenderTreeBuilder
      {
        public void OpenComponent<TComponent>(int sequence) { }
        public void AddComponentParameter(int sequence, string parameterName, object? parameterValue) { }
        public void CloseComponent() { }
      }
    }

    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class DirectFluentTextAreaAttribute : System.Attribute
      {
        public string Reason { get; }
        public DirectFluentTextAreaAttribute(string reason) => Reason = reason;
      }
    }
    """;

  private const string RawPage =
    """
    #region Purpose
    // Page rendering a raw FluentTextArea.
    #endregion
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.FluentUI.AspNetCore.Components;

    public class ReplyPage
    {
      protected void BuildRenderTree(RenderTreeBuilder builder)
      {
        builder.OpenComponent<FluentTextArea>(1);
        builder.CloseComponent();
      }
    }
    """;

  private static CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "ReplyPage_razor.g.cs",
    string? globalConfig = BlazorWasmGlobalConfig
  )
  {
    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn) =>
    new DiagnosticResult(id: "TWA0032", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn);

  public static async Task Given_Raw_FluentTextArea_In_Razor_Tree_Flags()
  {
    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = Test(RawPage);
    test.ExpectedDiagnostics.Add(Flag("ReplyPage_razor.g.cs", 11, 5, 45));
    await test.RunAsync();
  }

  public static async Task Given_Hidden_OpenComponent_Reports_On_The_Mapped_Parameter()
  {
    const string source =
      """
      #region Purpose
      // Razor-shaped tree: OpenComponent is hidden, the parameter maps to the .razor tag.
      #endregion
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public class ReplyPage
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
      #line hidden
          builder.OpenComponent<FluentTextArea>(1);
          builder.AddComponentParameter(2, "AriaLabel",
      #line 7 "/src/ReplyPage.razor"
            "Reply"
      #line hidden
          );
          builder.CloseComponent();
      #line default
        }
      }
      """;

    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = Test(source);
    // The verifier matches the unmapped span; line 15 is the "Reply" argument #line maps to the .razor tag.
    test.ExpectedDiagnostics.Add(Flag("ReplyPage_razor.g.cs", 15, 7, 14));
    await test.RunAsync();
  }

  public static async Task Given_Raw_FluentTextArea_In_Ordinary_Cs_Flags()
  {
    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = Test(RawPage, "reply-page.cs");
    test.ExpectedDiagnostics.Add(Flag("reply-page.cs", 11, 5, 45));
    await test.RunAsync();
  }

  public static async Task Given_Similarly_Named_Component_Flags()
  {
    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = Test(RawPage, "features_MyTextArea_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("features_MyTextArea_razor.g.cs", 11, 5, 45));
    await test.RunAsync();
  }

  public static async Task Given_Shared_TextArea_Host_Tree_Does_Not_Flag()
  {
    await Test(RawPage, "components_forms_TextArea_razor.g.cs").RunAsync();
  }

  public static async Task Given_Line_Mapped_To_TextArea_Razor_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Generated tree whose markup maps back to TextArea.razor.
      #endregion
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public class TextArea
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
      #line 40 "/src/web-spa/components/forms/TextArea.razor"
          builder.OpenComponent<FluentTextArea>(1);
      #line default
          builder.CloseComponent();
        }
      }
      """;

    await Test(source, "obj_TextArea_generated_razor.g.cs").RunAsync();
  }

  public static async Task Given_Other_FluentUi_Component_Does_Not_Flag()
  {
    await Test(RawPage.Replace("FluentTextArea", "FluentTextInput", StringComparison.Ordinal)).RunAsync();
  }

  public static async Task Given_Non_Razor_Generated_Tree_Does_Not_Flag()
  {
    await Test(RawPage, "Something.g.cs").RunAsync();
  }

  public static async Task Given_Non_Wasm_Compilation_Does_Not_Flag()
  {
    await Test(RawPage, globalConfig: null).RunAsync();
  }

  public static async Task Given_Opt_Out_With_Reason_Does_Not_Flag()
  {
    string source = RawPage.Replace(
      "public class ReplyPage",
      "[TimeWarp.Architecture.Attributes.DirectFluentTextArea(\"Chat composer sizes itself.\")]\npublic class ReplyPage",
      StringComparison.Ordinal);
    await Test(source).RunAsync();
  }

  public static async Task Given_Opt_Out_With_Empty_Reason_Flags()
  {
    string source = RawPage.Replace(
      "public class ReplyPage",
      "[TimeWarp.Architecture.Attributes.DirectFluentTextArea(\"  \")]\npublic class ReplyPage",
      StringComparison.Ordinal);
    CSharpAnalyzerTest<DirectFluentTextAreaAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("ReplyPage_razor.g.cs", 12, 5, 45));
    await test.RunAsync();
  }
}
