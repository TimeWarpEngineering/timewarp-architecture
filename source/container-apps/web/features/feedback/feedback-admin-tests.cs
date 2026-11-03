#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008
#:include feedback-triage-world-tests.cs

// Co-located Jaribu: admin feedback list, sort, paging, detail, status changes, replies, and notes.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-admin-tests.cs

#region Purpose
// Jaribu runfile for tasks 314 and 327: the admin list (cross-owner, closed hidden by default,
// filters, paging, and the sort: each column both ways, before paging, id tiebreaker), the admin
// detail, status changes (never back to New, 409 on the current status, email only for
// Planned/Completed/Declined), admin replies (always emailed when an address is known),
// internal notes (never shown to the filer, never emailed), filer replies (own items only),
// and contract validators.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{

  using System.Text.Json;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using AddNoteContract = TimeWarp.Architecture.Features.Feedback.AddFeedbackNote;
  using AdminGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedbackForAdmin;
  using AdminListContract = TimeWarp.Architecture.Features.Feedback.ListFeedback;
  using ChangeStatusContract = TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
  using ContractActivityKind = TimeWarp.Architecture.Features.Feedback.FeedbackActivityKind;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
  using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;
  using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;
  using FilerGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;
  using FilerListContract = TimeWarp.Architecture.Features.Feedback.ListMyFeedback;
  using FilerReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyAsFiler;
  using ReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyToFeedback;

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

    public static async Task List_Should_SearchTitlesAcrossEveryOwner()
    {
      TriageWorld world = new();
      FeedbackItem printer = await world.AddTitledAsync("Printer jams", DomainKind.BugReport, day: 1);
      FeedbackItem printerToo = await world.AddTitledAsync("Old PRINTER driver", DomainKind.Complaint, day: 2);
      await world.AddTitledAsync("Login is slow", DomainKind.Complaint, day: 3);

      AdminListContract.Response found = await world.ListAsync(new AdminListContract.Query { Search = " printer " });
      found.TotalCount.ShouldBe(2);
      found.Items.Select(item => item.FeedbackItemId).ShouldBe([printerToo.Id.Value, printer.Id.Value]);
      (await world.ListAsync(new AdminListContract.Query { Search = "" })).TotalCount.ShouldBe(3);
      new AdminListContract.Query { Search = " jam " }.GetRouteWithQueryString().ShouldContain("Search=jam");
      new AdminListContract.Validator()
        .Validate(new AdminListContract.Query { Search = new string('x', AdminListContract.MaxSearchLength + 1) })
        .IsValid.ShouldBeFalse();
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

} // namespace TimeWarp.Architecture.Features.Feedback
