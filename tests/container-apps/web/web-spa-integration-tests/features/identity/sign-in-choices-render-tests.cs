#region Purpose
// Render SignInChoices with HtmlRenderer and pin each sign-in mode.
#endregion

#region Design
// SignInChoices hosts Fluent buttons, so the renderer needs FluentUI services and a no-op JS
// runtime. The component injects nothing. Passkey keeps both passkey actions and treats Microsoft
// 365 as optional. Microsoft365Only and Staff omit those actions. Staff adds the staff-only line.
// A work-account mode with Microsoft 365 known not to be offered says so and shows no passkey
// button. While the offer is still null, that mode shows neither the button nor the unavailable line.
#endregion

namespace SignInChoicesRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;

[TestTag("Unit")]
public class SignInChoices_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SignInChoices_Should_>();

  public static async Task Render_The_Passkey_Card_And_Optional_Microsoft_365()
  {
    string plain = await RenderAsync(new SignInOptions(), offered: false);
    plain.ShouldContain("data-qa=\"ContinueWithPasskey\"");
    plain.ShouldContain("Sign in with a passkey");
    plain.ShouldContain("data-qa=\"CreatePasskey\"");
    plain.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
    plain.ShouldNotContain("data-qa=\"SignInStaffNote\"");
    plain.ShouldNotContain("data-qa=\"SignInLead\"");
    plain.ShouldNotContain("<style");

    string offered = await RenderAsync(new SignInOptions(), offered: true);
    offered.ShouldContain("data-qa=\"ContinueWithMicrosoft365\"");
    offered.ShouldContain("Continue with Microsoft 365");
    ButtonTag(offered, "ContinueWithPasskey").ShouldContain("appearance=\"primary\"");
    ButtonTag(offered, "ContinueWithMicrosoft365").ShouldContain("appearance=\"outline\"");
  }

  public static async Task Render_Microsoft365Only_Without_A_Passkey_Fallback()
  {
    SignInOptions signIn = new() { Mode = SignInMode.Microsoft365Only };
    string offered = await RenderAsync(signIn, offered: true);
    offered.ShouldContain("data-qa=\"SignInLead\"");
    offered.ShouldContain(SignInOptions.WorkAccountLead);
    offered.ShouldContain("data-qa=\"ContinueWithMicrosoft365\"");
    offered.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    offered.ShouldNotContain("data-qa=\"CreatePasskey\"");
    offered.ShouldNotContain("data-qa=\"SignInStaffNote\"");
    ButtonTag(offered, "ContinueWithMicrosoft365").ShouldContain("appearance=\"primary\"");

    string unknown = await RenderAsync(signIn, offered: null);
    unknown.ShouldContain("data-qa=\"SignInLead\"");
    unknown.ShouldNotContain("data-qa=\"Microsoft365Unavailable\"");
    unknown.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
    unknown.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");

    string missing = await RenderAsync(signIn, offered: false);
    missing.ShouldContain("data-qa=\"Microsoft365Unavailable\"");
    missing.ShouldContain(SignInOptions.Microsoft365UnavailableMessage);
    missing.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    missing.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
  }

  public static async Task Render_The_Staff_Line()
  {
    string html = await RenderAsync(new SignInOptions { Mode = SignInMode.Staff }, offered: true);
    html.ShouldContain("data-qa=\"SignInStaffNote\"");
    html.ShouldContain(SignInOptions.StaffNote);
    html.ShouldContain("data-qa=\"SignInLead\"");
    html.ShouldNotContain("data-qa=\"ContinueWithPasskey\"");
    html.ShouldNotContain("data-qa=\"CreatePasskey\"");

    string unknown = await RenderAsync(new SignInOptions { Mode = SignInMode.Staff }, offered: null);
    unknown.ShouldContain("data-qa=\"SignInStaffNote\"");
    unknown.ShouldNotContain("data-qa=\"Microsoft365Unavailable\"");
    unknown.ShouldNotContain("data-qa=\"ContinueWithMicrosoft365\"");
  }

  private static async Task<string> RenderAsync(SignInOptions signIn, bool? offered)
  {
    Dictionary<string, object?> parameters = new()
    {
      ["SignIn"] = signIn,
      ["Microsoft365Offered"] = offered
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
