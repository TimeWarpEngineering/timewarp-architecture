#region Purpose
// Render CredentialList with HtmlRenderer and assert the 248-001 row contract: nickname title,
// provider/attachment/client context line, created stamp, monospace fingerprint, and the inline
// rename editor auto-opening (prefilled) only for the pending credential id; plus task 246's
// RevokeDisabled gating the first step of the two-step revoke with its visible hint.
#endregion

#region Design
// Static render (HtmlRenderer) over a minimal TimeWarp.State container — the SPA assembly's states
// are registered so BaseComponent's Store/Subscriptions resolve, and ISender / IPublisher / IJSRuntime
// are FakeItEasy fakes because nothing is dispatched during a first render; BaseComponent's auth
// injections resolve via AddAuthorizationCore + a fake AuthenticationStateProvider (never consulted here). This pins the markup
// and the OnParametersSet auto-open rule; the click paths (Save / Cancel / two-step revoke) need an
// interactive renderer (no bUnit in this repo — see task 248-001 review disposition).
// CredentialRowPresenter text rules themselves are pinned in credential-row-presenter-tests.cs.
#endregion

namespace CredentialListRender_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net;
using System.Text.RegularExpressions;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

[TestTag("Unit")]
public class CredentialList_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialList_Should_>();

  public static async Task Render_Nickname_Title_Context_Created_And_Fingerprint()
  {
    CredentialSummary credential = Summary(nickname: "Work laptop", label: "1Password",
      registeredWith: new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"), fingerprint: "3f9a1c2e");

    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Credentials"] = new List<CredentialSummary> { credential },
      ["FallbackLabel"] = "Passkey",
      ["TitleDataQa"] = "PasskeyLabel"
    });

    CountOf(html, "CredentialRow").ShouldBe(1);
    TextOf(html, "PasskeyLabel").ShouldBe("Work laptop");
    TextOf(html, "CredentialContext").ShouldBe("1Password · Built-in · Chrome on Windows");
    TextOf(html, "CredentialCreated").ShouldBe("Created " + CredentialRowPresenter.CreatedText(credential));
    TextOf(html, "CredentialLastUsed").ShouldBe("Never used");
    TextOf(html, "CredentialFingerprint").ShouldBe("3f9a1c2e");
    CountOf(html, "RenameCredential").ShouldBe(1);
    CountOf(html, "RevokePasskey").ShouldBe(1);
    CountOf(html, "CredentialRenameEditor").ShouldBe(0);
    CountOf(html, "RevokeConfirm").ShouldBe(0);
  }

  public static async Task Auto_Open_Rename_Editor_Prefilled_For_Pending_Credential_Only()
  {
    CredentialSummary pending = Summary(nickname: null, label: "Proton Pass");
    CredentialSummary other = Summary(nickname: "Phone", label: "1Password");

    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Credentials"] = new List<CredentialSummary> { pending, other },
      ["FallbackLabel"] = "Passkey",
      ["TitleDataQa"] = "PasskeyLabel",
      ["PendingRenameCredentialId"] = pending.Id.Value,
      ["PendingRenameDefault"] = "Proton Pass"
    });

    // Exactly one editor, on the pending row, prefilled with the provider name.
    CountOf(html, "CredentialRenameEditor").ShouldBe(1);
    CountOf(html, "CredentialNicknameInput").ShouldBe(1);
    html.ShouldContain("value=\"Proton Pass\" data-qa=\"CredentialNicknameInput\"");
    CountOf(html, "CredentialRenameSave").ShouldBe(1);
    CountOf(html, "CredentialRenameCancel").ShouldBe(1);
    // The other row keeps its title; the pending row's title span is replaced by the editor.
    CountOf(html, "PasskeyLabel").ShouldBe(1);
    TextOf(html, "PasskeyLabel").ShouldBe("Phone");
  }

  public static async Task Pending_Id_Not_In_List_Does_Not_Open_An_Editor()
  {
    CredentialSummary credential = Summary(nickname: null, label: "Proton Pass");

    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Credentials"] = new List<CredentialSummary> { credential },
      ["FallbackLabel"] = "Passkey",
      ["TitleDataQa"] = "PasskeyLabel",
      ["PendingRenameCredentialId"] = Guid.NewGuid(),
      ["PendingRenameDefault"] = "Proton Pass"
    });

    CountOf(html, "CredentialRenameEditor").ShouldBe(0);
    TextOf(html, "PasskeyLabel").ShouldBe("Proton Pass");
  }

  public static async Task Revoke_Disabled_Disables_First_Step_And_Shows_Hint()
  {
    CredentialSummary credential = Summary(nickname: "Only one", label: "1Password");

    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Credentials"] = new List<CredentialSummary> { credential },
      ["FallbackLabel"] = "Passkey",
      ["RevokeDisabled"] = true,
      ["RevokeDisabledHint"] = "Add another passkey or agent key before revoking this one.",
      ["RevokeDisabledHintDataQa"] = "RevokePasskeyHint"
    });

    // Task 246's last-credential guard gates the FIRST step of the two-step revoke (248-001).
    TagOf(html, "RevokePasskey").ShouldContain("disabled");
    TextOf(html, "RevokePasskeyHint").ShouldBe("Add another passkey or agent key before revoking this one.");
    CountOf(html, "RevokeConfirm").ShouldBe(0);
  }

  public static async Task Empty_List_Renders_Empty_Message()
  {
    string html = await RenderAsync(new Dictionary<string, object?>
    {
      ["Credentials"] = new List<CredentialSummary>(),
      ["EmptyMessage"] = "No passkeys on this account yet."
    });

    html.ShouldContain("No passkeys on this account yet.");
    CountOf(html, "CredentialRow").ShouldBe(0);
  }

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    ServiceCollection services = new();
    services.AddLogging();
    services.AddFluentUIComponents();
    services.AddTimeWarpState(options => options.Assemblies = [typeof(CredentialList).Assembly]);
    services.AddAuthorizationCore();
    services.AddScoped(_ => A.Fake<AuthenticationStateProvider>());
    services.AddScoped(_ => A.Fake<IJSRuntime>());
    services.AddScoped(_ => A.Fake<ISender<ClientPipeline>>());
    services.AddScoped(_ => A.Fake<IPublisher<ClientPipeline>>());

    await using ServiceProvider provider = services.BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());

    string html = await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<CredentialList>(ParameterView.FromDictionary(parameters))).ToHtmlString());
    return WebUtility.HtmlDecode(html);
  }

  /// <summary>Number of elements carrying this data-qa marker (CSS-isolation scope attributes follow it, so match the attribute only).</summary>
  private static int CountOf(string html, string dataQa) =>
    Regex.Count(html, $"data-qa=\"{Regex.Escape(dataQa)}\"");

  /// <summary>The opening tag of the single element carrying this data-qa marker.</summary>
  private static string TagOf(string html, string dataQa)
  {
    Match match = Regex.Match(html, $"<[^<>]*data-qa=\"{Regex.Escape(dataQa)}\"[^<>]*>");
    match.Success.ShouldBeTrue($"no element with data-qa={dataQa}");
    return match.Value;
  }

  /// <summary>Trimmed inner text of the single element carrying this data-qa marker.</summary>
  private static string TextOf(string html, string dataQa)
  {
    Match match = Regex.Match(html, $"data-qa=\"{Regex.Escape(dataQa)}\"[^>]*>([^<]*)<");
    match.Success.ShouldBeTrue($"no element with data-qa={dataQa}");
    return match.Groups[1].Value.Trim();
  }

  private static CredentialSummary Summary
  (
    string? nickname,
    string? label,
    RegisteredWith? registeredWith = null,
    string fingerprint = "0123abcd"
  ) =>
    new
    (
      CredentialId.New(),
      CredentialType.Passkey,
      label,
      nickname,
      new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero),
      revokedAt: null,
      isActive: true,
      registeredWith ?? RegisteredWith.Unknown,
      fingerprint
    );
}
