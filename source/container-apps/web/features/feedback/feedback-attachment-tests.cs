#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008
#:include feedback-attachment-world-tests.cs

// Co-located Jaribu: attachment limits, owner-or-admin download, safe headers, upload cap, and remove.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-attachment-tests.cs

#region Purpose
// Jaribu runfile for attachment size and type limits, the pending cap and its expiry, the
// owner-or-admin download rule, safe download headers and X-File-Name decoding, removing a
// pending attachment, and an expiry that survives a blob delete that throws. A row whose bytes
// are gone downloads as a 404, not a 500.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Handler")]
  public class FeedbackAttachment_Given_
  {
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
        new AttachmentSizedStream(FeedbackAttachmentRules.MaxBytes + 1),
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
      AttachmentWorld world = new();
      SharedProblemDetails problem = await world.UploadProblemAsync(
        world.Owner,
        "quote\".png",
        FeedbackAttachmentRules.Png,
        AttachmentWorld.Png);
      problem.Status.ShouldBe(400);
    }

    public static async Task Download_Should_AllowTheOwnerAndAnAdminOnlyAfterLink()
    {
      AttachmentWorld world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "shot.png", FeedbackAttachmentRules.Png, AttachmentWorld.Png);
      PrincipalId other = PrincipalId.New();
      PrincipalId admin = PrincipalId.New();
      PrincipalId adminAreaOnly = PrincipalId.New();
      PrincipalId agentReader = PrincipalId.New();
      PrincipalId agentNoteOnly = PrincipalId.New();
      world.Permissions.Grant(admin, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminFeedbackRead);
      world.Permissions.Grant(adminAreaOnly, AuthenticationSchemeNames.IdentitySession, PermissionIds.AdminAccess);
      world.Permissions.Grant(agentReader, AuthenticationSchemeNames.AgentToken, PermissionIds.FeedbackRead);
      world.Permissions.Grant(agentNoteOnly, AuthenticationSchemeNames.AgentToken, PermissionIds.FeedbackNote);
      world.Agents.Mark(agentReader);
      world.Agents.Mark(agentNoteOnly);

      (await world.DownloadAsync(world.Owner, attachmentId)).ContentType.ShouldBe(FeedbackAttachmentRules.Png);
      (await world.DownloadProblemAsync(other, attachmentId)).Status.ShouldBe(404);
      (await world.DownloadProblemAsync(admin, attachmentId)).Status.ShouldBe(404);
      (await world.DownloadProblemAsync(agentReader, attachmentId)).Status.ShouldBe(404);

      (await world.Attachments.TryLinkAsync(FeedbackAttachmentId.From(attachmentId), world.Owner.Value, FeedbackItemId.New()))
        .ShouldBeTrue();

      (await world.DownloadAsync(world.Owner, attachmentId)).FileName.ShouldBe("shot.png");
      (await world.DownloadProblemAsync(other, attachmentId)).Status.ShouldBe(404);
      (await world.DownloadProblemAsync(adminAreaOnly, attachmentId)).Status.ShouldBe(404);
      (await world.DownloadAsync(agentReader, attachmentId)).FileName.ShouldBe("shot.png");
      (await world.DownloadProblemAsync(agentNoteOnly, attachmentId)).Status.ShouldBe(404);
      DownloadFeedbackAttachment.Response allowed = await world.DownloadAsync(admin, attachmentId);
      await using Stream content = allowed.Content;
      byte[] bytes = new byte[AttachmentWorld.Png.Length];
      (await content.ReadAsync(bytes)).ShouldBe(AttachmentWorld.Png.Length);
      bytes.ShouldBe(AttachmentWorld.Png);
    }

    public static async Task Download_Should_IgnoreHumanRolesWhenTheCallerIsAnAgentToken()
    {
      AttachmentWorld world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, "shot.png", FeedbackAttachmentRules.Png, AttachmentWorld.Png);
      (await world.Attachments.TryLinkAsync(
          FeedbackAttachmentId.From(attachmentId),
          world.Owner.Value,
          FeedbackItemId.New()))
        .ShouldBeTrue();

      PrincipalId agent = PrincipalId.New();
      PrincipalId humanAdmin = PrincipalId.New();
      PrincipalId reader = PrincipalId.New();
      InMemoryPrincipalRoleStore roles = new();
      await roles.SetRoleIdsAsync(agent, [RoleIds.Administrator]);
      await roles.SetRoleIdsAsync(humanAdmin, [RoleIds.Administrator]);
      AttachmentHeldScopes scopes = new();
      scopes.Hold(agent, AgentScopes.FeedbackNote);
      scopes.Hold(reader, AgentScopes.FeedbackRead);
      PermissionEvaluator evaluator = new(
        new EffectiveRolesResolver(
          roles,
          Options.Create(new BootstrapAdministratorOptions { BootstrapAdministratorPrincipalIds = [] }),
          new InMemoryPrincipalStore()),
        new InMemoryRolePermissionStore(),
        scopes);

      // The Administrator role includes admin.feedback.read. That grant is real for a human
      // session and must not open the file for the agent token.
      (await world.DownloadWithAsync(humanAdmin, attachmentId, evaluator, scopes)).FileName.ShouldBe("shot.png");
      (await world.DownloadProblemWithAsync(agent, attachmentId, evaluator, scopes)).Status.ShouldBe(404);
      (await world.DownloadWithAsync(reader, attachmentId, evaluator, scopes)).FileName.ShouldBe("shot.png");
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

      AttachmentWorld world = new();
      Guid attachmentId = await world.UploadAsync(world.Owner, traversal, FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackAttachment stored = (await world.Attachments.FindAsync(FeedbackAttachmentId.From(attachmentId)))!;
      stored.FileName.ShouldBe("passwd.txt");
      stored.StorageKey.ShouldNotContain("..");
    }

    public static async Task HeaderName_Should_RejectADecodedLineBreak()
    {
      string injected = FeedbackAttachmentHttp.DecodeFileNameHeader("a%0d%0aSet-Cookie%3A%20x.png");
      injected.ShouldBe("a\r\nSet-Cookie: x.png");

      AttachmentWorld world = new();
      SharedProblemDetails problem = await world.UploadProblemAsync(world.Owner, injected, FeedbackAttachmentRules.Png, AttachmentWorld.Png);
      problem.Status.ShouldBe(400);
    }

    public static async Task Upload_Should_RefuseMoreThanThePendingCap()
    {
      AttachmentWorld world = new();
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
      AttachmentWorld world = new();
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
      AttachmentWorld world = new();
      PrincipalId other = PrincipalId.New();
      Guid foreign = await world.UploadAsync(other, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      SharedProblemDetails problem = await world.RemoveProblemAsync(world.Owner, foreign);
      problem.Status.ShouldBe(404);
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(foreign))).ShouldNotBeNull();
    }

    public static async Task Download_Should_Return404WhenTheBytesAreGone()
    {
      AttachmentWorld world = new();
      Guid id = await world.UploadAsync(world.Owner, "notes.txt", FeedbackAttachmentRules.Text, "hello"u8.ToArray());
      FeedbackAttachment row = (await world.Attachments.FindAsync(FeedbackAttachmentId.From(id))).ShouldNotBeNull();
      await world.Blobs.DeleteAsync(row.StorageKey);
      SharedProblemDetails problem = await world.DownloadProblemAsync(world.Owner, id);
      problem.Status.ShouldBe(404);
      problem.Title.ShouldBe("Attachment no longer available");
      (await world.Attachments.FindAsync(FeedbackAttachmentId.From(id))).ShouldNotBeNull();
    }
    private static SharedProblemDetails Problem(OneOf<byte[], SharedProblemDetails> result) =>
      result.Match(_ => throw new InvalidOperationException("Expected a problem"), problem => problem);
  }
}
