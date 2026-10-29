#region Purpose
// TWA0037: FluentMessageBar renders only inside the shared components/MessageBar.razor control
// and the shell components/MessageBars.razor host.
#endregion

#region Design
// Gated on the Blazor WASM SDK like TWA0032, so these stubbed fixtures only report under the
// global config. Razor-generated trees and ordinary .cs trees are analyzed; other .g.cs trees
// are exempt. Both hosts are matched by their components path (generated tree name or
// #line-mapped path, either separator); a same-named file elsewhere is not. There is no opt-out
// attribute.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Require_The_Shared_MessageBar
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Require_The_Shared_MessageBar>();

  private const string BlazorWasmGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string Stubs =
    """
    #region Purpose
    // Minimal FluentUI and Blazor stubs for TWA0037 tests.
    #endregion
    namespace Microsoft.FluentUI.AspNetCore.Components
    {
      public class FluentMessageBar { }
      public class FluentButton { }
    }

    namespace TimeWarp.Architecture.Components
    {
      public class MessageBar { }
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
    """;

  private const string RawPage =
    """
    #region Purpose
    // Page rendering a raw FluentMessageBar.
    #endregion
    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.FluentUI.AspNetCore.Components;

    public class DriftPage
    {
      protected void BuildRenderTree(RenderTreeBuilder builder)
      {
        builder.OpenComponent<FluentMessageBar>(1);
        builder.CloseComponent();
      }
    }
    """;

  private static CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "DriftPage_razor.g.cs",
    string? globalConfig = BlazorWasmGlobalConfig
  )
  {
    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn) =>
    new DiagnosticResult(id: "TWA0037", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn);

  public static async Task Given_Raw_FluentMessageBar_In_Razor_Tree_Flags()
  {
    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = Test(RawPage);
    test.ExpectedDiagnostics.Add(Flag("DriftPage_razor.g.cs", 11, 5, 47));
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

      public class DriftPage
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
      #line hidden
          builder.OpenComponent<FluentMessageBar>(1);
          builder.AddComponentParameter(2, "Title",
      #line 7 "/src/DriftPage.razor"
            "Configuration drift"
      #line hidden
          );
          builder.CloseComponent();
      #line default
        }
      }
      """;

    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = Test(source);
    // The verifier matches the unmapped span; line 15 is the "Configuration drift" argument.
    test.ExpectedDiagnostics.Add(Flag("DriftPage_razor.g.cs", 15, 7, 28));
    await test.RunAsync();
  }

  public static async Task Given_Raw_FluentMessageBar_In_Ordinary_Cs_Flags()
  {
    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = Test(RawPage, "drift-page.cs");
    test.ExpectedDiagnostics.Add(Flag("drift-page.cs", 11, 5, 47));
    await test.RunAsync();
  }

  public static async Task Given_Shared_MessageBar_Tree_Does_Not_Flag()
  {
    await Test(RawPage, "components_MessageBar_razor.g.cs").RunAsync();
  }

  public static async Task Given_Shell_MessageBars_Host_Tree_Does_Not_Flag()
  {
    await Test(RawPage, "components_MessageBars_razor.g.cs").RunAsync();
  }

  public static async Task Given_Razor_Generator_Hint_Path_For_Hosts_Does_Not_Flag()
  {
    await Test(RawPage, "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/components/MessageBar_razor.g.cs").RunAsync();
    await Test(RawPage, "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/components/MessageBars_razor.g.cs").RunAsync();
  }

  public static async Task Given_Line_Mapped_To_MessageBar_Razor_Does_Not_Flag()
  {
    const string source =
      """
      #region Purpose
      // Generated tree whose markup maps back to MessageBar.razor through a Windows path.
      #endregion
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public class MessageBarControl
      {
        protected void BuildRenderTree(RenderTreeBuilder builder)
        {
      #line 70 "C:\src\web-spa\components\MessageBar.razor"
          builder.OpenComponent<FluentMessageBar>(1);
      #line default
          builder.CloseComponent();
        }
      }
      """;

    await Test(source, "obj_MessageBar_generated_razor.g.cs").RunAsync();
  }

  public static async Task Given_Same_Named_Component_In_Other_Folder_Flags()
  {
    const string path = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/features/notes/MessageBar_razor.g.cs";
    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = Test(RawPage, path);
    test.ExpectedDiagnostics.Add(Flag(path, 11, 5, 47));
    await test.RunAsync();
  }

  public static async Task Given_Similarly_Named_Component_Flags()
  {
    CSharpAnalyzerTest<DirectFluentMessageBarAnalyzer, RoslynTestVerifier> test = Test(RawPage, "components_MyMessageBar_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("components_MyMessageBar_razor.g.cs", 11, 5, 47));
    await test.RunAsync();
  }

  public static async Task Given_Shared_MessageBar_Usage_Does_Not_Flag()
  {
    string source = RawPage
      .Replace("FluentMessageBar", "MessageBar", StringComparison.Ordinal)
      .Replace("using Microsoft.FluentUI.AspNetCore.Components;", "using TimeWarp.Architecture.Components;", StringComparison.Ordinal);
    await Test(source).RunAsync();
  }

  public static async Task Given_Other_FluentUi_Component_Does_Not_Flag()
  {
    await Test(RawPage.Replace("FluentMessageBar", "FluentButton", StringComparison.Ordinal)).RunAsync();
  }

  public static async Task Given_Non_Razor_Generated_Tree_Does_Not_Flag()
  {
    await Test(RawPage, "Something.g.cs").RunAsync();
  }

  public static async Task Given_Non_Wasm_Compilation_Does_Not_Flag()
  {
    await Test(RawPage, globalConfig: null).RunAsync();
  }
}
