#region Purpose
// TWA0050 negative cases: external links, opt-outs, click-only buttons, and code outside the rule.
#endregion

#region Design
// The external rule: a constant http(s)/mailto/tel href that opens a new tab (target "_blank" or a
// Target enum member named Blank) or says rel="external" is not reported; a computed href needs
// rel="external". [DirectComponentSideEffect(reason)] on the component or the building member
// opts out; an empty reason does not. A non-component type, a non-anchor element, a null value,
// and a non-SPA compilation are never reported.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using static TimeWarp.Architecture.Analyzers.Tests.ComponentLinkTestFixture;

public class Should_Not_Flag_External_Or_Opted_Out_Links
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Not_Flag_External_Or_Opted_Out_Links>();

  public static async Task Given_External_Https_Link_With_Target_Blank_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public sealed class HomePage : ComponentBase
      {
        private string Docs => "https://docs.example.test/";

        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<NavLink>(0);
          __builder.AddAttribute(1, "href", "https://timewarp.example.test/");
          __builder.AddAttribute(2, "target", "_blank");
          __builder.CloseComponent();

          __builder.OpenComponent<FluentAnchorButton>(3);
          __builder.AddComponentParameter(4, nameof(FluentAnchorButton.Href), RuntimeHelpers.TypeCheck<string?>("https://github.com/"));
          __builder.AddComponentParameter(5, nameof(FluentAnchorButton.Target), RuntimeHelpers.TypeCheck<LinkTarget?>(LinkTarget.Blank));
          __builder.CloseComponent();

          __builder.OpenElement(6, "a");
          __builder.AddAttribute(7, "href", Docs);
          __builder.AddAttribute(8, "rel", "noopener external");
          __builder.CloseElement();

          __builder.AddMarkupContent(9, "<a href=\"mailto:help@example.test\" target=\"_blank\">Mail</a>");
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Component_With_DirectComponentSideEffect_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;
      using TimeWarp.Architecture.Attributes;

      [DirectComponentSideEffect("FluentNav matches the active item on Href and keeps middle-click.")]
      public sealed class NavMenu : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenComponent<FluentAnchor>(0);
          __builder.AddComponentParameter(1, nameof(FluentAnchor.Href), RuntimeHelpers.TypeCheck<string?>("/Counter"));
          __builder.CloseComponent();
        }
      }

      public sealed class SafeMarkdown : ComponentBase
      {
        [DirectComponentSideEffect("Links come from markdown content.")]
        private static void OpenAnchor(RenderTreeBuilder builder, string url)
        {
          builder.OpenElement(0, "a");
          builder.AddAttribute(1, "href", url);
        }

        protected override void BuildRenderTree(RenderTreeBuilder __builder) => OpenAnchor(__builder, "/Feedback");
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Empty_Opt_Out_Reason_Still_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;
      using TimeWarp.Architecture.Attributes;

      [DirectComponentSideEffect(" ")]
      public sealed class Footer : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenElement(0, "a");
          __builder.AddAttribute(1, "href", {|#0:"/About"|});
          __builder.CloseElement();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentLinkAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "<a href>"));
    await test.RunAsync();
  }

  public static async Task Given_FluentButton_With_OnClick_Only_Does_Not_Flag()
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
          __builder.OpenComponent<FluentButton>(0);
          __builder.AddComponentParameter(1, nameof(FluentButton.OnClick), RuntimeHelpers.TypeCheck<EventCallback>(default(EventCallback)));
          __builder.AddAttribute(2, "data-qa", "NewRole");
          __builder.CloseComponent();
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Null_Href_Stylesheet_Link_Or_Non_Component_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.CompilerServices;
      using Microsoft.AspNetCore.Components.Rendering;
      using Microsoft.FluentUI.AspNetCore.Components;

      public sealed class Head : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenElement(0, "link");
          __builder.AddAttribute(1, "href", "app.css");
          __builder.CloseElement();

          __builder.OpenComponent<FluentButton>(2);
          __builder.AddComponentParameter(3, nameof(FluentButton.Href), RuntimeHelpers.TypeCheck<string?>(null));
          __builder.CloseComponent();
        }
      }

      public static class LinkFragments
      {
        public static void Render(RenderTreeBuilder builder)
        {
          builder.OpenElement(0, "a");
          builder.AddAttribute(1, "href", "/Counter");
          builder.CloseElement();
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Non_Spa_Compilation_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.Rendering;

      public sealed class ServerPage : ComponentBase
      {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
          __builder.OpenElement(0, "a");
          __builder.AddAttribute(1, "href", "/");
          __builder.CloseElement();
        }
      }
      """;

    await Test(source, globalConfig: null).RunAsync();
  }
}
