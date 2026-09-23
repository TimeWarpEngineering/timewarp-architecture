#region Purpose
// Task 248-001: the CredentialList row text rules — title, context line, fingerprint, inline-rename
// prefill, and the revoke confirmation — pinned host-free through CredentialRowPresenter.
#endregion

#region Design
// Pure-function tests (no SPA host), same style as passkey-soft-prompt-tests. The presenter is the
// single owner of the row's strings so the Razor markup stays declarative; these facts are what
// "row content" means for this task. Dates use an explicit UTC offset so the created text is
// deterministic wherever the suite runs (the presenter formats local time; we assert the parts that
// do not depend on the zone).
#endregion

namespace CredentialRowPresenter_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

[TestTag("Unit")]
public class Row_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Row_Should_>();

  public static Task Title_Prefers_Nickname_Then_Provider_Then_Fallback()
  {
    CredentialRowPresenter.Title(Summary(nickname: "Work laptop", label: "1Password"), "Passkey").ShouldBe("Work laptop");
    CredentialRowPresenter.Title(Summary(nickname: null, label: "1Password"), "Passkey").ShouldBe("1Password");
    CredentialRowPresenter.Title(Summary(nickname: "  ", label: null), "Passkey").ShouldBe("Passkey");
    return Task.CompletedTask;
  }

  public static Task Context_Line_Is_Provider_Attachment_And_Client_Skipping_Unknowns()
  {
    CredentialSummary full = Summary(label: "1Password", registeredWith: new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"));
    CredentialRowPresenter.ContextLine(full, "Passkey").ShouldBe("1Password · Built-in · Chrome on Windows");

    CredentialSummary roamingNoOs = Summary(label: null, registeredWith: new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "Safari", null));
    CredentialRowPresenter.ContextLine(roamingNoOs, "Passkey").ShouldBe("Passkey · Roaming · Safari");

    CredentialSummary unknown = Summary(label: "Proton Pass", registeredWith: RegisteredWith.Unknown);
    CredentialRowPresenter.ContextLine(unknown, "Passkey").ShouldBe("Proton Pass");
    return Task.CompletedTask;
  }

  public static Task Client_Text_Handles_Every_Combination()
  {
    CredentialRowPresenter.ClientText(new RegisteredWith(AuthenticatorAttachment.Unknown, null, null)).ShouldBeNull();
    CredentialRowPresenter.ClientText(new RegisteredWith(AuthenticatorAttachment.Unknown, null, "Linux")).ShouldBe("Linux");
    CredentialRowPresenter.ClientText(new RegisteredWith(AuthenticatorAttachment.Unknown, "Firefox", null)).ShouldBe("Firefox");
    CredentialRowPresenter.ClientText(new RegisteredWith(AuthenticatorAttachment.Unknown, "Firefox", "Linux")).ShouldBe("Firefox on Linux");
    return Task.CompletedTask;
  }

  public static Task Rename_Prefill_Is_Current_Nickname_Else_Provider()
  {
    CredentialRowPresenter.NicknameDefault(Summary(nickname: "Phone", label: "1Password"), "Passkey").ShouldBe("Phone");
    CredentialRowPresenter.NicknameDefault(Summary(nickname: null, label: "1Password"), "Passkey").ShouldBe("1Password");
    CredentialRowPresenter.NicknameDefault(Summary(nickname: null, label: null), "Microsoft 365").ShouldBe("Microsoft 365");
    return Task.CompletedTask;
  }

  public static Task Revoke_Confirmation_Restates_Title_Provider_Created_And_Fingerprint()
  {
    CredentialSummary credential = Summary(nickname: "Work laptop", label: "1Password", fingerprint: "3f9a1c2e");

    string text = CredentialRowPresenter.RevokeConfirmation(credential, "Passkey", "Delete");

    text.ShouldStartWith("Delete “Work laptop”? 1Password, created ");
    text.ShouldContain(CredentialRowPresenter.CreatedText(credential));
    text.ShouldEndWith("fingerprint 3f9a1c2e. This cannot be undone.");
    return Task.CompletedTask;
  }

  public static Task Fingerprint_Is_Shown_Verbatim_And_Never_The_Handle()
  {
    CredentialSummary credential = Summary(fingerprint: "b71e04dd");
    credential.Fingerprint.ShouldBe("b71e04dd");
    typeof(CredentialSummary).GetProperties().Select(p => p.Name).ShouldNotContain(nameof(Credential.Handle));
    return Task.CompletedTask;
  }

  private static CredentialSummary Summary
  (
    string? nickname = null,
    string? label = "1Password",
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
