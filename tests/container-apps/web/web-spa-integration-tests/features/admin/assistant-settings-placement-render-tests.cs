#region Purpose
// Pin that the Assistant section lives on Admin → AI and not on Admin → Authentication.
#endregion

#region Design
// HtmlRenderer over StaticRenderHost. Static render is not interactive, so neither page fetches
// site settings. AI still renders the Assistant section (the unloaded placeholder carries the
// same title, description, and data-qa). Authentication renders its policy page with no
// Assistant section. CascadingAuthenticationState supplies the cascade Routes hosts.
#endregion

namespace AssistantSettingsPlacementRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Admin.Ai;
using TimeWarp.Architecture.Features.Admin.SiteSettings;

[TestTag("Unit")]
public class AssistantSettingsPlacement_Should_
{
  private const string AssistantDescription =
    "The name the in-app assistant goes by on the top-bar button, Ctrl-K, the panel and its greeting. Its avatar is generated from the name.";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AssistantSettingsPlacement_Should_>();

  public static async Task Show_The_Assistant_Section_On_The_Ai_Page()
  {
    string html = await RenderAsync<AiPage>();
    html.ShouldContain("data-qa=\"AssistantSettings\"");
    html.ShouldContain("Assistant");
    html.ShouldContain(AssistantDescription);
  }

  public static async Task Omit_The_Assistant_Section_From_Authentication()
  {
    string html = await RenderAsync<AuthenticationPage>();
    html.ShouldContain("Microsoft 365 sign-in policy");
    html.ShouldNotContain("data-qa=\"AssistantSettings\"");
    html.ShouldNotContain("data-qa=\"AssistantName\"");
    html.ShouldNotContain(AssistantDescription);
  }

  private static async Task<string> RenderAsync<TPage>() where TPage : IComponent
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices().BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageHost<TPage>>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private sealed class PageHost<TPage> : ComponentBase where TPage : IComponent
  {
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<TPage>(0);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }
}
