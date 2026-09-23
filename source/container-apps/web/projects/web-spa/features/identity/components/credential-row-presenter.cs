#region Purpose
// Pure, host-free text rules for a credential row: title, context line, created text, last-used text,
// and the revoke-confirmation sentence — so CredentialList markup stays declarative and the rules are testable.
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
// Last used (task 248-002): LastUsedText is "Never used" when the server sent no stamp, else
// "Last used <relative>" — relative because the question it answers is "is this the passkey I
// actually use", where "3 minutes ago" beats an absolute stamp; beyond ~30 days the absolute date is
// more honest than "43 days ago". `now` is a parameter (the component passes UtcNow) so the buckets
// are pinned in tests; a stamp in the future (clock skew) reads as "just now". The same text is
// restated in RevokeConfirmation between created and fingerprint, so the user confirms the row's
// usage before revoking.
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

  public const string NeverUsedText = "Never used";

  /// <summary>"Never used" or "Last used &lt;relative&gt;" (see <see cref="RelativeTime"/>).</summary>
  public static string LastUsedText(CredentialSummary credential, DateTimeOffset now) =>
    credential.LastUsedAt is DateTimeOffset lastUsedAt
      ? $"Last used {RelativeTime(lastUsedAt, now)}"
      : NeverUsedText;

  /// <summary>Mid-sentence form of <see cref="LastUsedText"/>: "never used" / "last used &lt;relative&gt;".</summary>
  private static string LastUsedClause(CredentialSummary credential, DateTimeOffset now) =>
    credential.LastUsedAt is DateTimeOffset lastUsedAt
      ? $"last used {RelativeTime(lastUsedAt, now)}"
      : "never used";

  /// <summary>
  /// just now · N minute(s) ago · N hour(s) ago · yesterday · N days ago (under 30) · on M/d/yyyy.
  /// A future instant reads as "just now".
  /// </summary>
  public static string RelativeTime(DateTimeOffset at, DateTimeOffset now)
  {
    TimeSpan age = now - at;
    if (age < TimeSpan.FromMinutes(1))
    {
      return "just now";
    }

    if (age < TimeSpan.FromHours(1))
    {
      int minutes = (int)age.TotalMinutes;
      return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
    }

    if (age < TimeSpan.FromDays(1))
    {
      int hours = (int)age.TotalHours;
      return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
    }

    if (age < TimeSpan.FromDays(30))
    {
      int days = (int)age.TotalDays;
      return days == 1 ? "yesterday" : $"{days} days ago";
    }

    return "on " + at.ToLocalTime().ToString("M/d/yyyy", CultureInfo.InvariantCulture);
  }

  public static string NicknameDefault(CredentialSummary credential, string fallbackLabel) =>
    !string.IsNullOrWhiteSpace(credential.Nickname) ? credential.Nickname! : Provider(credential, fallbackLabel);

  /// <summary>Confirmation sentence restating title, provider, created, last used, and fingerprint.</summary>
  public static string RevokeConfirmation(CredentialSummary credential, string fallbackLabel, string actionLabel, DateTimeOffset? now = null) =>
    $"{actionLabel} \u201c{Title(credential, fallbackLabel)}\u201d? "
    + $"{Provider(credential, fallbackLabel)}, created {CreatedText(credential)}, "
    + $"{LastUsedClause(credential, now ?? DateTimeOffset.UtcNow)}, fingerprint {credential.Fingerprint}. "
    + "This cannot be undone.";
}

/// <summary>Inline-rename result raised by CredentialList.</summary>
public sealed record CredentialRename(Guid CredentialId, string Nickname);
