#region Purpose
// Render Microsoft365AccountChoice with HtmlRenderer and pin each sign-in mode.
#endregion

#region Design
// The component hosts Fluent buttons, so the renderer needs FluentUI services and a no-op JS
// runtime. It injects nothing. Passkey shows create-account and the passkey proof. Microsoft365Only
// omits that proof and the staff line. Staff shows the staff line and omits the proof.
#endregion

namespace Microsoft365AccountChoiceRender_;

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
public class Microsoft365AccountChoice_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Microsoft365AccountChoice_Should_>();

  public static async Task Render_Both_Actions_In_Passkey_Mode()
  {
    string html = await RenderAsync(new SignInOptions());
    html.ShouldContain("data-qa=\"CreateNewAccount\"");
    html.ShouldContain("Create a new account");
    html.ShouldContain("data-qa=\"AlreadyHaveAccount\"");
    html.ShouldContain("I already have an account");
    html.ShouldContain("prove you already have one with a passkey");
    html.ShouldNotContain("data-qa=\"ChooseStaffNote\"");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Microsoft365Only_Without_The_Passkey_Proof_Or_Staff_Line()
  {
    string html = await RenderAsync(new SignInOptions { Mode = SignInMode.Microsoft365Only });
    html.ShouldContain("data-qa=\"CreateNewAccount\"");
    html.ShouldContain("Create a new account to continue.");
    html.ShouldNotContain("data-qa=\"AlreadyHaveAccount\"");
    html.ShouldNotContain("data-qa=\"ChooseStaffNote\"");
    html.ShouldNotContain("prove you already have one with a passkey");
  }

  public static async Task Render_The_Staff_Line_Without_The_Passkey_Proof()
  {
    string html = await RenderAsync(new SignInOptions { Mode = SignInMode.Staff });
    html.ShouldContain("data-qa=\"ChooseStaffNote\"");
    html.ShouldContain(SignInOptions.StaffNote);
    html.ShouldContain("data-qa=\"CreateNewAccount\"");
    html.ShouldNotContain("data-qa=\"AlreadyHaveAccount\"");
    html.ShouldNotContain("prove you already have one with a passkey");
  }

  private static async Task<string> RenderAsync(SignInOptions signIn)
  {
    Dictionary<string, object?> parameters = new()
    {
      ["SignIn"] = signIn
    };
    await using ServiceProvider services = Services();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);
    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<Microsoft365AccountChoice>(ParameterView.FromDictionary(parameters))).ToHtmlString());
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

  private sealed class NoopJsRuntime : IJSRuntime
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
      ValueTask.FromResult(default(TValue)!);
  }
}
