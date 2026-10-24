#region Purpose
// Render SignInChoices with HtmlRenderer and pin each offered-method list.
#endregion

#region Design
// SignInChoices hosts Fluent buttons, so the renderer needs FluentUI services and a no-op JS
// runtime. The component injects nothing. Buttons follow SignInMethod declaration order.
// Null paints nothing. An empty list is the work-account lead plus the unavailable line.
// Passkey is primary and keeps account creation. Microsoft 365 is outline beside passkey and
// primary when it is the only method.
#endregion

namespace SignInChoicesRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Features.Identity;

[TestTag("Unit")]
public class SignInChoices_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInChoices_Should_>();

  public static async Task Render_Nothing_While_The_List_Is_Unknown()
  {
    string html = await RenderAsync(methods: null);
    html.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    html.ShouldNotContain("data-qa=\"CreatePasskey\"");
    html.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
    html.ShouldNotContain("data-qa=\"SignInLead\"");
    html.ShouldNotContain("data-qa=\"Microsoft365Unavailable\"");
  }

  public static async Task Render_The_Unavailable_Line_For_An_Empty_List()
  {
    string html = await RenderAsync([]);
    html.ShouldContain("data-qa=\"SignInLead\"");
    html.ShouldContain(SignInCopy.WorkAccountLead);
    html.ShouldContain("data-qa=\"Microsoft365Unavailable\"");
    html.ShouldContain(SignInCopy.Microsoft365UnavailableMessage);
    html.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    html.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
  }

  public static async Task Render_Passkey_Only()
  {
    string html = await RenderAsync([SignInMethod.Passkey]);
    html.ShouldContain("data-qa=\"ContinueWithPasskey\"");
    html.ShouldContain("Sign in with a passkey");
    html.ShouldContain("data-qa=\"CreatePasskey\"");
    html.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
    html.ShouldNotContain("data-qa=\"SignInLead\"");
    html.ShouldNotContain("<style");
    ButtonTag(html, "ContinueWithPasskey").ShouldContain("appearance=\"primary\"");
  }

  public static async Task Render_Microsoft365_Only()
  {
    string html = await RenderAsync([SignInMethod.Microsoft365]);
    html.ShouldContain("data-qa=\"SignInLead\"");
    html.ShouldContain(SignInCopy.WorkAccountLead);
    html.ShouldContain("data-qa=\"ContinueWithMicrosoft365\"");
    html.ShouldContain("Continue with Microsoft 365");
    html.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    html.ShouldNotContain("data-qa=\"CreatePasskey\"");
    html.ShouldNotContain("data-qa=\"Microsoft365Unavailable\"");
    ButtonTag(html, "ContinueWithMicrosoft365").ShouldContain("appearance=\"primary\"");
  }

  public static async Task Render_Both_Methods_In_Enum_Order()
  {
    string configuredBackwards = await RenderAsync([SignInMethod.Microsoft365, SignInMethod.Passkey]);
    string configuredForwards = await RenderAsync([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    foreach (string html in new[] { configuredBackwards, configuredForwards })
    {
      html.ShouldContain("data-qa=\"ContinueWithPasskey\"");
      html.ShouldContain("data-qa=\"ContinueWithMicrosoft365\"");
      html.ShouldContain("data-qa=\"CreatePasskey\"");
      html.ShouldNotContain("data-qa=\"SignInLead\"");
      ButtonTag(html, "ContinueWithPasskey").ShouldContain("appearance=\"primary\"");
      ButtonTag(html, "ContinueWithMicrosoft365").ShouldContain("appearance=\"outline\"");
      html.IndexOf("data-qa=\"ContinueWithPasskey\"", StringComparison.Ordinal)
        .ShouldBeLessThan(html.IndexOf("data-qa=\"ContinueWithMicrosoft365\"", StringComparison.Ordinal));
    }
  }

  private static async Task<string> RenderAsync(IReadOnlyList<SignInMethod>? methods)
  {
    Dictionary<string, object?> parameters = new()
    {
      ["Methods"] = methods
    };
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<SignInChoices>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  private static ServiceProvider Services()
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddSingleton<IJSRuntime, NoopJsRuntime>();
    return services.BuildServiceProvider();
  }

  private static string ButtonTag(string html, string dataQa)
  {
    string marker = "data-qa=\"" + dataQa + "\"";
    int at = html.IndexOf(marker, StringComparison.Ordinal);
    at.ShouldBeGreaterThan(-1, dataQa);
    int start = html.LastIndexOf('<', at, StringComparison.Ordinal);
    int end = html.IndexOf('>', at, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }
}
