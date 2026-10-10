#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: attachment limits, owner-or-admin download, and submit linking.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-attachment-tests.cs

#region Purpose
// Jaribu runfile for attachment size and type limits, the pending cap and its expiry, the
// owner-or-admin download rule, safe download headers and X-File-Name decoding, and filing
// that links only the caller's pending uploads, lists unavailable ids, and rolls back when a
// link loses a race, commits then throws, or the rollback's unlink step fails. Expiry survives
// a blob delete that throws.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{
  using Microsoft.Extensions.Logging.Abstractions;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using DownloadHandler = TimeWarp.Architecture.Features.Feedback.Application.DownloadFeedbackAttachment.Handler;
  using GetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedback.Handler;
  using RemoveHandler = TimeWarp.Architecture.Features.Feedback.Application.RemoveFeedbackAttachment.Handler;
  using SubmitHandler = TimeWarp.Architecture.Features.Feedback.Application.SubmitFeedback.Handler;
  using UploadHandler = TimeWarp.Architecture.Features.Feedback.Application.UploadFeedbackAttachment.Handler;

  [TestTag("Handler")]
  public class FeedbackAttachment_Given_
  {
    private static readonly byte[] Png = Convert.FromBase64String(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackAttachment_Given_>();

    public static Task Limits_Should_MatchBetweenContractAndDomain()
    {
      FeedbackAttachment.MaxBytes.ShouldBe(FeedbackAttachmentRules.MaxBytes);
      FeedbackAttachment.MaxFileNameLength.ShouldBe(FeedbackAttachmentRules.MaxFileNameLength);
      FeedbackAttachment.MaxPerItem.ShouldBe(FeedbackAttachmentRules.MaxPerItem);
      FeedbackAttachment.AllowedContentTypes.ShouldBe(FeedbackAttachmentRules.AllowedContentTypes);
      return Task.CompletedTask;
    }

    public static async Task Read_Should_RejectOversizeEmptyAndMismatchedBytes()
    {
      OneOf<byte[], SharedProblemDetails> oversize = await FeedbackAttachmentContent.ReadAsync(
        new SizedStream(FeedbackAttachmentRules.MaxBytes + 1),
        FeedbackAttachmentRules.Png);
      Problem(oversize).Status.ShouldBe(413);

      OneOf<byte[], SharedProblemDetails> empty = await FeedbackAttachmentContent.ReadAsync(
        Stream.Null,
        FeedbackAttachmentRules.Text);
      Problem(empty).Status.ShouldBe(400);

      OneOf<byte[], SharedProblemDetails> svg = await FeedbackAttachmentContent.ReadAsync(
        new MemoryStream("<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray()),
        "image/svg+xml");
      Problem(svg).Status.ShouldBe(415);

      OneOf<byte[], SharedProblemDetails> badMagic = await FeedbackAttachmentContent.ReadAsync(
        new MemoryStream("not a png"u8.ToArray()),
        FeedbackAttachmentRules.Png);
      Problem(badMagic).Status.ShouldBe(415);

      OneOf<byte[], SharedProblemDetails> script = await FeedbackAttachmentContent.ReadAsync(
        new MemoryStream("<script>alert(1)</script>"u8.ToArray()),
        FeedbackAttachmentRules.Text);
      Problem(script).Status.ShouldBe(415);

      OneOf<byte[], SharedProblemDetails> text = await FeedbackAttachmentContent.ReadAsync(
        new MemoryStream("hello"u8.ToArray()),
        FeedbackAttachmentRules.Text);
      text.IsT0.ShouldBeTrue();
    }

    public static async Task Upload_Should_RejectANameThatCannotBeStored()
    {
      World world = new();
      SharedProblemDetails problem = await world.UploadProblemAsync(
        world.Owner,
        "quote\".png",
        FeedbackAttachmentRules.Png,
        Png);
      problem.Status.ShouldBe(400);
    }

    public static async Task Download_Should_AllowTheOwnerAndAnAdminOnlyAfterLink()
    {
      World world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "shot.png", FeedbackAttachmentRules.Png, Png);
      PrincipalId other = PrincipalId.New();
      PrincipalId admin = PrincipalId.New();
      world.Permissions.Grant(admin, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminAccess);

      (await world.DownloadAsync(world.Owner, attachmentId)).ContentType.ShouldBe(FeedbackAttachmentRules.Png);
      (await world.DownloadProblemAsync(other, attachmentId)).Status.ShouldBe(404);
      (await world.DownloadProblemAsync(admin, attachmentId)).Status.ShouldBe(404);

      (await world.Attachments.TryLinkAsync(FeedbackAttachmentId.From(attachmentId), world.Owner.Value, FeedbackItemId.New()))
        .ShouldBeTrue();

      (await world.DownloadAsync(world.Owner, attachmentId)).FileName.ShouldBe("shot.png");
      (await world.DownloadProblemAsync(other, attachmentId)).Status.ShouldBe(404);
      DownloadFeedbackAttachment.Response allowed = await world.DownloadAsync(admin, attachmentId);
      await using Stream content = allowed.Content;
      byte[] bytes = new byte[Png.Length];
      (await content.ReadAsync(bytes)).ShouldBe(Png.Length);
      bytes.ShouldBe(Png);
    }

    public static Task Disposition_Should_NotBreakOutOfTheHeader()
    {
      string header = FeedbackAttachmentHttp.ContentDisposition(
        "a\"; filename=\"b\r\nSet-Cookie: x.png",
        FeedbackAttachmentRules.Pdf);
      header.ShouldNotContain("\r");
      header.ShouldNotContain("\n");
      header.ShouldStartWith("attachment; filename=\"");
      header.Count(character => character == '"').ShouldBe(2);
      return Task.CompletedTask;
    }

    public static Task Disposition_Should_UseAnAsciiFallbackForANonAsciiName()
    {
      string resume = FeedbackAttachmentHttp.ContentDisposition("résumé.pdf", FeedbackAttachmentRules.Pdf);
      resume.ShouldBe("attachment; filename=\"r_sum_.pdf\"; filename*=UTF-8''r%C3%A9sum%C3%A9.pdf");

      string screenshot = FeedbackAttachmentHttp.ContentDisposition(
        "Screenshot 2026-10-10 at 3.04.58\u202FPM.png",
        FeedbackAttachmentRules.Png);
      screenshot.ShouldStartWith("inline; filename=\"Screenshot 2026-10-10 at 3.04.58_PM.png\"; ");
      screenshot.ShouldContain("filename*=UTF-8''Screenshot%202026-10-10%20at%203.04.58%E2%80%AFPM.png");
      screenshot.All(character => character is >= ' ' and <= '~').ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static async Task HeaderName_Should_DecodeThenStoreOnlyTheLastSegment()
    {
      string traversal = FeedbackAttachmentHttp.DecodeFileNameHeader("..%2F..%2Fetc%2Fpasswd.txt");
      traversal.ShouldBe("../../etc/passwd.txt");
      FeedbackAttachmentHttp.DecodeFileNameHeader("").ShouldBe("");

      World world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, traversal, FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackAttachment stored = (await world.Attachments.FindAsync(FeedbackAttachmentId.From(attachmentId)))!;
      stored.FileName.ShouldBe("passwd.txt");
      stored.StorageKey.ShouldNotContain("..");
    }

    public static async Task HeaderName_Should_RejectADecodedLineBreak()
    {
      string injected = FeedbackAttachmentHttp.DecodeFileNameHeader("a%0d%0aSet-Cookie%3A%20x.png");
      injected.ShouldBe("a\r\nSet-Cookie: x.png");

      World world = new();
      SharedProblemDetails problem = await world.UploadProblemAsync(world.Owner, injected, FeedbackAttachmentRules.Png, Png);
      problem.Status.ShouldBe(400);
    }

    public static async Task Upload_Should_RefuseMoreThanThePendingCap()
    {
      World world = new();
      for (int index = 0; index < FeedbackAttachmentRules.MaxPerItem; index++)
      {
        await world.UploadAsync(world.Owner, $"notes-{index}.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      }

      SharedProblemDetails problem = await world.UploadProblemAsync(
        world.Owner,
        "one-more.txt",
        FeedbackAttachmentRules.Text,
        "hello"u8.ToArray());
      problem.Status.ShouldBe(409);
      (await world.Attachments.CountUnlinkedByOwnerAsync(world.Owner.Value)).ShouldBe(FeedbackAttachmentRules.MaxPerItem);

      PrincipalId other = PrincipalId.New();
      await world.UploadAsync(other, "theirs.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
    }

    public static async Task Upload_Should_DeleteExpiredPendingFilesBeforeCountingTheCap()
    {
      World world = new();
      DateTimeOffset expiredAt = DateTimeOffset.UtcNow - FeedbackAttachmentRules.PendingLifetime - TimeSpan.FromMinutes(1);
      List<FeedbackAttachment> stale = [];
      for (int index = 0; index < FeedbackAttachmentRules.MaxPerItem; index++)
      {
        var attachment = FeedbackAttachment.Create(
          world.Owner.Value,
          $"lost-{index}.txt",
          FeedbackAttachmentRules.Text,
          5,
          expiredAt);
        await world.Attachments.AddAsync(attachment);
        await world.Blobs.PutAsync(attachment.StorageKey, "hello"u8.ToArray(), FeedbackAttachmentRules.Text);
        stale.Add(attachment);
      }

      Guid filedId = await world.UploadAsync(world.Owner, "kept.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      (await world.Attachments.TryLinkAsync(FeedbackAttachmentId.From(filedId), world.Owner.Value, FeedbackItemId.New()))
        .ShouldBeTrue();

      Guid freshId = await world.UploadAsync(world.Owner, "fresh.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());

      foreach (FeedbackAttachment attachment in stale)
      {
        (await world.Attachments.FindAsync(attachment.Id)).ShouldBeNull();
        (await world.Blobs.OpenReadAsync(attachment.StorageKey)).ShouldBeNull();
      }

      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(filedId))).ShouldNotBeNull();
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(freshId))).ShouldNotBeNull();
      (await world.Attachments.CountUnlinkedByOwnerAsync(world.Owner.Value)).ShouldBe(1);
    }

    public static async Task Remove_Should_Return404ForAnotherPrincipalsPendingAttachment()
    {
      World world = new();
      PrincipalId other = PrincipalId.New();
      Guid foreign = await world.UploadAsync(other, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      SharedProblemDetails problem = await world.RemoveProblemAsync(world.Owner, foreign);
      problem.Status.ShouldBe(404);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(foreign))).ShouldNotBeNull();
    }

    public static async Task Link_Should_SucceedOnceAndOnlyForTheOwner()
    {
      World world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackAttachmentId id = FeedbackAttachmentId.From(attachmentId);
      (await world.Attachments.TryLinkAsync(id, Guid.NewGuid(), FeedbackItemId.New())).ShouldBeFalse();
      (await world.Attachments.TryLinkAsync(id, world.Owner.Value, FeedbackItemId.New())).ShouldBeTrue();
      (await world.Attachments.TryLinkAsync(id, world.Owner.Value, FeedbackItemId.New())).ShouldBeFalse();
    }

    public static async Task Submit_Should_RollBackWhenALinkLosesARace()
    {
      World world = new();
      Guid first = await world.UploadAsync(world.Owner, "first.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid second = await world.UploadAsync(world.Owner, "second.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackItemId elsewhere = FeedbackItemId.New();
      RacingAttachmentStore racing = new(world.Attachments, FeedbackAttachmentId.From(second), world.Owner.Value, elsewhere);

      SharedProblemDetails problem = await world.SubmitProblemAsync(world.Owner, [first, second], racing);
      problem.Status.ShouldBe(400);
      (await world.Feedback.ListByOwnerAsync(world.Owner.Value)).Count.ShouldBe(0);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(first)))!.FeedbackItemId.ShouldBeNull();
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(second)))!.FeedbackItemId.ShouldBe(elsewhere);
      UnavailableIds(problem).ShouldBe([second]);
    }

    public static async Task Submit_Should_UndoALinkThatCommittedThenThrewAndKeepTheError()
    {
      World world = new();
      Guid first = await world.UploadAsync(world.Owner, "first.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid second = await world.UploadAsync(world.Owner, "second.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      RacingAttachmentStore throwing = new(world.Attachments, FeedbackAttachmentId.From(second), world.Owner.Value, FeedbackItemId.New())
      {
        CommitThenThrow = true,
      };

      InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(
        () => world.SubmitThrowsAsync(world.Owner, [first, second], throwing));
      error.Message.ShouldBe("link committed then threw");
      (await world.Feedback.ListByOwnerAsync(world.Owner.Value)).Count.ShouldBe(0);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(first)))!.FeedbackItemId.ShouldBeNull();
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(second)))!.FeedbackItemId.ShouldBeNull();
    }

    public static async Task Submit_Should_StillRemoveTheItemWhenUnlinkFails()
    {
      World world = new();
      Guid first = await world.UploadAsync(world.Owner, "first.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid second = await world.UploadAsync(world.Owner, "second.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      RacingAttachmentStore failing = new(world.Attachments, FeedbackAttachmentId.From(second), world.Owner.Value, FeedbackItemId.New())
      {
        CommitThenThrow = true,
        ThrowOnUnlink = true,
      };

      InvalidOperationException error = await Should.ThrowAsync<InvalidOperationException>(
        () => world.SubmitThrowsAsync(world.Owner, [first, second], failing));
      error.Message.ShouldBe("link committed then threw");
      (await world.Feedback.ListByOwnerAsync(world.Owner.Value)).Count.ShouldBe(0);
    }

    public static async Task Submit_Should_ListEveryUnavailableAttachment()
    {
      World world = new();
      Guid kept = await world.UploadAsync(world.Owner, "kept.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid removed = await world.UploadAsync(world.Owner, "removed.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      await world.Attachments.RemoveAsync(FeedbackAttachmentId.From(removed));
      Guid missing = Guid.NewGuid();

      SharedProblemDetails problem = await world.SubmitProblemAsync(world.Owner, [kept, removed, missing]);
      problem.Status.ShouldBe(400);
      UnavailableIds(problem).ShouldBe([removed, missing]);
      (await world.Feedback.ListByOwnerAsync(world.Owner.Value)).Count.ShouldBe(0);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(kept)))!.FeedbackItemId.ShouldBeNull();
    }

    public static async Task Upload_Should_SucceedWhenAnExpiredBlobCannotBeDeleted()
    {
      World world = new();
      DateTimeOffset expiredAt = DateTimeOffset.UtcNow - FeedbackAttachmentRules.PendingLifetime - TimeSpan.FromMinutes(1);
      List<FeedbackAttachment> stale = [];
      for (int index = 0; index < 2; index++)
      {
        var attachment = FeedbackAttachment.Create(world.Owner.Value, $"lost-{index}.txt", FeedbackAttachmentRules.Text, 5, expiredAt);
        await world.Attachments.AddAsync(attachment);
        await world.Blobs.PutAsync(attachment.StorageKey, "hello"u8.ToArray(), FeedbackAttachmentRules.Text);
        stale.Add(attachment);
      }

      world.UploadBlobs = new UndeletableBlobStore(world.Blobs);
      Guid freshId = await world.UploadAsync(world.Owner, "fresh.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());

      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(freshId))).ShouldNotBeNull();
      foreach (FeedbackAttachment attachment in stale)
      {
        (await world.Attachments.FindAsync(attachment.Id)).ShouldBeNull();
      }
    }

    public static async Task Remove_Should_RejectALinkedAttachment()
    {
      World world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      (await world.Attachments.TryLinkAsync(FeedbackAttachmentId.From(attachmentId), world.Owner.Value, FeedbackItemId.New()))
        .ShouldBeTrue();
      SharedProblemDetails problem = await world.RemoveProblemAsync(world.Owner, attachmentId);
      problem.Status.ShouldBe(409);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(attachmentId))).ShouldNotBeNull();
    }

    public static async Task Submit_Should_LinkOwnedAttachmentsAndGetShouldReturnThem()
    {
      World world = new();
      Guid textId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid imageId = await world.UploadAsync(world.Owner, "shot.png", FeedbackAttachmentRules.Png, Png);
      SubmitFeedback.Response filed = await world.SubmitAsync(world.Owner, [textId, imageId]);
      filed.AttachmentIds.ShouldBe([textId, imageId]);

      GetFeedback.Response opened = await world.GetAsync(world.Owner, filed.FeedbackItemId);
      opened.Attachments.Count.ShouldBe(2);
      GetFeedback.Attachment notes = opened.Attachments.Single(attachment => attachment.FileName == "notes.txt");
      notes.IsImage.ShouldBeFalse();
      GetFeedback.Attachment image = opened.Attachments.Single(attachment => attachment.FileName == "shot.png");
      image.IsImage.ShouldBeTrue();
      image.DownloadPath.ShouldStartWith("/api/Feedback/attachments/");
    }

    public static async Task Submit_Should_RejectAnotherPrincipalsPendingAttachment()
    {
      World world = new();
      PrincipalId other = PrincipalId.New();
      Guid foreign = await world.UploadAsync(other, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      SharedProblemDetails problem = await world.SubmitProblemAsync(world.Owner, [foreign]);
      problem.Status.ShouldBe(400);
      (await world.Feedback.ListByOwnerAsync(world.Owner.Value)).Count.ShouldBe(0);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(foreign)))!.FeedbackItemId.ShouldBeNull();
    }

    private static List<Guid> UnavailableIds(SharedProblemDetails problem) =>
      problem.Extensions.TryGetValue(SubmitFeedback.UnavailableAttachmentIdsExtension, out object? value)
        && value is IEnumerable<Guid> ids
          ? ids.ToList()
          : [];

    private static SharedProblemDetails Problem(OneOf<byte[], SharedProblemDetails> result) =>
      result.Match(_ => throw new InvalidOperationException("Expected a problem"), problem => problem);

    private sealed class World
    {
      public InMemoryFeedbackStore Feedback { get; } = new();
      public InMemoryFeedbackAttachmentStore Attachments { get; } = new();
      public InMemoryFeedbackAttachmentBlobStore Blobs { get; } = new();
      public GrantingPermissionEvaluator Permissions { get; } = new();
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
        new(new StubCurrentPrincipalAccessor(principal), Attachments, UploadBlobs ?? Blobs, NullLogger<UploadHandler>.Instance);

      private DownloadHandler DownloadAs(PrincipalId principal) =>
        new(new StubCurrentPrincipalAccessor(principal), Attachments, Blobs, Permissions);

      private RemoveHandler RemoveAs(PrincipalId principal) =>
        new(new StubCurrentPrincipalAccessor(principal), Attachments, Blobs);

      private SubmitHandler SubmitAs(PrincipalId principal, IFeedbackAttachmentStore? attachmentStore = null) =>
        new(
          new StubCurrentPrincipalAccessor(principal),
          Feedback,
          new NoEmail(),
          new NoMail(),
          new NoBaseUrl(),
          NullLogger<SubmitHandler>.Instance,
          attachmentStore ?? Attachments);

      private GetHandler GetAs(PrincipalId principal) =>
        new(new StubCurrentPrincipalAccessor(principal), Feedback, Attachments);

      private static SubmitFeedback.Command Command(IReadOnlyList<Guid> attachmentIds) => new()
      {
        Kind = ContractKind.Complaint,
        Title = "With files",
        Body = "Details",
        AttachmentIds = attachmentIds.ToList(),
      };
    }

    /// <summary>Links one attachment to another item just before submit tries to link it.</summary>
    private sealed class RacingAttachmentStore : IFeedbackAttachmentStore
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
    private sealed class UndeletableBlobStore : IFeedbackAttachmentBlobStore
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

    private sealed class SizedStream : Stream
    {
      private long Remaining;

      public SizedStream(long length) => Remaining = length;

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

    private sealed class GrantingPermissionEvaluator : IPermissionEvaluator
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

    private sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId PrincipalId;

      public StubCurrentPrincipalAccessor(PrincipalId principalId) => PrincipalId = principalId;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult<PrincipalId?>(PrincipalId);
    }

    private sealed class NoEmail : IProfileEmailLookup
    {
      public Task<string?> FindEmailAsync(Guid principalId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
    }

    private sealed class NoMail : IEmailSender
    {
      public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
    }

    private sealed class NoBaseUrl : IAppBaseUrlAccessor
    {
      public Uri? GetBaseUrl() => null;
    }
  }
}
