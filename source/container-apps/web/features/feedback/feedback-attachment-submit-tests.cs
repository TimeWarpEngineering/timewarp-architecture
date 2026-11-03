#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008
#:include feedback-attachment-world-tests.cs

// Co-located Jaribu: filing that links pending attachments, lists unavailable ids, and rolls back on failure.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-attachment-submit-tests.cs

#region Purpose
// Jaribu runfile for filing that links only the caller's pending uploads, lists unavailable
// ids, rejects a linked attachment on remove, and rolls back when a link loses a race, commits
// then throws, or the rollback's unlink step fails.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{
  using Shouldly;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Handler")]
  public class FeedbackAttachmentSubmit_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackAttachmentSubmit_Given_>();

    public static async Task Link_Should_SucceedOnceAndOnlyForTheOwner()
    {
      AttachmentWorld world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackAttachmentId id = FeedbackAttachmentId.From(attachmentId);
      (await world.Attachments.TryLinkAsync(id, Guid.NewGuid(), FeedbackItemId.New())).ShouldBeFalse();
      (await world.Attachments.TryLinkAsync(id, world.Owner.Value, FeedbackItemId.New())).ShouldBeTrue();
      (await world.Attachments.TryLinkAsync(id, world.Owner.Value, FeedbackItemId.New())).ShouldBeFalse();
    }

    public static async Task Submit_Should_RollBackWhenALinkLosesARace()
    {
      AttachmentWorld world = new();
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
      AttachmentWorld world = new();
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
      AttachmentWorld world = new();
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
      AttachmentWorld world = new();
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

    public static async Task Remove_Should_RejectALinkedAttachment()
    {
      AttachmentWorld world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      (await world.Attachments.TryLinkAsync(FeedbackAttachmentId.From(attachmentId), world.Owner.Value, FeedbackItemId.New()))
        .ShouldBeTrue();
      SharedProblemDetails problem = await world.RemoveProblemAsync(world.Owner, attachmentId);
      problem.Status.ShouldBe(409);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(attachmentId))).ShouldNotBeNull();
    }

    public static async Task Submit_Should_LinkOwnedAttachmentsAndGetShouldReturnThem()
    {
      AttachmentWorld world = new();
      Guid textId = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      Guid imageId = await world.UploadAsync(world.Owner, "shot.png", FeedbackAttachmentRules.Png, AttachmentWorld.Png);
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
      AttachmentWorld world = new();
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
  }
}
