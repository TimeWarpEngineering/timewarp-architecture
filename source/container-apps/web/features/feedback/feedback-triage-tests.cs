#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: admin feedback triage (status, replies, internal notes) and the filer's thread.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs

#region Purpose
// Jaribu runfile for task 314: the FeedbackItem status and thread mutations and their
// invariants, the admin list (cross-owner, closed hidden by default, filters, paging, and the
// task 327 sort: each column both ways, before paging, id tiebreaker), the
// admin detail, status changes (never back to New, 409 on the current status, email only for
// Planned/Completed/Declined), admin replies (always emailed when an address is known),
// internal notes (never shown to the filer, never emailed), filer replies (own items only),
// contract validators, and the authorization surface: each contract's [EndpointAuthorize]
// policy and the Administrator-only seed. Task 336: the owner display name on list rows and the
// admin view (set only with admin.profiles.read, null for a missing profile, one directory
// batch per page). HTTP 401/403 and no-PII proofs live in
// web-server-integration-tests (admin-feedback-authorization-tests).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{

  using System.Reflection;
  using System.Text.Json;
  using FluentValidation.Results;
  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using AddNoteContract = TimeWarp.Architecture.Features.Feedback.AddFeedbackNote;
  using AddNoteHandler = TimeWarp.Architecture.Features.Feedback.Application.AddFeedbackNote.Handler;
  using AdminGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedbackForAdmin;
  using AdminGetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedbackForAdmin.Handler;
  using AdminListContract = TimeWarp.Architecture.Features.Feedback.ListFeedback;
  using AdminListHandler = TimeWarp.Architecture.Features.Feedback.Application.ListFeedback.Handler;
  using ChangeStatusContract = TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
  using ChangeStatusHandler = TimeWarp.Architecture.Features.Feedback.Application.ChangeFeedbackStatus.Handler;
  using ContractActivityKind = TimeWarp.Architecture.Features.Feedback.FeedbackActivityKind;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
  using DomainActivityKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackActivityKind;
  using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;
  using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;
  using FilerGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;
  using FilerGetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedback.Handler;
  using FilerListContract = TimeWarp.Architecture.Features.Feedback.ListMyFeedback;
  using FilerListHandler = TimeWarp.Architecture.Features.Feedback.Application.ListMyFeedback.Handler;
  using FilerReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyAsFiler;
  using FilerReplyHandler = TimeWarp.Architecture.Features.Feedback.Application.ReplyAsFiler.Handler;
  using ReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyToFeedback;
  using ReplyHandler = TimeWarp.Architecture.Features.Feedback.Application.ReplyToFeedback.Handler;

  [TestTag("Domain")]
  public class FeedbackItemTriage_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackItemTriage_Given_>();

    private static readonly DateTimeOffset At = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    public static Task File_Should_StartNewWithAnEmptyThread()
    {
      FeedbackItem item = Filed(Guid.NewGuid());
      item.Status.ShouldBe(DomainStatus.New);
      item.Activity.ShouldBeEmpty();
      return Task.CompletedTask;
    }

    public static Task ChangeStatus_Should_RecordFromToWhoAndWhen()
    {
      FeedbackItem item = Filed(Guid.NewGuid());
      var admin = Guid.NewGuid();
      FeedbackActivity entry = item.ChangeStatus(DomainStatus.UnderReview, admin, At);
      item.Status.ShouldBe(DomainStatus.UnderReview);
      entry.Kind.ShouldBe(DomainActivityKind.StatusChange);
      entry.FromStatus.ShouldBe(DomainStatus.New);
      entry.ToStatus.ShouldBe(DomainStatus.UnderReview);
      entry.AuthorPrincipalId.ShouldBe(admin);
      entry.At.ShouldBe(At);
      entry.Sequence.ShouldBe(1);
      entry.Body.ShouldBeNull();
      return Task.CompletedTask;
    }

    public static Task ChangeStatus_Should_RefuseNewAndTheCurrentStatus()
    {
      FeedbackItem item = Filed(Guid.NewGuid());
      var admin = Guid.NewGuid();
      Should.Throw<InvalidOperationException>(() => item.ChangeStatus(DomainStatus.New, admin, At));
      item.ChangeStatus(DomainStatus.Planned, admin, At);
      Should.Throw<InvalidOperationException>(() => item.ChangeStatus(DomainStatus.Planned, admin, At));
      Should.Throw<InvalidOperationException>(() => item.ChangeStatus(DomainStatus.New, admin, At));
      Should.Throw<ArgumentException>(() => item.ChangeStatus((DomainStatus)99, admin, At));
      Should.Throw<ArgumentException>(() => item.ChangeStatus(DomainStatus.Declined, Guid.Empty, At));
      item.CanChangeStatusTo(DomainStatus.Planned).ShouldBeFalse();
      item.CanChangeStatusTo(DomainStatus.New).ShouldBeFalse();
      item.CanChangeStatusTo(DomainStatus.Completed).ShouldBeTrue();
      item.Activity.Count.ShouldBe(1);
      return Task.CompletedTask;
    }

    public static Task ClosedItem_Should_ReopenToUnderReview()
    {
      FeedbackItem item = Filed(Guid.NewGuid());
      var admin = Guid.NewGuid();
      item.ChangeStatus(DomainStatus.Declined, admin, At);
      item.ChangeStatus(DomainStatus.UnderReview, admin, At.AddMinutes(1));
      item.Status.ShouldBe(DomainStatus.UnderReview);
      item.Activity.Select(entry => entry.Sequence).ShouldBe([1, 2]);
      return Task.CompletedTask;
    }

    public static Task Replies_Should_TrimAndOnlyTheOwnerMayFileAFilerReply()
    {
      var owner = Guid.NewGuid();
      FeedbackItem item = Filed(owner);
      FeedbackActivity admin = item.AddAdminReply(Guid.NewGuid(), "  Thanks  ", At);
      FeedbackActivity filer = item.AddFilerReply(owner, "More detail", At.AddMinutes(1));
      admin.Body.ShouldBe("Thanks");
      admin.Kind.ShouldBe(DomainActivityKind.AdminReply);
      filer.Kind.ShouldBe(DomainActivityKind.FilerReply);
      Should.Throw<InvalidOperationException>(() => item.AddFilerReply(Guid.NewGuid(), "Not mine", At));
      Should.Throw<ArgumentException>(() => item.AddAdminReply(Guid.NewGuid(), "   ", At));
      Should.Throw<ArgumentOutOfRangeException>(() =>
        item.AddAdminReply(Guid.NewGuid(), new string('x', FeedbackItem.MaxMessageLength + 1), At));
      item.Activity.Count.ShouldBe(2);
      return Task.CompletedTask;
    }

    public static Task InternalNote_Should_BeHiddenFromTheFiler()
    {
      FeedbackItem item = Filed(Guid.NewGuid());
      FeedbackActivity note = item.AddInternalNote(Guid.NewGuid(), "Duplicate of an older bug", At);
      note.Kind.ShouldBe(DomainActivityKind.InternalNote);
      note.IsVisibleToFiler.ShouldBeFalse();
      item.AddAdminReply(Guid.NewGuid(), "Looking", At).IsVisibleToFiler.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EnumNames_Should_MatchBetweenContractAndDomain()
    {
      Enum.GetNames<ContractStatus>().ShouldBe(Enum.GetNames<DomainStatus>());
      Enum.GetNames<ContractActivityKind>().ShouldBe(Enum.GetNames<DomainActivityKind>());
      return Task.CompletedTask;
    }

    public static Task StatusRules_Should_NeverOfferNewAndEmailOnlyTheEndOfTheRoad()
    {
      FeedbackStatusRules.Assignable.ShouldNotContain(ContractStatus.New);
      FeedbackStatusRules.Assignable.Count.ShouldBe(Enum.GetValues<ContractStatus>().Length - 1);
      Enum.GetValues<ContractStatus>().Where(FeedbackStatusRules.NotifiesFiler)
        .ShouldBe([ContractStatus.Planned, ContractStatus.Completed, ContractStatus.Declined]);
      Enum.GetValues<ContractStatus>().Where(FeedbackStatusRules.IsClosed)
        .ShouldBe([ContractStatus.Completed, ContractStatus.Declined]);
      return Task.CompletedTask;
    }

    public static Task MessageLengths_Should_AgreeBetweenContractsAndDomain()
    {
      ReplyContract.MaxBodyLength.ShouldBe(FeedbackItem.MaxMessageLength);
      AddNoteContract.MaxBodyLength.ShouldBe(FeedbackItem.MaxMessageLength);
      FilerReplyContract.MaxBodyLength.ShouldBe(FeedbackItem.MaxMessageLength);
      return Task.CompletedTask;
    }

    internal static FeedbackItem Filed(Guid owner, DomainKind kind = DomainKind.BugReport, DateTimeOffset? filedAt = null) =>
      FeedbackItem.File(owner, kind, "Export failed", "Nothing came back.", filedAt ?? At);
  }

  [TestTag("Handler")]
  public class FeedbackAdmin_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackAdmin_Given_>();

    public static async Task List_Should_ShowEveryOwnerAndHideClosedByDefault()
    {
      TriageWorld world = new();
      FeedbackItem mine = await world.AddAsync(world.Filer.Value, day: 1);
      FeedbackItem theirs = await world.AddAsync(Guid.NewGuid(), day: 2);
      FeedbackItem closed = await world.AddAsync(Guid.NewGuid(), day: 3);
      closed.ChangeStatus(DomainStatus.Completed, world.Admin.Value, DateTimeOffset.UtcNow);
      await world.Feedback.UpdateAsync(closed);

      AdminListContract.Response open = await world.ListAsync(new AdminListContract.Query());
      open.TotalCount.ShouldBe(2);
      open.Items.Select(item => item.FeedbackItemId).ShouldBe([theirs.Id.Value, mine.Id.Value]);
      open.Items[0].OwnerPrincipalId.ShouldBe(theirs.OwnerPrincipalId);
      open.Items[0].Status.ShouldBe(ContractStatus.New);

      AdminListContract.Response all = await world.ListAsync(new AdminListContract.Query { IncludeClosed = true });
      all.TotalCount.ShouldBe(3);
      all.Items[0].Status.ShouldBe(ContractStatus.Completed);

      AdminListContract.Response onlyCompleted = await world.ListAsync(
        new AdminListContract.Query { Status = ContractStatus.Completed });
      onlyCompleted.Items.Select(item => item.FeedbackItemId).ShouldBe([closed.Id.Value]);
    }

    public static async Task List_Should_FilterByKindAndDateAndPage()
    {
      TriageWorld world = new();
      for (int day = 1; day <= 5; day++)
      {
        await world.AddAsync(Guid.NewGuid(), day, day % 2 == 0 ? DomainKind.FeatureRequest : DomainKind.BugReport);
      }

      AdminListContract.Response features = await world.ListAsync(
        new AdminListContract.Query { Kind = ContractKind.FeatureRequest });
      features.TotalCount.ShouldBe(2);
      features.Items.ShouldAllBe(item => item.Kind == ContractKind.FeatureRequest);

      AdminListContract.Response window = await world.ListAsync(new AdminListContract.Query
      {
        FiledFrom = TriageWorld.Day(2),
        FiledTo = TriageWorld.Day(4),
      });
      window.Items.Select(item => item.FiledAt).ShouldBe([TriageWorld.Day(3), TriageWorld.Day(2)]);

      AdminListContract.Response page2 = await world.ListAsync(new AdminListContract.Query { Page = 2, PageSize = 2 });
      page2.TotalCount.ShouldBe(5);
      page2.Page.ShouldBe(2);
      page2.PageSize.ShouldBe(2);
      page2.Items.Select(item => item.FiledAt).ShouldBe([TriageWorld.Day(3), TriageWorld.Day(2)]);
    }

    public static async Task List_Should_SortEachColumnBothWaysBeforePaging()
    {
      TriageWorld world = new();
      FeedbackItem apple = await world.AddTitledAsync("Apple", DomainKind.BugReport, day: 1);
      FeedbackItem appleTwin = await world.AddTitledAsync("Apple", DomainKind.BugReport, day: 1, DomainStatus.Planned);
      FeedbackItem banana = await world.AddTitledAsync("banana", DomainKind.FeatureRequest, day: 3);
      FeedbackItem cherry = await world.AddTitledAsync("cherry", DomainKind.Complaint, day: 2, DomainStatus.UnderReview);
      // apple and appleTwin share Title, Kind, and FiledAt, so only the id tiebreaker orders them.
      (Guid low, Guid high) = apple.Id.Value.CompareTo(appleTwin.Id.Value) < 0
        ? (apple.Id.Value, appleTwin.Id.Value)
        : (appleTwin.Id.Value, apple.Id.Value);
      Guid a = apple.Id.Value, b = banana.Id.Value, c = cherry.Id.Value, t = appleTwin.Id.Value;

      (await world.SortedIdsAsync(FeedbackListSortColumn.Title, descending: false)).ShouldBe([low, high, b, c]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.Title, descending: true)).ShouldBe([c, b, low, high]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.Kind, descending: false)).ShouldBe([low, high, c, b]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.Kind, descending: true)).ShouldBe([b, c, low, high]);
      // By name: New, Planned, UnderReview. Inside New, newest first.
      (await world.SortedIdsAsync(FeedbackListSortColumn.Status, descending: false)).ShouldBe([b, a, t, c]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.Status, descending: true)).ShouldBe([c, t, b, a]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.FiledAt, descending: false)).ShouldBe([low, high, c, b]);
      (await world.SortedIdsAsync(FeedbackListSortColumn.FiledAt, descending: true)).ShouldBe([b, c, low, high]);
      (await world.ListAsync(new AdminListContract.Query { IncludeClosed = true }))
        .Items.Select(item => item.FeedbackItemId).ShouldBe([b, c, low, high], "default is FiledAt descending");

      AdminListContract.Response page2 = await world.ListAsync(new AdminListContract.Query
      {
        IncludeClosed = true,
        SortBy = FeedbackListSortColumn.Title,
        SortDescending = false,
        Page = 2,
        PageSize = 2,
      });
      page2.TotalCount.ShouldBe(4);
      page2.Items.Select(item => item.Title).ShouldBe(["banana", "cherry"], "the sort spans every page");
    }

    public static Task ListValidator_Should_RejectBadPagingAndBackwardsDates()
    {
      AdminListContract.Validator validator = new();
      validator.Validate(new AdminListContract.Query()).IsValid.ShouldBeTrue();
      validator.Validate(new AdminListContract.Query { Page = 0 }).IsValid.ShouldBeFalse();
      validator.Validate(new AdminListContract.Query { PageSize = AdminListContract.MaxPageSize + 1 }).IsValid.ShouldBeFalse();
      validator.Validate(new AdminListContract.Query { Page = AdminListContract.MaxPage + 1 }).IsValid.ShouldBeFalse();
      validator.Validate(new AdminListContract.Query { Page = AdminListContract.MaxPage, PageSize = AdminListContract.MaxPageSize }).IsValid.ShouldBeTrue();
      validator.Validate(new AdminListContract.Query { FiledFrom = TriageWorld.Day(3), FiledTo = TriageWorld.Day(2) })
        .IsValid.ShouldBeFalse();
      validator.Validate(new AdminListContract.Query { SortBy = (FeedbackListSortColumn)99 }).IsValid.ShouldBeFalse();
      validator.Validate(new AdminListContract.Query { SortBy = FeedbackListSortColumn.Title, SortDescending = false })
        .IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task ListQuery_Should_CarryFiltersInTheQueryString()
    {
      string route = new AdminListContract.Query
      {
        Status = ContractStatus.Planned,
        Kind = ContractKind.BugReport,
        IncludeClosed = true,
        Page = 3,
        PageSize = 10,
        SortBy = FeedbackListSortColumn.Title,
        SortDescending = false,
      }.GetRouteWithQueryString();
      route.ShouldStartWith("api/admin/feedback?");
      route.ShouldContain("Status=Planned");
      route.ShouldContain("Kind=BugReport");
      route.ShouldContain("IncludeClosed=true");
      route.ShouldContain("Page=3");
      route.ShouldContain("PageSize=10");
      route.ShouldContain("SortBy=Title");
      route.ShouldContain("SortDescending=false");
      return Task.CompletedTask;
    }

    public static async Task AdminGet_Should_ReturnAnyOwnersItemWithNotesAndEmailPresence()
    {
      TriageWorld world = new();
      await world.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      await world.NoteAsync(item.Id.Value, "Seen before");

      AdminGetContract.Response response = (await world.AdminGet().Handle(
          new AdminGetContract.Query { FeedbackItemId = item.Id.Value },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected admin get success"));
      response.Item.OwnerPrincipalId.ShouldBe(world.Filer.Value);
      response.Item.FilerHasEmail.ShouldBeTrue();
      response.Item.Body.ShouldBe("Nothing came back.");
      response.Item.Activity.Count.ShouldBe(1);
      response.Item.Activity[0].Kind.ShouldBe(ContractActivityKind.InternalNote);
      response.Item.Activity[0].AuthorPrincipalId.ShouldBe(world.Admin.Value);
    }

    public static async Task AdminGet_Should_Return404ForAMissingId()
    {
      TriageWorld world = new();
      SharedProblemDetails problem = (await world.AdminGet().Handle(
          new AdminGetContract.Query { FeedbackItemId = Guid.NewGuid() },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected 404"), details => details);
      problem.Status.ShouldBe(404);
    }

    public static async Task ChangeStatus_Should_RecordAndEmailOnlyForNotifyingStatuses()
    {
      TriageWorld world = new();
      await world.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);

      ChangeStatusContract.Response underReview = await world.ChangeStatusAsync(item.Id.Value, ContractStatus.UnderReview);
      underReview.FilerEmailed.ShouldBeFalse();
      underReview.Item.Status.ShouldBe(ContractStatus.UnderReview);
      world.Mail.Messages.ShouldBeEmpty();

      ChangeStatusContract.Response planned = await world.ChangeStatusAsync(item.Id.Value, ContractStatus.Planned);
      planned.FilerEmailed.ShouldBeTrue();
      planned.Item.Activity.Count.ShouldBe(2);
      planned.Item.Activity[1].FromStatus.ShouldBe(ContractStatus.UnderReview);
      planned.Item.Activity[1].ToStatus.ShouldBe(ContractStatus.Planned);
      world.Mail.Messages.Count.ShouldBe(1);
      EmailMessage message = world.Mail.Messages[0];
      message.To.ShouldBe("ada@example.com");
      message.Subject.ShouldContain("Planned");
      message.Body.ShouldContain($"https://app.example/Feedback/{item.Id.Value:D}");

      (await world.ChangeStatusAsync(item.Id.Value, ContractStatus.InProgress)).FilerEmailed.ShouldBeFalse();
      (await world.ChangeStatusAsync(item.Id.Value, ContractStatus.Completed)).FilerEmailed.ShouldBeTrue();
      (await world.ChangeStatusAsync(item.Id.Value, ContractStatus.Declined)).FilerEmailed.ShouldBeTrue();
      world.Mail.Messages.Count.ShouldBe(3);

      FilerGetContract.Response filerView = await world.FilerGetAsync(item.Id.Value);
      filerView.Status.ShouldBe(ContractStatus.Declined);
      filerView.Activity.Count.ShouldBe(5);
      filerView.Activity.ShouldAllBe(entry => entry.AuthorPrincipalId == null);

      FilerListContract.Response filerList = await world.FilerListAsync();
      filerList.Items.Single().Status.ShouldBe(ContractStatus.Declined);
      Console.WriteLine($"TRIAGE-PROOF id={item.Id.Value:D} status=Declined mails={world.Mail.Messages.Count}");
    }

    public static async Task ChangeStatus_Should_Refuse_New_CurrentAndMissing()
    {
      TriageWorld world = new();
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      (await world.ChangeStatusProblemAsync(item.Id.Value, ContractStatus.New)).Status.ShouldBe(400);
      (await world.ChangeStatusProblemAsync(item.Id.Value, (ContractStatus)99)).Status.ShouldBe(400);
      await world.ChangeStatusAsync(item.Id.Value, ContractStatus.Planned);
      (await world.ChangeStatusProblemAsync(item.Id.Value, ContractStatus.Planned)).Status.ShouldBe(409);
      (await world.ChangeStatusProblemAsync(Guid.NewGuid(), ContractStatus.Planned)).Status.ShouldBe(404);
    }

    public static async Task ChangeStatus_Should_SucceedWhenMailFailsOrNoAddressIsKnown()
    {
      TriageWorld broken = new(new TriageBrokenMail());
      await broken.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await broken.AddAsync(broken.Filer.Value, day: 1);
      ChangeStatusContract.Response response = await broken.ChangeStatusAsync(item.Id.Value, ContractStatus.Completed);
      response.FilerEmailed.ShouldBeFalse();
      response.Item.Status.ShouldBe(ContractStatus.Completed);

      TriageWorld silent = new();
      FeedbackItem other = await silent.AddAsync(silent.Filer.Value, day: 1);
      (await silent.ChangeStatusAsync(other.Id.Value, ContractStatus.Completed)).FilerEmailed.ShouldBeFalse();
      silent.Mail.Messages.ShouldBeEmpty();
    }

    public static Task ChangeStatusValidator_Should_RejectNew()
    {
      ChangeStatusContract.Validator validator = new();
      Guid id = Guid.NewGuid();
      validator.Validate(new ChangeStatusContract.Command { FeedbackItemId = id, Status = ContractStatus.New })
        .IsValid.ShouldBeFalse();
      validator.Validate(new ChangeStatusContract.Command { FeedbackItemId = id, Status = ContractStatus.Planned })
        .IsValid.ShouldBeTrue();
      validator.Validate(new ChangeStatusContract.Command { FeedbackItemId = Guid.Empty, Status = ContractStatus.Planned })
        .IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static async Task AdminReply_Should_ReachTheFilerInAppAndByEmail()
    {
      TriageWorld world = new();
      await world.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      ReplyContract.Response response = (await world.Reply().Handle(
          new ReplyContract.Command { FeedbackItemId = item.Id.Value, Body = "  Fixed in the next release.  " },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected reply success"));
      response.FilerEmailed.ShouldBeTrue();
      response.Item.Activity.Single().Body.ShouldBe("Fixed in the next release.");
      world.Mail.Messages.Single().Body.ShouldContain("Fixed in the next release.");

      FilerGetContract.Response filerView = await world.FilerGetAsync(item.Id.Value);
      filerView.Activity.Single().Kind.ShouldBe(ContractActivityKind.AdminReply);
      filerView.Activity.Single().AuthorPrincipalId.ShouldBeNull();
    }

    public static async Task InternalNote_Should_StayAdminOnlyAndSendNoMail()
    {
      TriageWorld world = new();
      await world.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      AddNoteContract.Response response = await world.NoteAsync(item.Id.Value, "Probably a duplicate");
      response.Item.Activity.Single().Kind.ShouldBe(ContractActivityKind.InternalNote);
      world.Mail.Messages.ShouldBeEmpty();

      FilerGetContract.Response filerView = await world.FilerGetAsync(item.Id.Value);
      filerView.Activity.ShouldBeEmpty();
      string json = JsonSerializer.Serialize(filerView, ContractSerializationDefaults.Options);
      json.ShouldNotContain("Probably a duplicate");
    }

    public static async Task MessageHandlers_Should_RefuseBlankOrOverlongBodies()
    {
      TriageWorld world = new();
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      string tooLong = new('x', FeedbackItem.MaxMessageLength + 1);
      foreach (string body in new[] { "   ", tooLong })
      {
        (await world.Reply().Handle(new ReplyContract.Command { FeedbackItemId = item.Id.Value, Body = body }, CancellationToken.None))
          .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(400);
        (await world.Note().Handle(new AddNoteContract.Command { FeedbackItemId = item.Id.Value, Body = body }, CancellationToken.None))
          .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(400);
        (await world.FilerReply(world.Filer).Handle(
            new FilerReplyContract.Command { FeedbackItemId = item.Id.Value, Body = body },
            CancellationToken.None))
          .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(400);
      }

      new ReplyContract.Validator().Validate(new ReplyContract.Command { FeedbackItemId = item.Id.Value, Body = "" })
        .IsValid.ShouldBeFalse();
      new AddNoteContract.Validator().Validate(new AddNoteContract.Command { FeedbackItemId = item.Id.Value, Body = tooLong })
        .IsValid.ShouldBeFalse();
      ValidationResult ok = new FilerReplyContract.Validator().Validate(
        new FilerReplyContract.Command { FeedbackItemId = item.Id.Value, Body = "Thanks" });
      ok.IsValid.ShouldBeTrue();
      (await world.FilerGetAsync(item.Id.Value)).Activity.ShouldBeEmpty();
    }

    public static async Task FilerReply_Should_AnswerOwnItemsOnly()
    {
      TriageWorld world = new();
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      await world.NoteAsync(item.Id.Value, "Internal");
      FilerReplyContract.Response response = (await world.FilerReply(world.Filer).Handle(
          new FilerReplyContract.Command { FeedbackItemId = item.Id.Value, Body = "Steps to reproduce: ..." },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected filer reply success"));
      response.Status.ShouldBe(ContractStatus.New);
      response.Activity.Single().Kind.ShouldBe(ContractActivityKind.FilerReply);

      SharedProblemDetails foreign = (await world.FilerReply(PrincipalId.New()).Handle(
          new FilerReplyContract.Command { FeedbackItemId = item.Id.Value, Body = "Not mine" },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected 404"), problem => problem);
      foreign.Status.ShouldBe(404);

      AdminGetContract.Response admin = (await world.AdminGet().Handle(
          new AdminGetContract.Query { FeedbackItemId = item.Id.Value },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected admin get success"));
      admin.Item.Activity.Select(entry => entry.Kind)
        .ShouldBe([ContractActivityKind.InternalNote, ContractActivityKind.FilerReply]);
      admin.Item.Activity[1].AuthorPrincipalId.ShouldBe(world.Filer.Value);
    }

    public static async Task UnsignedWrites_Should_Return401()
    {
      TriageWorld world = new();
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      Guid id = item.Id.Value;
      (await world.ChangeStatus(null).Handle(new ChangeStatusContract.Command { FeedbackItemId = id, Status = ContractStatus.Planned }, CancellationToken.None))
        .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(401);
      (await world.Reply(null).Handle(new ReplyContract.Command { FeedbackItemId = id, Body = "x" }, CancellationToken.None))
        .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(401);
      (await world.Note(null).Handle(new AddNoteContract.Command { FeedbackItemId = id, Body = "x" }, CancellationToken.None))
        .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(401);
      (await world.FilerReply(null).Handle(new FilerReplyContract.Command { FeedbackItemId = id, Body = "x" }, CancellationToken.None))
        .Match(_ => 0, problem => problem.Status ?? 0).ShouldBe(401);
      item.Activity.ShouldBeEmpty();
    }

    public static Task MockFactories_Should_ProduceDeterministicSamples()
    {
      AdminListContract.GetMockResponseFactory()(new AdminListContract.Query()).Items.Length.ShouldBe(2);
      Guid id = Guid.Parse("66666666-6666-6666-6666-666666666666");
      AdminGetContract.GetMockResponseFactory()(new AdminGetContract.Query { FeedbackItemId = id }).Item.FeedbackItemId.ShouldBe(id);
      ChangeStatusContract.GetMockResponseFactory()(new ChangeStatusContract.Command { FeedbackItemId = id, Status = ContractStatus.Planned })
        .Item.Status.ShouldBe(ContractStatus.Planned);
      FilerGetContract.GetMockResponseFactory()(new FilerGetContract.Query { FeedbackItemId = id })
        .Activity.ShouldNotContain(entry => entry.Kind == ContractActivityKind.InternalNote);
      return Task.CompletedTask;
    }

    public static Task AdminView_Should_RoundTripAsPascalCaseEnumStrings()
    {
      FeedbackAdminView view = FeedbackAdminView.Sample(Guid.Parse("11111111-1111-1111-1111-111111111111"), ContractStatus.Planned);
      string json = JsonSerializer.Serialize(new AdminGetContract.Response(view), ContractSerializationDefaults.Options);
      json.ShouldContain("\"status\":\"Planned\"");
      json.ShouldContain("\"kind\":\"InternalNote\"");
      AdminGetContract.Response parsed = JsonSerializer
        .Deserialize<AdminGetContract.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();
      parsed.Item.Status.ShouldBe(ContractStatus.Planned);
      parsed.Item.Activity.Count.ShouldBe(3);
      parsed.Item.Activity[0].ToStatus.ShouldBe(ContractStatus.Planned);
      return Task.CompletedTask;
    }
  }

  [TestTag("Handler")]
  public class FeedbackOwnerName_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackOwnerName_Given_>();

    public static async Task List_WithProfilesRead_Should_NameOwnersInOneBatchPerPage()
    {
      TriageWorld world = new();
      var ada = Guid.NewGuid();
      var grace = Guid.NewGuid();
      var noProfile = Guid.NewGuid();
      await world.AddProfileAsync(ada, "Ada Lovelace");
      await world.AddProfileAsync(grace, "Grace Hopper");
      await world.AddAsync(ada, day: 1);
      await world.AddAsync(ada, day: 2);
      await world.AddAsync(grace, day: 3);
      await world.AddAsync(noProfile, day: 4);

      AdminListContract.Response response = await world.ListAsync(new AdminListContract.Query());

      response.Items.Select(item => item.OwnerDisplayName)
        .ShouldBe([null, "Grace Hopper", "Ada Lovelace", "Ada Lovelace"]);
      world.Directory.Batches.Count.ShouldBe(1, "one batch per page, never one call per row");
      world.Directory.Batches[0].Order().ShouldBe(new[] { ada, grace, noProfile }.Order(), "distinct owners only");
    }

    public static async Task List_WithoutProfilesRead_Should_ShowIdsOnlyAndNeverAskTheDirectory()
    {
      TriageWorld world = new(grantProfilesRead: false);
      var ada = Guid.NewGuid();
      await world.AddProfileAsync(ada, "Ada Lovelace");
      await world.AddAsync(ada, day: 1);

      AdminListContract.Response response = await world.ListAsync(new AdminListContract.Query());

      response.Items.Single().OwnerPrincipalId.ShouldBe(ada);
      response.Items.Single().OwnerDisplayName.ShouldBeNull();
      world.Directory.Batches.ShouldBeEmpty();
    }

    public static async Task EmptyPage_Should_NotAskTheDirectory()
    {
      TriageWorld world = new();
      (await world.ListAsync(new AdminListContract.Query())).Items.ShouldBeEmpty();
      world.Directory.Batches.ShouldBeEmpty();
    }

    public static async Task AdminView_Should_NameTheFilerOnlyWithProfilesRead()
    {
      TriageWorld world = new();
      await world.AddProfileAsync(world.Filer.Value, "Ada Lovelace");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      FeedbackItem orphan = await world.AddAsync(Guid.NewGuid(), day: 2);

      AdminGetContract.Response named = (await world.AdminGet()
          .Handle(new AdminGetContract.Query { FeedbackItemId = item.Id.Value }, CancellationToken.None)).AsT0;
      named.Item.OwnerDisplayName.ShouldBe("Ada Lovelace");
      world.Directory.Batches.Count.ShouldBe(1);

      AdminGetContract.Response missing = (await world.AdminGet()
          .Handle(new AdminGetContract.Query { FeedbackItemId = orphan.Id.Value }, CancellationToken.None)).AsT0;
      missing.Item.OwnerDisplayName.ShouldBeNull("no profile row");

      // A mutation returns the same view, so the detail page keeps the name after a write.
      (await world.NoteAsync(item.Id.Value, "Checked")).Item.OwnerDisplayName.ShouldBe("Ada Lovelace");

      TriageWorld gated = new(grantProfilesRead: false);
      await gated.AddProfileAsync(gated.Filer.Value, "Ada Lovelace");
      FeedbackItem hidden = await gated.AddAsync(gated.Filer.Value, day: 1);
      AdminGetContract.Response noPii = (await gated.AdminGet()
          .Handle(new AdminGetContract.Query { FeedbackItemId = hidden.Id.Value }, CancellationToken.None)).AsT0;
      noPii.Item.OwnerDisplayName.ShouldBeNull();
      noPii.Item.OwnerPrincipalId.ShouldBe(gated.Filer.Value);
      gated.Directory.Batches.ShouldBeEmpty();
    }

    public static Task Contracts_Should_CarryOwnerDisplayNameThroughJson()
    {
      AdminListContract.Item row = new(
        Guid.NewGuid(), Guid.NewGuid(), ContractKind.BugReport, ContractStatus.New, "Title", DateTimeOffset.UnixEpoch, "Ada");
      string json = JsonSerializer.Serialize(new AdminListContract.Response(1, [row], 1, 25), ContractSerializationDefaults.Options);
      JsonSerializer.Deserialize<AdminListContract.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull().Items[0].OwnerDisplayName.ShouldBe("Ada");

      FeedbackAdminView view = FeedbackAdminView.Sample(Guid.NewGuid());
      string viewJson = JsonSerializer.Serialize(view, ContractSerializationDefaults.Options);
      JsonSerializer.Deserialize<FeedbackAdminView>(viewJson, ContractSerializationDefaults.Options)
        .ShouldNotBeNull().OwnerDisplayName.ShouldBe("Ada Lovelace");
      return Task.CompletedTask;
    }
  }

  [TestTag("Authorization")]
  public class FeedbackAdminAuthorization_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackAdminAuthorization_Given_>();

    public static Task Contracts_Should_RequireTheReadOrManagePermission()
    {
      PolicyOf(typeof(AdminListContract)).ShouldBe(PermissionIds.AdminFeedbackRead);
      PolicyOf(typeof(AdminGetContract)).ShouldBe(PermissionIds.AdminFeedbackRead);
      PolicyOf(typeof(ChangeStatusContract)).ShouldBe(PermissionIds.AdminFeedbackManage);
      PolicyOf(typeof(ReplyContract)).ShouldBe(PermissionIds.AdminFeedbackManage);
      PolicyOf(typeof(AddNoteContract)).ShouldBe(PermissionIds.AdminFeedbackManage);
      PolicyOf(typeof(FilerReplyContract)).ShouldBe(PermissionIds.FeedbackFileSelf);
      return Task.CompletedTask;
    }

    public static Task Seed_Should_GrantFeedbackAdminToAdministratorOnly()
    {
      foreach (string permission in new[] { PermissionIds.AdminFeedbackRead, PermissionIds.AdminFeedbackManage })
      {
        PermissionIds.All.ShouldContain(permission);
        RolePermissionSeed.AdminPermissions.ShouldContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Administrator].ShouldContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Member].ShouldNotContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Developer].ShouldNotContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Operator].ShouldNotContain(permission);
      }

      return Task.CompletedTask;
    }

    // By name: the attribute's namespace is a platform literal the template rewrites.
    private static string? PolicyOf(Type contract)
    {
      Attribute attribute = contract.GetCustomAttributes()
        .Single(candidate => candidate.GetType().Name == "EndpointAuthorizeAttribute");
      return (string?)attribute.GetType().GetProperty("Policy")!.GetValue(attribute);
    }
  }

  internal sealed class TriageWorld
  {
    public InMemoryFeedbackStore Feedback { get; } = new();
    public InMemoryFeedbackAttachmentStore Attachments { get; } = new();
    public InMemoryProfileStore Profiles { get; } = new();
    public ProfileEmailLookup Lookup { get; }
    public TriageCountingDirectory Directory { get; }
    public TriageMailCapture Mail { get; } = new();
    public PrincipalId Filer { get; } = PrincipalId.New();
    public PrincipalId Admin { get; } = PrincipalId.New();
    private readonly IEmailSender Sender;
    private readonly TriageBaseUrl BaseUrl = new(new Uri("https://app.example"));
    private readonly PermissionEvaluator Evaluator;

    /// <summary>Admin is an Administrator; grantProfilesRead false strips admin.profiles.read from that role.</summary>
    public TriageWorld(IEmailSender? sender = null, bool grantProfilesRead = true)
    {
      Lookup = new ProfileEmailLookup(Profiles);
      Directory = new TriageCountingDirectory(new ProfileDirectory(Profiles));
      Sender = sender ?? Mail;
      InMemoryPrincipalRoleStore roles = new();
      roles.SetRoleIdsAsync(Admin, [RoleIds.Administrator]).GetAwaiter().GetResult();
      InMemoryRolePermissionStore permissions = new();
      if (!grantProfilesRead)
      {
        permissions.SetPermissionIdsForRoleAsync(
          RoleIds.Administrator,
          [.. RolePermissionSeed.DefaultGrants[RoleIds.Administrator].Where(static id => id != PermissionIds.AdminProfilesRead)])
          .GetAwaiter().GetResult();
      }

      EffectiveRolesResolver resolver = new(
        roles,
        Options.Create(new BootstrapAdministratorOptions { BootstrapAdministratorPrincipalIds = [] }));
      Evaluator = new PermissionEvaluator(resolver, permissions, new TriageNoAgentScopes());
    }

    public static DateTimeOffset Day(int day) => new(2026, 10, day, 0, 0, 0, TimeSpan.Zero);

    public async Task<FeedbackItem> AddAsync(Guid owner, int day, DomainKind kind = DomainKind.BugReport)
    {
      FeedbackItem item = FeedbackItemTriage_Given_.Filed(owner, kind, Day(day));
      await Feedback.AddAsync(item);
      return item;
    }

    public async Task<FeedbackItem> AddTitledAsync(
      string title,
      DomainKind kind,
      int day,
      DomainStatus status = DomainStatus.New)
    {
      FeedbackItem item = FeedbackItem.File(Guid.NewGuid(), kind, title, "Body", Day(day));
      if (status != DomainStatus.New)
      {
        item.ChangeStatus(status, Admin.Value, Day(day));
      }

      await Feedback.AddAsync(item);
      return item;
    }

    public async Task<Guid[]> SortedIdsAsync(FeedbackListSortColumn sortBy, bool descending) =>
      [
        .. (await ListAsync(new AdminListContract.Query { IncludeClosed = true, SortBy = sortBy, SortDescending = descending }))
          .Items.Select(item => item.FeedbackItemId)
      ];

    public async Task AddProfileAsync(Guid principal, string displayName)
    {
      Profile profile = Profile.Create(ProfileId.From(principal), displayName, "en-US", "US", "dark");
      await Profiles.AddAsync(profile);
    }

    public FeedbackOwnerNames OwnerNames(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Evaluator, Directory);

    public async Task SetFilerEmailAsync(string email)
    {
      Profile profile = Profile.Create(ProfileId.From(Filer.Value), "Ada Lovelace", "en-US", "US", "dark");
      profile.SetEmail(email);
      await Profiles.AddAsync(profile);
    }

    public AdminListHandler AdminList() => new(Feedback, OwnerNames(Admin));

    public AdminGetHandler AdminGet() => new(Feedback, Attachments, Lookup, OwnerNames(Admin));

    public ChangeStatusHandler ChangeStatus(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Feedback, Attachments, Lookup, OwnerNames(principal), Sender, BaseUrl,
        NullLogger<ChangeStatusHandler>.Instance);

    public ReplyHandler Reply(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Feedback, Attachments, Lookup, OwnerNames(principal), Sender, BaseUrl,
        NullLogger<ReplyHandler>.Instance);

    public ReplyHandler Reply() => Reply(Admin);

    public AddNoteHandler Note(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Feedback, Attachments, Lookup, OwnerNames(principal));

    public AddNoteHandler Note() => Note(Admin);

    public FilerReplyHandler FilerReply(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Feedback);

    public async Task<AdminListContract.Response> ListAsync(AdminListContract.Query query) =>
      (await AdminList().Handle(query, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected list success"));

    public async Task<ChangeStatusContract.Response> ChangeStatusAsync(Guid id, ContractStatus status) =>
      (await ChangeStatus(Admin).Handle(new ChangeStatusContract.Command { FeedbackItemId = id, Status = status }, CancellationToken.None))
        .Match(ok => ok, problem => throw new InvalidOperationException($"Expected status success: {problem.Detail}"));

    public async Task<SharedProblemDetails> ChangeStatusProblemAsync(Guid id, ContractStatus status) =>
      (await ChangeStatus(Admin).Handle(new ChangeStatusContract.Command { FeedbackItemId = id, Status = status }, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected status problem"), problem => problem);

    public async Task<AddNoteContract.Response> NoteAsync(Guid id, string body) =>
      (await Note().Handle(new AddNoteContract.Command { FeedbackItemId = id, Body = body }, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected note success"));

    public async Task<FilerGetContract.Response> FilerGetAsync(Guid id) =>
      (await new FilerGetHandler(new TriagePrincipalAccessor(Filer), Feedback, Attachments)
          .Handle(new FilerGetContract.Query { FeedbackItemId = id }, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected filer get success"));

    public async Task<FilerListContract.Response> FilerListAsync() =>
      (await new FilerListHandler(new TriagePrincipalAccessor(Filer), Feedback, Lookup)
          .Handle(new FilerListContract.Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected filer list success"));
  }

  internal sealed class TriagePrincipalAccessor : ICurrentPrincipalAccessor
  {
    private readonly PrincipalId? PrincipalId;

    public TriagePrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

    public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
      Task.FromResult(PrincipalId);
  }

  /// <summary>The real ProfileDirectory, counting batch calls so a test can prove one per page.</summary>
  internal sealed class TriageCountingDirectory : IProfileDirectory
  {
    private readonly IProfileDirectory Inner;

    public TriageCountingDirectory(IProfileDirectory inner) => Inner = inner;

    public List<Guid[]> Batches { get; } = [];

    public Task<IReadOnlyDictionary<Guid, ProfileDirectoryEntry>> FindManyAsync(
      IReadOnlyCollection<Guid> principalIds,
      CancellationToken cancellationToken = default)
    {
      Batches.Add([.. principalIds]);
      return Inner.FindManyAsync(principalIds, cancellationToken);
    }

    public Task<IReadOnlyList<Guid>> SearchAsync(string term, CancellationToken cancellationToken = default) =>
      Inner.SearchAsync(term, cancellationToken);
  }

  internal sealed class TriageNoAgentScopes : IAgentPermissionScopeSource
  {
    public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) => null;
  }

  internal sealed class TriageMailCapture : IEmailSender
  {
    public List<EmailMessage> Messages { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Messages.Add(message);
      return Task.CompletedTask;
    }
  }

  internal sealed class TriageBrokenMail : IEmailSender
  {
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
      throw new InvalidOperationException("Mail transport is down");
  }

  internal sealed class TriageBaseUrl : IAppBaseUrlAccessor
  {
    private readonly Uri? BaseUrl;

    public TriageBaseUrl(Uri? baseUrl) => BaseUrl = baseUrl;

    public Uri? GetBaseUrl() => BaseUrl;
  }

} // namespace TimeWarp.Architecture.Features.Feedback
