#region Purpose
// Maps the feedback domain onto its wire contracts for the filer's and the admin's views.
#endregion

#region Design
// Enums map by name, never by numeric cast, so a reordered contract enum cannot silently
// change meaning; a stored name with no contract member is a programming error and throws.
// FilerThread is the one place that decides what the filer sees: it keeps the IsVisibleToFiler
// allow-list (status changes, admin replies, filer replies) and drops internal notes and
// severity changes, and it nulls author ids. AdminThread keeps every entry and author.
// AdminViewAsync loads attachments, severity, and the filer's email presence (not the address)
// for FeedbackAdminView, and the filer's display name through FeedbackOwnerNames (null without
// admin.profiles.read). Enums, including severity, map by name.
// Project is the one attachment projection (task 479): AttachmentsAsync and the upload handler
// wrap it into their own contract types with named arguments. Every constructor call here
// names its arguments so same-typed members cannot be swapped silently.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using ContractActivityKind = TimeWarp.Architecture.Features.Feedback.FeedbackActivityKind;
using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
using ContractSeverity = TimeWarp.Architecture.Features.Feedback.FeedbackSeverity;
using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
using DomainSeverity = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackSeverity;
using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;
using DownloadFeedbackAttachmentContract = TimeWarp.Architecture.Features.Feedback.DownloadFeedbackAttachment;
using GetFeedbackContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;

internal static class FeedbackContractMap
{
  /// <summary>The attachment members GetFeedback.Attachment and UploadFeedbackAttachment.Response share.</summary>
  public readonly record struct AttachmentProjection
  (
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long Size,
    string DownloadPath
  );

  public static ContractKind ToContract(FeedbackKind kind) => ByName<FeedbackKind, ContractKind>(kind);

  public static ContractStatus ToContract(DomainStatus status) => ByName<DomainStatus, ContractStatus>(status);

  public static ContractSeverity ToContract(DomainSeverity severity) =>
    ByName<DomainSeverity, ContractSeverity>(severity);

  public static ContractActivityKind ToContract(FeedbackActivityKind kind) =>
    ByName<FeedbackActivityKind, ContractActivityKind>(kind);

  /// <summary>Maps a wire status onto the domain, or null when the name has no domain member.</summary>
  public static DomainStatus? ToDomain(ContractStatus status) =>
    Enum.TryParse(status.ToString(), out DomainStatus mapped) && Enum.IsDefined(mapped) ? mapped : null;

  /// <summary>Maps a wire severity onto the domain, or null when the name has no domain member.</summary>
  public static DomainSeverity? ToDomain(ContractSeverity severity) =>
    Enum.TryParse(severity.ToString(), out DomainSeverity mapped) && Enum.IsDefined(mapped) ? mapped : null;

  /// <summary>Maps a wire kind onto the domain, or null when the name has no domain member.</summary>
  public static FeedbackKind? ToDomain(ContractKind kind) =>
    Enum.TryParse(kind.ToString(), out FeedbackKind mapped) && Enum.IsDefined(mapped) ? mapped : null;

  /// <summary>The thread as the filer sees it: no internal notes, no severity changes, no admin identities.</summary>
  public static List<FeedbackActivityEntry> FilerThread(FeedbackItem item) =>
    [.. item.Activity.Where(entry => entry.IsVisibleToFiler).Select(entry => ToEntry(entry, includeAuthor: false))];

  /// <summary>Every entry with its author, for administrators.</summary>
  public static List<FeedbackActivityEntry> AdminThread(FeedbackItem item) =>
    [.. item.Activity.Select(entry => ToEntry(entry, includeAuthor: true))];

  public static async Task<List<GetFeedbackContract.Attachment>> AttachmentsAsync(
    IFeedbackAttachmentStore attachmentStore,
    FeedbackItemId itemId,
    CancellationToken cancellationToken)
  {
    IReadOnlyList<FeedbackAttachment> stored = await attachmentStore
      .ListByItemAsync(itemId, cancellationToken)
      .ConfigureAwait(false);
    return
    [
      .. stored.Select(Project).Select(projection => new GetFeedbackContract.Attachment(
        attachmentId: projection.AttachmentId,
        fileName: projection.FileName,
        contentType: projection.ContentType,
        size: projection.Size,
        downloadPath: projection.DownloadPath))
    ];
  }

  /// <summary>Reads the members every attachment contract shares from a stored attachment.</summary>
  public static AttachmentProjection Project(FeedbackAttachment attachment) =>
    new
    (
      AttachmentId: attachment.Id.Value,
      FileName: attachment.FileName,
      ContentType: attachment.ContentType,
      Size: attachment.Size,
      DownloadPath: DownloadFeedbackAttachmentContract.DownloadPath(attachment.Id.Value)
    );

  public static async Task<FeedbackAdminView> AdminViewAsync(
    FeedbackItem item,
    IFeedbackAttachmentStore attachmentStore,
    IProfileEmailLookup profileEmailLookup,
    FeedbackOwnerNames ownerNames,
    CancellationToken cancellationToken)
  {
    List<GetFeedbackContract.Attachment> attachments = await AttachmentsAsync(attachmentStore, item.Id, cancellationToken)
      .ConfigureAwait(false);
    string? email = await profileEmailLookup
      .FindEmailAsync(item.OwnerPrincipalId, cancellationToken)
      .ConfigureAwait(false);
    string? ownerDisplayName = await ownerNames
      .FindAsync(item.OwnerPrincipalId, cancellationToken)
      .ConfigureAwait(false);
    return new FeedbackAdminView(
      feedbackItemId: item.Id.Value,
      ownerPrincipalId: item.OwnerPrincipalId,
      kind: ToContract(item.Kind),
      status: ToContract(item.Status),
      title: item.Title,
      body: item.Body,
      filedAt: item.FiledAt,
      filerHasEmail: !string.IsNullOrWhiteSpace(email),
      attachments: attachments,
      activity: AdminThread(item),
      ownerDisplayName: ownerDisplayName,
      severity: ToContract(item.Severity));
  }

  private static FeedbackActivityEntry ToEntry(FeedbackActivity entry, bool includeAuthor) =>
    new(
      sequence: entry.Sequence,
      kind: ToContract(entry.Kind),
      authorPrincipalId: includeAuthor ? entry.AuthorPrincipalId : null,
      body: entry.Body,
      fromStatus: entry.FromStatus is { } from ? ToContract(from) : null,
      toStatus: entry.ToStatus is { } to ? ToContract(to) : null,
      at: entry.At,
      fromSeverity: entry.FromSeverity is { } fromSeverity ? ToContract(fromSeverity) : null,
      toSeverity: entry.ToSeverity is { } toSeverity ? ToContract(toSeverity) : null);

  private static TTarget ByName<TSource, TTarget>(TSource value)
    where TSource : struct, Enum
    where TTarget : struct, Enum
  {
    if (!Enum.TryParse(value.ToString(), out TTarget mapped) || !Enum.IsDefined(mapped))
    {
      throw new InvalidOperationException($"Stored {typeof(TSource).Name} '{value}' has no contract member.");
    }

    return mapped;
  }
}
