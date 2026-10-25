#region Purpose
// Render Microsoft365AccountChoice with HtmlRenderer and pin each offered-method list.
#endregion

#region Design
// The component hosts Fluent buttons, so the renderer needs FluentUI services and a no-op JS
// runtime. It injects nothing. Passkey in the list shows the passkey proof and the already-have
// action. Any list without Passkey keeps only the new-account action.
#endregion

namespace Microsoft365AccountChoiceRender_;

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Features.Identity;

[TestTag("Unit")]
public class Microsoft365AccountChoice_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Microsoft365AccountChoice_Should_>();

  public static async Task Render_The_Passkey_Proof_When_Passkey_Is_Listed()
  {
    string html = await RenderAsync([SignInMethod.Passkey, SignInMethod.Microsoft365]);
    html.ShouldContain("data-qa=\"CreateNewAccount\"");
    html.ShouldContain("Create a new account");
    html.ShouldContain("data-qa=\"AlreadyHaveAccount\"");
    html.ShouldContain("I already have an account");
    html.ShouldContain("prove you already have one with a passkey");
    html.ShouldNotContain("<style");
  }

  public static async Task Render_Create_Only_When_Passkey_Is_Not_Listed()
  {
    string microsoft365 = await RenderAsync([SignInMethod.Microsoft365]);
    string empty = await RenderAsync([]);
    string unknown = await RenderAsync(methods: null);
    foreach (string html in new[] { microsoft365, empty, unknown })
    {
      html.ShouldContain("data-qa=\"CreateNewAccount\"");
      html.ShouldContain("Create a new account to continue.");
      html.ShouldNotContain("data-qa=\"AlreadyHaveAccount\"");
      html.ShouldNotContain("prove you already have one with a passkey");
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
