#region Purpose
// Pure, host-free text rules for a credential row: title, context line, created text, and the
// revoke-confirmation sentence — so CredentialList markup stays declarative and the rules are testable.
#endregion

#region Design
// Task 248-001. Title = Nickname, else provider Label, else the page's fallback ("Passkey" /
// "Microsoft 365"). Context line = provider · attachment word · "Browser on OS" (each part
// omitted when unknown — never "Unknown on Unknown"). Fingerprint is shown verbatim (8 hex, from
// the server; the SPA never sees the handle). RevokeConfirmation restates nickname/title,
// provider, created, and fingerprint so the user confirms the SAME row they see, per the parent
// task's decision. NicknameDefault is the prefill for the inline editor: the current nickname when
// renaming, else the provider name. Static + culture-explicit dates so SPA unit tests can pin the
// strings without a render host.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Globalization;
using TimeWarp.Identity;
using static GetCredentials;

public static class CredentialRowPresenter
{
  public const string PlatformAttachmentWord = "Built-in";
  public const string CrossPlatformAttachmentWord = "Roaming";

  public static string Title(CredentialSummary credential, string fallbackLabel) =>
    !string.IsNullOrWhiteSpace(credential.Nickname) ? credential.Nickname!
    : Provider(credential, fallbackLabel);

  public static string Provider(CredentialSummary credential, string fallbackLabel) =>
    string.IsNullOrWhiteSpace(credential.Label) ? fallbackLabel : credential.Label!;

  public static string? AttachmentWord(RegisteredWith registeredWith) =>
    registeredWith.Attachment switch
    {
      AuthenticatorAttachment.Platform => PlatformAttachmentWord,
      AuthenticatorAttachment.CrossPlatform => CrossPlatformAttachmentWord,
      _ => null
    };

  public static string? ClientText(RegisteredWith registeredWith) =>
    (registeredWith.Browser, registeredWith.Os) switch
    {
      (null, null) => null,
      (var browser, null) => browser,
      (null, var os) => os,
      (var browser, var os) => $"{browser} on {os}"
    };

  /// <summary>Provider · attachment · client, skipping unknown parts.</summary>
  public static string ContextLine(CredentialSummary credential, string fallbackLabel)
  {
    string?[] parts =
    [
      Provider(credential, fallbackLabel),
      AttachmentWord(credential.RegisteredWith),
      ClientText(credential.RegisteredWith)
    ];
    return string.Join(" \u00b7 ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
  }

  public static string CreatedText(CredentialSummary credential) =>
    credential.CreatedAt.ToLocalTime().ToString("M/d/yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);

  public static string NicknameDefault(CredentialSummary credential, string fallbackLabel) =>
    !string.IsNullOrWhiteSpace(credential.Nickname) ? credential.Nickname! : Provider(credential, fallbackLabel);

  /// <summary>Confirmation sentence restating title, provider, created, and fingerprint.</summary>
  public static string RevokeConfirmation(CredentialSummary credential, string fallbackLabel, string actionLabel) =>
    $"{actionLabel} \u201c{Title(credential, fallbackLabel)}\u201d? "
    + $"{Provider(credential, fallbackLabel)}, created {CreatedText(credential)}, fingerprint {credential.Fingerprint}. "
    + "This cannot be undone.";
}

/// <summary>Inline-rename result raised by CredentialList.</summary>
public sealed record CredentialRename(Guid CredentialId, string Nickname);
