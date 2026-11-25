#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008
#:include feedback-triage-world-tests.cs
#:include feedback-postgres-stores-tests.cs

// Co-located Jaribu: who filed and where from on the admin feedback views: the owner display name
// (task 336) and the app version and page path recorded at filing (task 521).
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-owner-name-tests.cs

#region Purpose
// Jaribu runfile for task 336: the owner display name on list rows and the admin view (set only
// with admin.profiles.read, null for a missing profile, one directory batch per page).
// Task 521 (FeedbackFilingContext_Given_): a filing stores the server's version (never the
// client's) and the submitted page path; a filing without a path still succeeds; the validator
// refuses a path with a query, a fragment, an absolute or protocol-relative URL, whitespace, or
// over the limit; the contract rule matches the domain copy; and the admin list, detail and
// mutation views return both.
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
  using Microsoft.Extensions.Logging.Abstractions;
  using Shouldly;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Versioning;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using AdminGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedbackForAdmin;
  using AdminListContract = TimeWarp.Architecture.Features.Feedback.ListFeedback;
  using ChangeStatusContract = TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
  using ContractActivityKind = TimeWarp.Architecture.Features.Feedback.FeedbackActivityKind;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using ContractSeverity = TimeWarp.Architecture.Features.Feedback.FeedbackSeverity;
  using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
  using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;
  using FilerGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;
  using SetSeverityContract = TimeWarp.Architecture.Features.Feedback.SetFeedbackSeverity;
  using SubmitContract = TimeWarp.Architecture.Features.Feedback.SubmitFeedback;
  using SubmitHandler = TimeWarp.Architecture.Features.Feedback.Application.SubmitFeedback.Handler;

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

    public static async Task AgentToken_WithAdministratorRole_Should_NotReadOwnerNames()
    {
      TriageWorld world = new();
      await world.AddProfileAsync(world.Filer.Value, "Ada Lovelace");
      PrincipalId agent = PrincipalId.New();
      await world.AssignAdministratorAsync(agent);
      world.HoldAgentScopes(agent, AgentScopes.FeedbackRead);

      (await world.OwnerNames(agent).FindAsync(world.Filer.Value, CancellationToken.None)).ShouldBeNull();
      world.Directory.Batches.ShouldBeEmpty();

      // The same principal, evaluated as a human session, holds admin.profiles.read through
      // the Administrator role. The agent-token path above must not have used that grant.
      world.ReleaseAgentScopes(agent);
      (await world.OwnerNames(agent).FindAsync(world.Filer.Value, CancellationToken.None))
        .ShouldBe("Ada Lovelace");
      world.Directory.Batches.Count.ShouldBe(1);
    }

    public static async Task List_Should_TreatOmittedSeverityAsEveryItemAndUnsetAsUnreviewed()
    {
      TriageWorld world = new();
      FeedbackItem unreviewed = await world.AddAsync(world.Filer.Value, day: 1);
      FeedbackItem reviewed = await world.AddAsync(Guid.NewGuid(), day: 2);
      (await world.SetSeverityAsync(reviewed.Id.Value, ContractSeverity.Low)).Item.Status.ShouldBe(ContractStatus.New);

      AdminListContract.Response all = await world.ListAsync(new AdminListContract.Query { IncludeClosed = true });
      all.Items.Select(item => item.FeedbackItemId).ShouldBe([reviewed.Id.Value, unreviewed.Id.Value]);
      all.Items.Single(item => item.FeedbackItemId == reviewed.Id.Value).Severity.ShouldBe(ContractSeverity.Low);
      all.Items.Single(item => item.FeedbackItemId == unreviewed.Id.Value).Severity.ShouldBe(ContractSeverity.Unset);

      AdminListContract.Response queue = await world.ListAsync(
        new AdminListContract.Query { IncludeClosed = true, Severity = ContractSeverity.Unset });
      queue.Items.Select(item => item.FeedbackItemId).ShouldBe([unreviewed.Id.Value]);

      new AdminListContract.Query { Severity = ContractSeverity.Unset }.GetRouteWithQueryString()
        .ShouldContain("Severity=Unset");
      new AdminListContract.Query().GetRouteWithQueryString()
        .Contains("Severity", StringComparison.Ordinal).ShouldBeFalse();
    }

    public static async Task SetSeverity_Should_LeaveStatusAndSendNoMail_AndCompletedStillEmails()
    {
      TriageWorld world = new();
      await world.SetFilerEmailAsync("ada@example.com");
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);

      SetSeverityContract.Response set = await world.SetSeverityAsync(item.Id.Value, ContractSeverity.Medium);
      set.Item.Severity.ShouldBe(ContractSeverity.Medium);
      set.Item.Status.ShouldBe(ContractStatus.New);
      set.Item.Activity.ShouldContain(entry =>
        entry.Kind == ContractActivityKind.SeverityChange
        && entry.FromSeverity == ContractSeverity.Unset
        && entry.ToSeverity == ContractSeverity.Medium
        && entry.Body == null
        && entry.FromStatus == null);
      world.Mail.Messages.ShouldBeEmpty();

      (await world.SetSeverityProblemAsync(item.Id.Value, ContractSeverity.Medium)).Status.ShouldBe(409);
      (await world.SetSeverityProblemAsync(item.Id.Value, ContractSeverity.Unset)).Status.ShouldBe(400);
      world.Mail.Messages.ShouldBeEmpty();

      ChangeStatusContract.Response completed = await world.ChangeStatusAsync(item.Id.Value, ContractStatus.Completed);
      completed.FilerEmailed.ShouldBeTrue();
      completed.Item.Severity.ShouldBe(ContractSeverity.Medium);
      completed.Item.Status.ShouldBe(ContractStatus.Completed);
      world.Mail.Messages.Count.ShouldBe(1);
      world.Mail.Messages[0].To.ShouldContain("ada@example.com");
    }

    public static async Task FilerJson_Should_OmitSeverityOnGetAndListMine()
    {
      TriageWorld world = new();
      FeedbackItem item = await world.AddAsync(world.Filer.Value, day: 1);
      await world.SetSeverityAsync(item.Id.Value, ContractSeverity.High);
      await world.ChangeStatusAsync(item.Id.Value, ContractStatus.UnderReview);

      FilerGetContract.Response get = await world.FilerGetAsync(item.Id.Value);
      get.Activity.ShouldNotContain(entry => entry.Kind == ContractActivityKind.SeverityChange);
      get.Activity.ShouldContain(entry => entry.Kind == ContractActivityKind.StatusChange);
      string getJson = JsonSerializer.Serialize(get, ContractSerializationDefaults.Options);
      getJson.Contains("severity", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();

      string listJson = JsonSerializer.Serialize(await world.FilerListAsync(), ContractSerializationDefaults.Options);
      listJson.Contains("severity", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    public static Task Contracts_Should_CarryOwnerDisplayNameThroughJson()
    {
      AdminListContract.Item row = new(
        Guid.NewGuid(), Guid.NewGuid(), ContractKind.BugReport, ContractStatus.New, "Title", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "Ada");
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

  [TestTag("Handler")]
  public class FeedbackFilingContext_Given_
  {
    private const string ServerVersion = "9.8.7-test+0123abc";

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackFilingContext_Given_>();

    public static async Task Submit_Should_StoreTheServerVersionAndThePagePath()
    {
      TriageWorld world = new();
      SubmitContract.Command command = Complaint("Counter stuck");
      command.PagePath = "/Counter";
      SubmitContract.Response response = await SubmitAsync(world, command, ServerVersion);

      FeedbackItem stored = (await world.Feedback.FindAsync(FeedbackItemId.From(response.FeedbackItemId))).ShouldNotBeNull();
      stored.AppVersion.ShouldBe(ServerVersion);
      stored.PagePath.ShouldBe("/Counter");
    }

    public static async Task SubmitWithoutPagePath_Should_FileWithNoPath()
    {
      TriageWorld world = new();
      SubmitContract.Response withNull = await SubmitAsync(world, Complaint("No page"), ServerVersion);
      SubmitContract.Command empty = Complaint("Empty page");
      empty.PagePath = "";
      SubmitContract.Response withEmpty = await SubmitAsync(world, empty, ServerVersion);

      foreach (Guid id in new[] { withNull.FeedbackItemId, withEmpty.FeedbackItemId })
      {
        FeedbackItem stored = (await world.Feedback.FindAsync(FeedbackItemId.From(id))).ShouldNotBeNull();
        stored.PagePath.ShouldBeNull();
        stored.AppVersion.ShouldBe(ServerVersion);
      }
    }

    public static async Task SubmitWithAnOverlongServerVersion_Should_StillFile()
    {
      TriageWorld world = new();
      string longVersion = "1.0.0+" + new string('a', FeedbackItem.MaxAppVersionLength);
      SubmitContract.Response response = await SubmitAsync(world, Complaint("Long version"), longVersion);

      FeedbackItem stored = (await world.Feedback.FindAsync(FeedbackItemId.From(response.FeedbackItemId))).ShouldNotBeNull();
      stored.AppVersion.ShouldBe(longVersion[..FeedbackItem.MaxAppVersionLength]);
    }

    public static async Task AppVersionAndPagePath_Should_ReachTheAdminListAndDetail()
    {
      TriageWorld world = new();
      SubmitContract.Command command = Complaint("Counter is stuck");
      command.PagePath = "/Counter";
      Guid recordedId = (await SubmitAsync(world, command, ServerVersion)).FeedbackItemId;
      FeedbackItem older = await world.AddAsync(world.Filer.Value, day: 1);

      AdminListContract.Response list = await world.ListAsync(new AdminListContract.Query());
      AdminListContract.Item listed = list.Items.Single(item => item.FeedbackItemId == recordedId);
      listed.AppVersion.ShouldBe(ServerVersion);
      listed.PagePath.ShouldBe("/Counter");
      AdminListContract.Item legacy = list.Items.Single(item => item.FeedbackItemId == older.Id.Value);
      legacy.AppVersion.ShouldBeNull();
      legacy.PagePath.ShouldBeNull();

      AdminGetContract.Response detail = (await world.AdminGet().Handle(
          new AdminGetContract.Query { FeedbackItemId = recordedId }, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected admin get success"));
      detail.Item.AppVersion.ShouldBe(ServerVersion);
      detail.Item.PagePath.ShouldBe("/Counter");

      // A status change returns the same view, and the filing fields stay as filed.
      ChangeStatusContract.Response changed = await world.ChangeStatusAsync(recordedId, ContractStatus.UnderReview);
      changed.Item.AppVersion.ShouldBe(ServerVersion);
      changed.Item.PagePath.ShouldBe("/Counter");
      FeedbackItem stored = (await world.Feedback.FindAsync(FeedbackItemId.From(recordedId))).ShouldNotBeNull();
      stored.AppVersion.ShouldBe(ServerVersion);
      stored.PagePath.ShouldBe("/Counter");
    }

    public static Task AdminView_Should_RoundTripVersionAndPath()
    {
      FeedbackAdminView view = FeedbackAdminView.Sample(Guid.Parse("11111111-1111-1111-1111-111111111111"));
      string json = JsonSerializer.Serialize(new AdminGetContract.Response(view), ContractSerializationDefaults.Options);
      json.ShouldContain("\"pagePath\":\"/Counter\"");
      AdminGetContract.Response parsed = JsonSerializer
        .Deserialize<AdminGetContract.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();
      parsed.Item.AppVersion.ShouldBe(view.AppVersion);
      parsed.Item.PagePath.ShouldBe(view.PagePath);
      return Task.CompletedTask;
    }

    public static Task Validator_Should_AcceptAnAppPathOrNone()
    {
      foreach (string? path in new[] { null, "", "/", "/Counter", "/Feedback/11111111-1111-1111-1111-111111111111", "/a%20b" })
      {
        SubmitContract.Command command = Complaint("Path");
        command.PagePath = path;
        new SubmitContract.Validator().Validate(command).IsValid.ShouldBeTrue($"'{path}' is an app path or absent");
      }

      return Task.CompletedTask;
    }

    public static Task Validator_Should_RefuseAPathThatIsNotAnAppPath()
    {
      string[] refused =
      [
        "/Counter?token=secret",
        "/Counter#section",
        "https://evil.example/Counter",
        "//evil.example/Counter",
        "Counter",
        "/Count er",
        " ",
        "/a\\b",
        "/" + new string('a', SubmitContract.MaxPagePathLength),
      ];
      foreach (string path in refused)
      {
        SubmitContract.Command command = Complaint("Path");
        command.PagePath = path;
        ValidationResult result = new SubmitContract.Validator().Validate(command);
        result.IsValid.ShouldBeFalse($"'{path}' must be refused");
        result.Errors.ShouldContain(error => error.PropertyName == nameof(SubmitContract.Command.PagePath));
      }

      return Task.CompletedTask;
    }

    public static Task PagePathRule_Should_MatchTheDomainCopy()
    {
      SubmitContract.MaxPagePathLength.ShouldBe(FeedbackItem.MaxPagePathLength);
      string[] samples = ["/", "/Counter", "/a/b/c", "/Counter?x=1", "/Counter#y", "//host/x", "https://host/x", "x", "/a b", "/a\tb", "/a\\b"];
      foreach (string sample in samples)
      {
        SubmitContract.IsPagePath(sample).ShouldBe(FeedbackItem.IsAppRelativePath(sample), sample);
      }

      Should.Throw<ArgumentException>(() => FeedbackItem.File(
        Guid.NewGuid(), DomainKind.Other, "Title", "Body", DateTimeOffset.UtcNow, pagePath: "/Counter?token=secret"));
      Should.Throw<ArgumentOutOfRangeException>(() => FeedbackItem.File(
        Guid.NewGuid(), DomainKind.Other, "Title", "Body", DateTimeOffset.UtcNow,
        pagePath: "/" + new string('a', FeedbackItem.MaxPagePathLength)));
      FeedbackItem blank = FeedbackItem.File(
        Guid.NewGuid(), DomainKind.Other, "Title", "Body", DateTimeOffset.UtcNow, appVersion: "  ", pagePath: " ");
      blank.AppVersion.ShouldBeNull();
      blank.PagePath.ShouldBeNull();
      return Task.CompletedTask;
    }

    private static SubmitContract.Command Complaint(string title) => new()
    {
      Kind = ContractKind.Complaint,
      Title = title,
      Body = $"Body for {title}",
    };

    private static async Task<SubmitContract.Response> SubmitAsync(
      TriageWorld world,
      SubmitContract.Command command,
      string? serverVersion)
    {
      SubmitHandler handler = new(
        new TriagePrincipalAccessor(world.Filer),
        world.Feedback,
        world.Lookup,
        world.Mail,
        new TriageBaseUrl(null),
        NullLogger<SubmitHandler>.Instance,
        world.Attachments,
        new FixedVersion(serverVersion));
      return (await handler.Handle(command, CancellationToken.None))
        .Match(ok => ok, problem => throw new InvalidOperationException($"Expected submit success: {problem.Detail}"));
    }

    private sealed class FixedVersion : IAppVersionAccessor
    {
      private readonly string? Version;

      public FixedVersion(string? version) => Version = version;

      public string? GetVersion() => Version;
    }
  }

} // namespace TimeWarp.Architecture.Features.Feedback
