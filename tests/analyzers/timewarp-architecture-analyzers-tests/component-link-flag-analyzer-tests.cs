#region Purpose
// TWA0050 positive cases: each markup link shape a component can write is reported.
#endregion

#region Design
// Each source is the render-tree code Razor emits for one .razor line, inside a ComponentBase
// type. The {|#n:...|} span is the attribute value (after RuntimeHelpers.TypeCheck), which is the
// expression Razor's #line pragma maps back to the .razor file; for static markup it is the
// AddMarkupContent string.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using static TimeWarp.Architecture.Analyzers.Tests.ComponentLinkTestFixture;

public class Should_Flag_Component_Markup_Links
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Flag_Component_Markup_Links>();

  public static async Task Given_FluentAnchorButton_Href_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public sealed class RolesListPage : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<FluentAnchorButton>(0);
          __builder.AddComponentParameter(1, nameof(FluentAnchorButton.Href), RuntimeHelpers.TypeCheck<string?>({|#0:"/Admin/Roles/New"|}));
          __builder.AddAttribute(2, "data-qa", "NewRole");
          __builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "FluentAnchorButton Href"));
    await test.RunAsync();
  }

  public static async Task Given_FluentAnchor_Href_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public sealed class FooterLinks : ComponentBase
      {
        private string Url => "/About";

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<FluentAnchor>(0);
          __builder.AddComponentParameter(1, "Href", RuntimeHelpers.TypeCheck<string?>({|#0:Url|}));
          __builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "FluentAnchor Href"));
    await test.RunAsync();
  }

  public static async Task Given_NavLink_Href_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class PasskeysPage : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<NavLink>(0);
          __builder.AddAttribute(1, "href", {|#0:"/Login"|});
          __builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "NavLink href"));
    await test.RunAsync();
  }

  public static async Task Given_Anchor_Element_Href_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class RoleRow : ComponentBase
      {
        private string DetailUrl => "/Admin/Roles/1";

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenElement(0, "a");
          __builder.AddAttribute(1, "href", {|#0:DetailUrl|});
          __builder.AddAttribute(2, "data-qa", "RoleDetail");
          __builder.AddContent(3, "Admin");
          __builder.CloseElement();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "<a href>"));
    await test.RunAsync();
  }

  public static async Task Given_FluentButton_Href_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public sealed class SettingsCard : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<FluentButton>(0);
          __builder.AddComponentParameter(1, nameof(FluentButton.Href), RuntimeHelpers.TypeCheck<string?>({|#0:"/Settings"|}));
          __builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "FluentButton Href"));
    await test.RunAsync();
  }

  public static async Task Given_Static_Anchor_Markup_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class NotFoundPage : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.AddMarkupContent(0, "<p>404</p>\n    ");
          __builder.AddMarkupContent(1, {|#0:"<a class=\"twe-status-page__home\" href=\"/\" b-yi2ivb1lf3>Go back home</a>"|});
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "<a href>"));
    await test.RunAsync();
  }

  public static async Task Given_Link_In_A_Child_Content_Lambda_Flags()
  {
    const string source =
      """
      using System;
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class BreadcrumbHost : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          Action<RenderTreeBuilder> crumbs = __builder2 =>
          {
            __builder2.OpenElement(0, "a");
            __builder2.AddAttribute(1, "class", "twe-breadcrumb__link");
            __builder2.AddAttribute(2, "href", {|#0:"/Admin"|});
            __builder2.CloseElement();
          };
          crumbs(__builder);
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "<a href>"));
    await test.RunAsync();
  }

  public static async Task Given_Https_Link_Without_New_Tab_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class AboutCard : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<NavLink>(0);
          __builder.AddAttribute(1, "href", {|#0:"https://example.test/"|});
          __builder.CloseComponent();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "NavLink href"));
    await test.RunAsync();
  }
}
