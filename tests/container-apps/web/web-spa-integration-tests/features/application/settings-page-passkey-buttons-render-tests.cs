#region Purpose
// Render Settings and pin that the passkey buttons follow the offered list.
#endregion

#region Design
// HtmlRenderer over StaticRenderHost, the same host as TimeWarpPage_Should_. A scripted
// IWebServerApiService answers the offered-methods read and an empty credential list, so the
// page can finish OnInitializedAsync without a server. Static render is not interactive, so
// the site-settings fetch stays off. CascadingAuthenticationState supplies the cascade Routes
// hosts. The credential list stays; only Create and Add existing follow PasskeyOffered.
#endregion

namespace SettingsPagePasskeyButtonsRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;

[TestTag("Unit")]
public class SettingsPagePasskeyButtons_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SettingsPagePasskeyButtons_Should_>();

  public static async Task Show_Create_And_Add_When_Passkey_Is_Offered()
  {
    string html = await RenderAsync([SignInMethod.Passkey]);
    html.ShouldContain("Passkeys");
    html.ShouldContain("data-qa=\"CreatePasskey\"");
    html.ShouldContain("data-qa=\"AddExistingPasskey\"");
  }

  public static async Task Hide_Create_And_Add_When_Only_Microsoft365_Is_Offered()
  {
    string html = await RenderAsync([SignInMethod.Microsoft365]);
    html.ShouldContain("Passkeys");
    html.ShouldNotContain("data-qa=\"CreatePasskey\"");
    html.ShouldNotContain("data-qa=\"AddExistingPasskey\"");
  }

  private static async Task<string> RenderAsync(IReadOnlyList<SignInMethod> methods)
  {
    ServiceCollection services = StaticRenderHost.CreateServices();
    services.AddSingleton<IWebServerApiService>(new ScriptedApi(methods));
    await using ServiceProvider provider = services.BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PageHost>(ParameterView.Empty)).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private sealed class PageHost : ComponentBase
  {
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<SettingsPage>(0);
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }

  private sealed class ScriptedApi(IReadOnlyList<SignInMethod> methods) : IWebServerApiService
  {
    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      object answer = request switch
      {
        GetEntraSignInOffered.Query => new GetEntraSignInOffered.Response(methods),
        GetCredentials.Query => new GetCredentials.Response([]),
        _ => throw new InvalidOperationException("No scripted response for " + request.GetType().Name + ".")
      };

      return Task.FromResult((OneOf<TResponse, FileResponse, SharedProblemDetails>)(TResponse)answer);
    }
  }
}
