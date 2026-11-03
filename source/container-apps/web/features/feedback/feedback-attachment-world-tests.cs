#region Purpose
// Shared fakes for the feedback attachment runfiles: in-memory world, racing store, fake blob store, and stub lookups.
#endregion

namespace TimeWarp.Architecture.Features.Feedback
{
  using Microsoft.Extensions.Logging.Abstractions;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using DownloadHandler = TimeWarp.Architecture.Features.Feedback.Application.DownloadFeedbackAttachment.Handler;
  using GetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedback.Handler;
  using RemoveHandler = TimeWarp.Architecture.Features.Feedback.Application.RemoveFeedbackAttachment.Handler;
  using SubmitHandler = TimeWarp.Architecture.Features.Feedback.Application.SubmitFeedback.Handler;
  using UploadHandler = TimeWarp.Architecture.Features.Feedback.Application.UploadFeedbackAttachment.Handler;

  internal sealed class AttachmentWorld
  {
    internal static readonly byte[] Png = Convert.FromBase64String(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    public InMemoryFeedbackStore Feedback { get; } = new();
    public InMemoryFeedbackAttachmentStore Attachments { get; } = new();
    public InMemoryFeedbackAttachmentBlobStore Blobs { get; } = new();
    public AttachmentGrantingPermissionEvaluator Permissions { get; } = new();
    public AttachmentHeldAgentCallers Agents { get; } = new();
    public PrincipalId Owner { get; } = PrincipalId.New();
    public IFeedbackAttachmentBlobStore? UploadBlobs { get; set; }

    public async Task<Guid> UploadAsync(PrincipalId principal, string fileName, string contentType, byte[] content)
    {
      UploadFeedbackAttachment.Response response = (await UploadAs(principal).Handle(
          new UploadFeedbackAttachment.Command
          {
            FileName = fileName,
            ContentType = contentType,
            Content = new MemoryStream(content),
          },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected upload success"));
      return response.AttachmentId;
    }

    public async Task<SharedProblemDetails> UploadProblemAsync(
      PrincipalId principal,
      string fileName,
      string contentType,
      byte[] content) =>
      (await UploadAs(principal).Handle(
          new UploadFeedbackAttachment.Command
          {
            FileName = fileName,
            ContentType = contentType,
            Content = new MemoryStream(content),
          },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected upload problem"), problem => problem);

    public async Task<DownloadFeedbackAttachment.Response> DownloadAsync(PrincipalId principal, Guid attachmentId) =>
      (await DownloadAs(principal).Handle(
          new DownloadFeedbackAttachment.Query { AttachmentId = attachmentId },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected download success"));

    public async Task<SharedProblemDetails> DownloadProblemAsync(PrincipalId principal, Guid attachmentId) =>
      (await DownloadAs(principal).Handle(
          new DownloadFeedbackAttachment.Query { AttachmentId = attachmentId },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected download problem"), problem => problem);

    public async Task<DownloadFeedbackAttachment.Response> DownloadWithAsync(
      PrincipalId principal,
      Guid attachmentId,
      IPermissionEvaluator evaluator,
      IAgentPermissionScopeSource scopes) =>
      (await DownloadAs(principal, evaluator, scopes).Handle(
          new DownloadFeedbackAttachment.Query { AttachmentId = attachmentId },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected download success"));

    public async Task<SharedProblemDetails> DownloadProblemWithAsync(
      PrincipalId principal,
      Guid attachmentId,
      IPermissionEvaluator evaluator,
      IAgentPermissionScopeSource scopes) =>
      (await DownloadAs(principal, evaluator, scopes).Handle(
          new DownloadFeedbackAttachment.Query { AttachmentId = attachmentId },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected download problem"), problem => problem);

    public async Task<SharedProblemDetails> RemoveProblemAsync(PrincipalId principal, Guid attachmentId) =>
      (await RemoveAs(principal).Handle(
          new RemoveFeedbackAttachment.Command { AttachmentId = attachmentId },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected remove problem"), problem => problem);

    public async Task<SubmitFeedback.Response> SubmitAsync(PrincipalId principal, IReadOnlyList<Guid> attachmentIds) =>
      (await SubmitAs(principal).Handle(Command(attachmentIds), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected submit success"));

    public async Task SubmitThrowsAsync(PrincipalId principal, IReadOnlyList<Guid> attachmentIds, IFeedbackAttachmentStore attachmentStore) =>
      _ = await SubmitAs(principal, attachmentStore).Handle(Command(attachmentIds), CancellationToken.None);

    public async Task<SharedProblemDetails> SubmitProblemAsync(
      PrincipalId principal,
      IReadOnlyList<Guid> attachmentIds,
      IFeedbackAttachmentStore? attachmentStore = null) =>
      (await SubmitAs(principal, attachmentStore).Handle(Command(attachmentIds), CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected submit problem"), problem => problem);

    public async Task<GetFeedback.Response> GetAsync(PrincipalId principal, Guid feedbackItemId) =>
      (await GetAs(principal).Handle(new GetFeedback.Query { FeedbackItemId = feedbackItemId }, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected get success"));

    private UploadHandler UploadAs(PrincipalId principal) =>
      new(new AttachmentStubCurrentPrincipalAccessor(principal), Attachments, UploadBlobs ?? Blobs, NullLogger<UploadHandler>.Instance);

    private DownloadHandler DownloadAs(PrincipalId principal) =>
      DownloadAs(principal, Permissions, Agents);

    private DownloadHandler DownloadAs(
      PrincipalId principal,
      IPermissionEvaluator evaluator,
      IAgentPermissionScopeSource scopes) =>
      new(new AttachmentStubCurrentPrincipalAccessor(principal), Attachments, Blobs, evaluator, scopes);

    private RemoveHandler RemoveAs(PrincipalId principal) =>
      new(new AttachmentStubCurrentPrincipalAccessor(principal), Attachments, Blobs);

    private SubmitHandler SubmitAs(PrincipalId principal, IFeedbackAttachmentStore? attachmentStore = null) =>
      new(
        new AttachmentStubCurrentPrincipalAccessor(principal),
        Feedback,
        new AttachmentNoEmail(),
        new AttachmentNoMail(),
        new AttachmentNoBaseUrl(),
        NullLogger<SubmitHandler>.Instance,
        attachmentStore ?? Attachments);

    private GetHandler GetAs(PrincipalId principal) =>
      new(new AttachmentStubCurrentPrincipalAccessor(principal), Feedback, Attachments);

    private static SubmitFeedback.Command Command(IReadOnlyList<Guid> attachmentIds) => new()
    {
      Kind = ContractKind.Complaint,
      Title = "With files",
      Body = "Details",
      AttachmentIds = attachmentIds.ToList(),
    };
  }

  /// <summary>Links one attachment to another item just before submit tries to link it.</summary>
  internal sealed class RacingAttachmentStore : IFeedbackAttachmentStore
  {
    private readonly IFeedbackAttachmentStore Inner;
    private readonly FeedbackAttachmentId Contested;
    private readonly Guid Owner;
    private readonly FeedbackItemId Elsewhere;

    /// <summary>Commit the contested link, then throw, instead of losing it to another item.</summary>
    public bool CommitThenThrow { get; init; }

    /// <summary>Throw from UnlinkAll, as a failing rollback step.</summary>
    public bool ThrowOnUnlink { get; init; }

    public RacingAttachmentStore(
      IFeedbackAttachmentStore inner,
      FeedbackAttachmentId contested,
      Guid owner,
      FeedbackItemId elsewhere)
    {
      Inner = inner;
      Contested = contested;
      Owner = owner;
      Elsewhere = elsewhere;
    }

    public Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default) =>
      Inner.AddAsync(attachment, cancellationToken);

    public Task<FeedbackAttachment?> FindAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default) =>
      Inner.FindAsync(id, cancellationToken);

    public Task<IReadOnlyList<FeedbackAttachment>> ListByItemAsync(
      FeedbackItemId itemId,
      CancellationToken cancellationToken = default) =>
      Inner.ListByItemAsync(itemId, cancellationToken);

    public Task<int> CountUnlinkedByOwnerAsync(Guid ownerPrincipalId, CancellationToken cancellationToken = default) =>
      Inner.CountUnlinkedByOwnerAsync(ownerPrincipalId, cancellationToken);

    public async Task<bool> TryLinkAsync(
      FeedbackAttachmentId id,
      Guid ownerPrincipalId,
      FeedbackItemId itemId,
      CancellationToken cancellationToken = default)
    {
      if (id == Contested && CommitThenThrow)
      {
        (await Inner.TryLinkAsync(id, ownerPrincipalId, itemId, cancellationToken)).ShouldBeTrue();
        throw new InvalidOperationException("link committed then threw");
      }

      if (id == Contested)
      {
        (await Inner.TryLinkAsync(Contested, Owner, Elsewhere, cancellationToken)).ShouldBeTrue();
      }

      return await Inner.TryLinkAsync(id, ownerPrincipalId, itemId, cancellationToken);
    }

    public Task UnlinkAllAsync(FeedbackItemId itemId, CancellationToken cancellationToken = default) =>
      ThrowOnUnlink
        ? throw new InvalidOperationException("unlink failed")
        : Inner.UnlinkAllAsync(itemId, cancellationToken);

    public Task<IReadOnlyList<FeedbackAttachment>> RemoveExpiredUnlinkedAsync(
      Guid ownerPrincipalId,
      DateTimeOffset uploadedBefore,
      CancellationToken cancellationToken = default) =>
      Inner.RemoveExpiredUnlinkedAsync(ownerPrincipalId, uploadedBefore, cancellationToken);

    public Task RemoveAsync(FeedbackAttachmentId id, CancellationToken cancellationToken = default) =>
      Inner.RemoveAsync(id, cancellationToken);
  }

  /// <summary>Blob store whose deletes always fail.</summary>
  internal sealed class UndeletableBlobStore : IFeedbackAttachmentBlobStore
  {
    private readonly IFeedbackAttachmentBlobStore Inner;

    public UndeletableBlobStore(IFeedbackAttachmentBlobStore inner) => Inner = inner;

    public Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default) =>
      Inner.PutAsync(key, content, contentType, cancellationToken);

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
      Inner.OpenReadAsync(key, cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
      throw new IOException("blob delete failed");
  }

  internal sealed class AttachmentSizedStream : Stream
  {
    private long Remaining;

    public AttachmentSizedStream(long length) => Remaining = length;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (Remaining == 0)
      {
        return 0;
      }

      int take = (int)Math.Min(count, Remaining);
      Remaining -= take;
      return take;
    }
  }

  internal sealed class AttachmentHeldAgentCallers : IAgentPermissionScopeSource
  {
    private readonly HashSet<Guid> Agents = [];

    public void Mark(PrincipalId principal) => Agents.Add(principal.Value);

    public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) =>
      Agents.Contains(principalId.Value) ? [] : null;
  }

  internal sealed class AttachmentHeldScopes : IAgentPermissionScopeSource
  {
    private readonly Dictionary<Guid, IReadOnlyList<string>> Scopes = [];

    public void Hold(PrincipalId principal, params string[] scopes) =>
      Scopes[principal.Value] = scopes;

    public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) =>
      Scopes.TryGetValue(principalId.Value, out IReadOnlyList<string>? scopes) ? scopes : null;
  }

  internal sealed class AttachmentGrantingPermissionEvaluator : IPermissionEvaluator
  {
    private readonly HashSet<(Guid Principal, string Scheme, string Permission)> Grants = [];

    public void Grant(PrincipalId principal, string scheme, string permission) =>
      Grants.Add((principal.Value, scheme, permission));

    public Task<bool> HasPermissionAsync(
      PrincipalId principalId,
      string? authenticationScheme,
      string permissionId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(
        authenticationScheme is not null
        && Grants.Contains((principalId.Value, authenticationScheme, permissionId)));

    public Task<IReadOnlyList<string>> GetPermissionsAsync(
      PrincipalId principalId,
      string? authenticationScheme,
      CancellationToken cancellationToken = default) =>
      Task.FromResult<IReadOnlyList<string>>([]);
  }

  internal sealed class AttachmentStubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
  {
    private readonly PrincipalId PrincipalId;

    public AttachmentStubCurrentPrincipalAccessor(PrincipalId principalId) => PrincipalId = principalId;

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult<PrincipalId?>(PrincipalId);
  }

  internal sealed class AttachmentNoEmail : IProfileEmailLookup
  {
    public Task<string?> FindEmailAsync(Guid principalId, CancellationToken cancellationToken = default) =>
      Task.FromResult<string?>(null);
  }

  internal sealed class AttachmentNoMail : IEmailSender
  {
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
      Task.CompletedTask;
  }

  internal sealed class AttachmentNoBaseUrl : IAppBaseUrlAccessor
  {
    public Uri? GetBaseUrl() => null;
  }
}
