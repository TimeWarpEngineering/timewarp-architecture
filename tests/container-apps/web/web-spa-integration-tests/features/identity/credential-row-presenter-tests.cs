#region Purpose
// Task 248-001/248-002/250: the CredentialList row text rules — title, context line, fingerprint,
// inline-rename prefill, last-used text, the revoke confirmation, and the no-repeat dedupe (250) — pinned host-free through
// CredentialRowPresenter.
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
    CredentialSummary full = Summary(nickname: "Work laptop", label: "1Password", registeredWith: new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"));
    CredentialRowPresenter.ContextLine(full, "Passkey").ShouldBe("1Password · Built-in · Chrome on Windows");

    CredentialSummary roamingNoOs = Summary(nickname: "Key", label: null, registeredWith: new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "Safari", null));
    CredentialRowPresenter.ContextLine(roamingNoOs, "Passkey").ShouldBe("Passkey · Roaming · Safari");

    CredentialSummary unknown = Summary(nickname: "Phone", label: "Proton Pass", registeredWith: RegisteredWith.Unknown);
    CredentialRowPresenter.ContextLine(unknown, "Passkey").ShouldBe("Proton Pass");
    return Task.CompletedTask;
  }

  public static Task Context_Line_Drops_Provider_That_Repeats_The_Title()
  {
    // Un-renamed passkey with Unknown context: title is the provider, so the line is empty (row hides it).
    CredentialSummary unrenamed = Summary(label: "Proton Pass", registeredWith: RegisteredWith.Unknown);
    CredentialRowPresenter.Title(unrenamed, "Passkey").ShouldBe("Proton Pass");
    CredentialRowPresenter.ContextLine(unrenamed, "Passkey").ShouldBe(string.Empty);

    // Case-insensitive: a nickname equal to the provider still does not repeat it.
    CredentialSummary sameCase = Summary(nickname: "proton pass", label: "Proton Pass",
      registeredWith: new RegisteredWith(AuthenticatorAttachment.Platform, null, null));
    CredentialRowPresenter.ContextLine(sameCase, "Passkey").ShouldBe("Built-in");

    // Un-renamed passkey WITH context keeps the context, drops only the repeated provider.
    CredentialSummary withContext = Summary(label: "1Password",
      registeredWith: new RegisteredWith(AuthenticatorAttachment.Platform, "Chrome", "Windows"));
    CredentialRowPresenter.ContextLine(withContext, "Passkey").ShouldBe("Built-in · Chrome on Windows");
    return Task.CompletedTask;
  }

  public static Task Entra_Row_Context_Line_Is_The_Account_Hint_Only()
  {
    CredentialSummary linked = Entra(accountHint: "steve@contoso.com");
    CredentialRowPresenter.Title(linked, "Microsoft 365").ShouldBe("Microsoft 365");
    CredentialRowPresenter.ContextLine(linked, "Microsoft 365").ShouldBe("steve@contoso.com");

    CredentialSummary noHint = Entra(accountHint: null);
    CredentialRowPresenter.ContextLine(noHint, "Microsoft 365").ShouldBe(string.Empty, "a link made before task 250 has no hint until the next sign-in");
    return Task.CompletedTask;
  }

  public static Task Revoke_Confirmation_Does_Not_Repeat_The_Title()
  {
    DateTimeOffset now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    string entra = CredentialRowPresenter.RevokeConfirmation(Entra(accountHint: "steve@contoso.com", fingerprint: "5c7d9e01"), "Microsoft 365", "Unlink", now);
    entra.ShouldStartWith("Unlink “Microsoft 365”? steve@contoso.com, created ");
    entra.ShouldNotContain("Microsoft 365, ");
    entra.ShouldEndWith(", never used, fingerprint 5c7d9e01. This cannot be undone.");

    string entraNoHint = CredentialRowPresenter.RevokeConfirmation(Entra(accountHint: null), "Microsoft 365", "Unlink", now);
    entraNoHint.ShouldStartWith("Unlink “Microsoft 365”? Created ");

    string unrenamed = CredentialRowPresenter.RevokeConfirmation(Summary(label: "Proton Pass"), "Passkey", "Delete", now);
    unrenamed.ShouldStartWith("Delete “Proton Pass”? Created ");
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

  public static Task Revoke_Confirmation_Restates_Title_Provider_Created_Last_Used_And_Fingerprint()
  {
    DateTimeOffset now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
    CredentialSummary credential = Summary(nickname: "Work laptop", label: "1Password", fingerprint: "3f9a1c2e", lastUsedAt: now.AddHours(-2));

    string text = CredentialRowPresenter.RevokeConfirmation(credential, "Passkey", "Delete", now);

    text.ShouldStartWith("Delete “Work laptop”? 1Password, created ");
    text.ShouldContain(CredentialRowPresenter.CreatedText(credential));
    text.ShouldContain(", last used 2 hours ago, fingerprint 3f9a1c2e.");
    text.ShouldEndWith("fingerprint 3f9a1c2e. This cannot be undone.");

    string neverUsed = CredentialRowPresenter.RevokeConfirmation(Summary(fingerprint: "3f9a1c2e"), "Passkey", "Revoke", now);
    neverUsed.ShouldContain(", never used, fingerprint 3f9a1c2e.");
    return Task.CompletedTask;
  }

  public static Task Last_Used_Text_Is_Never_Used_Or_Relative()
  {
    DateTimeOffset now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    CredentialRowPresenter.LastUsedText(Summary(), now).ShouldBe("Never used");
    CredentialRowPresenter.LastUsedText(Summary(lastUsedAt: now.AddSeconds(-20)), now).ShouldBe("Last used just now");
    CredentialRowPresenter.LastUsedText(Summary(lastUsedAt: now.AddMinutes(-3)), now).ShouldBe("Last used 3 minutes ago");
    CredentialRowPresenter.LastUsedText(Summary(lastUsedAt: now.AddHours(-5)), now).ShouldBe("Last used 5 hours ago");
    CredentialRowPresenter.LastUsedText(Summary(lastUsedAt: now.AddDays(-1)), now).ShouldBe("Last used yesterday");
    CredentialRowPresenter.LastUsedText(Summary(lastUsedAt: now.AddDays(-12)), now).ShouldBe("Last used 12 days ago");
    return Task.CompletedTask;
  }

  public static Task Relative_Time_Buckets_Singular_Future_And_Absolute_Fallback()
  {
    DateTimeOffset now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    CredentialRowPresenter.RelativeTime(now.AddMinutes(-1), now).ShouldBe("1 minute ago");
    CredentialRowPresenter.RelativeTime(now.AddHours(-1), now).ShouldBe("1 hour ago");
    CredentialRowPresenter.RelativeTime(now.AddMinutes(5), now).ShouldBe("just now", "clock skew never reads as negative age");
    CredentialRowPresenter.RelativeTime(now.AddDays(-45), now).ShouldStartWith("on ");
    CredentialRowPresenter.RelativeTime(now.AddDays(-45), now)
      .ShouldBe("on " + now.AddDays(-45).ToLocalTime().ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture));
    return Task.CompletedTask;
  }

  public static Task Fingerprint_Is_Shown_Verbatim_And_Never_The_Handle()
  {
    CredentialSummary credential = Summary(fingerprint: "b71e04dd");
    credential.Fingerprint.ShouldBe("b71e04dd");
    typeof(CredentialSummary).GetProperties().Select(p => p.Name).ShouldNotContain(nameof(Credential.Handle));
    return Task.CompletedTask;
  }

  private static CredentialSummary Entra(string? accountHint, string fingerprint = "0123abcd") =>
    new
    (
      CredentialId.New(),
      CredentialType.EntraAccount,
      "Microsoft 365",
      nickname: null,
      new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero),
      revokedAt: null,
      isActive: true,
      RegisteredWith.Unknown,
      fingerprint,
      accountHint: accountHint
    );

  private static CredentialSummary Summary
  (
    string? nickname = null,
    string? label = "1Password",
    RegisteredWith? registeredWith = null,
    string fingerprint = "0123abcd",
    DateTimeOffset? lastUsedAt = null
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
      fingerprint,
      lastUsedAt
    );
}
