#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008
#:include feedback-triage-world-tests.cs

// Co-located Jaribu: admin feedback triage (status, replies, internal notes) and the filer's thread.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs

#region Purpose
// Jaribu runfile for task 314: the FeedbackItem status and thread mutations and their
// invariants, plus the authorization surface: each contract's [EndpointAuthorize] policy and the
// Administrator-only seed. HTTP 401/403 and no-PII proofs live in
// web-server-integration-tests (admin-feedback-authorization-*-tests).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Feedback
{

  using System.Reflection;
  using Shouldly;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using AddNoteContract = TimeWarp.Architecture.Features.Feedback.AddFeedbackNote;
  using AdminGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedbackForAdmin;
  using AdminListContract = TimeWarp.Architecture.Features.Feedback.ListFeedback;
  using ChangeStatusContract = TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
  using ContractActivityKind = TimeWarp.Architecture.Features.Feedback.FeedbackActivityKind;
  using ContractSeverity = TimeWarp.Architecture.Features.Feedback.FeedbackSeverity;
  using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
  using DomainActivityKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackActivityKind;
  using DomainSeverity = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackSeverity;
  using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;
  using FilerReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyAsFiler;
  using ReplyContract = TimeWarp.Architecture.Features.Feedback.ReplyToFeedback;
  using SetSeverityContract = TimeWarp.Architecture.Features.Feedback.SetFeedbackSeverity;

  [TestTag("Domain")]
  public class FeedbackItemTriage_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackItemTriage_Given_>();

    private static readonly DateTimeOffset At = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    public static Task File_Should_StartNewWithAnEmptyThread()
    {
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
      item.Status.ShouldBe(DomainStatus.New);
      item.Severity.ShouldBe(DomainSeverity.Unset);
      item.Activity.ShouldBeEmpty();
      return Task.CompletedTask;
    }

    public static Task ChangeStatus_Should_RecordFromToWhoAndWhen()
    {
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
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
      entry.FromSeverity.ShouldBeNull();
      entry.ToSeverity.ShouldBeNull();
      item.Severity.ShouldBe(DomainSeverity.Unset);
      return Task.CompletedTask;
    }

    public static Task SetSeverity_Should_RecordFromToWhoAndWhenWithoutMovingStatus()
    {
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
      var reviewer = Guid.NewGuid();
      FeedbackActivity entry = item.SetSeverity(DomainSeverity.High, reviewer, At);
      item.Status.ShouldBe(DomainStatus.New);
      item.Severity.ShouldBe(DomainSeverity.High);
      entry.Kind.ShouldBe(DomainActivityKind.SeverityChange);
      entry.FromSeverity.ShouldBe(DomainSeverity.Unset);
      entry.ToSeverity.ShouldBe(DomainSeverity.High);
      entry.FromStatus.ShouldBeNull();
      entry.ToStatus.ShouldBeNull();
      entry.Body.ShouldBeNull();
      entry.AuthorPrincipalId.ShouldBe(reviewer);
      entry.At.ShouldBe(At);
      entry.Sequence.ShouldBe(1);
      entry.IsVisibleToFiler.ShouldBeFalse();

      item.ChangeStatus(DomainStatus.Planned, reviewer, At.AddMinutes(1));
      item.Severity.ShouldBe(DomainSeverity.High);
      item.SetSeverity(DomainSeverity.Critical, reviewer, At.AddMinutes(2));
      item.Status.ShouldBe(DomainStatus.Planned);
      item.Severity.ShouldBe(DomainSeverity.Critical);
      item.Activity.Count.ShouldBe(3);
      item.Activity.Count(candidate => candidate.Kind == DomainActivityKind.SeverityChange).ShouldBe(2);
      return Task.CompletedTask;
    }

    public static Task SetSeverity_Should_RefuseUnsetAndTheCurrentValue()
    {
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
      var reviewer = Guid.NewGuid();
      Should.Throw<InvalidOperationException>(() => item.SetSeverity(DomainSeverity.Unset, reviewer, At));
      item.SetSeverity(DomainSeverity.Low, reviewer, At);
      Should.Throw<InvalidOperationException>(() => item.SetSeverity(DomainSeverity.Low, reviewer, At));
      Should.Throw<InvalidOperationException>(() => item.SetSeverity(DomainSeverity.Unset, reviewer, At));
      Should.Throw<ArgumentException>(() => item.SetSeverity((DomainSeverity)99, reviewer, At));
      Should.Throw<ArgumentException>(() => item.SetSeverity(DomainSeverity.Medium, Guid.Empty, At));
      item.CanSetSeverityTo(DomainSeverity.Low).ShouldBeFalse();
      item.CanSetSeverityTo(DomainSeverity.Unset).ShouldBeFalse();
      item.CanSetSeverityTo(DomainSeverity.High).ShouldBeTrue();
      item.Status.ShouldBe(DomainStatus.New);
      item.Activity.Count.ShouldBe(1);
      return Task.CompletedTask;
    }

    public static Task ChangeStatus_Should_RefuseNewAndTheCurrentStatus()
    {
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
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
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
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
      FeedbackItem item = TriageWorld.Filed(owner, At);
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
      FeedbackItem item = TriageWorld.Filed(Guid.NewGuid(), At);
      FeedbackActivity note = item.AddInternalNote(Guid.NewGuid(), "Duplicate of an older bug", At);
      note.Kind.ShouldBe(DomainActivityKind.InternalNote);
      note.IsVisibleToFiler.ShouldBeFalse();
      item.AddAdminReply(Guid.NewGuid(), "Looking", At).IsVisibleToFiler.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EnumNames_Should_MatchBetweenContractAndDomain()
    {
      Enum.GetNames<ContractStatus>().ShouldBe(Enum.GetNames<DomainStatus>());
      Enum.GetNames<ContractSeverity>().ShouldBe(Enum.GetNames<DomainSeverity>());
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
  }

  [TestTag("Authorization")]
  public class FeedbackAdminAuthorization_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackAdminAuthorization_Given_>();

    public static Task Contracts_Should_RequireTheReadOrManagePermission()
    {
      PolicyOf(typeof(AdminListContract)).ShouldBe(PermissionIds.FeedbackRead);
      PolicyOf(typeof(AdminGetContract)).ShouldBe(PermissionIds.FeedbackRead);
      PolicyOf(typeof(AddNoteContract)).ShouldBe(PermissionIds.FeedbackNote);
      PolicyOf(typeof(SetSeverityContract)).ShouldBe(PermissionIds.FeedbackSeverity);
      PolicyOf(typeof(ChangeStatusContract)).ShouldBe(PermissionIds.FeedbackStatus);
      PolicyOf(typeof(ReplyContract)).ShouldBe(PermissionIds.AdminFeedbackManage);
      PolicyOf(typeof(FilerReplyContract)).ShouldBe(PermissionIds.FeedbackFileSelf);

      foreach (Type agentContract in new[]
      {
        typeof(AdminListContract),
        typeof(AdminGetContract),
        typeof(AddNoteContract),
        typeof(SetSeverityContract),
        typeof(ChangeStatusContract),
        typeof(DownloadFeedbackAttachment),
      })
      {
        SchemesOf(agentContract).Split(',').ShouldContain(AuthenticationSchemeNames.AgentToken);
      }

      SchemesOf(typeof(ReplyContract)).Split(',').ShouldNotContain(AuthenticationSchemeNames.AgentToken);
      SchemesOf(typeof(FilerReplyContract)).Split(',').ShouldNotContain(AuthenticationSchemeNames.AgentToken);
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

      foreach (string permission in new[]
      {
        PermissionIds.FeedbackRead,
        PermissionIds.FeedbackNote,
        PermissionIds.FeedbackSeverity,
        PermissionIds.FeedbackStatus,
      })
      {
        PermissionIds.All.ShouldContain(permission);
        RolePermissionSeed.AdminPermissions.ShouldNotContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Administrator].ShouldContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Member].ShouldNotContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Developer].ShouldNotContain(permission);
        RolePermissionSeed.DefaultGrants[RoleIds.Operator].ShouldNotContain(permission);
      }

      return Task.CompletedTask;
    }

    public static Task Scopes_Should_ExpandToOneFeedbackPermissionAndNothingAdmin()
    {
      (string Scope, string Permission)[] pairs =
      [
        (AgentScopes.FeedbackRead, PermissionIds.FeedbackRead),
        (AgentScopes.FeedbackNote, PermissionIds.FeedbackNote),
        (AgentScopes.FeedbackSeverity, PermissionIds.FeedbackSeverity),
        (AgentScopes.FeedbackStatus, PermissionIds.FeedbackStatus),
      ];
      string[] writesAndReply =
      [
        PermissionIds.FeedbackNote,
        PermissionIds.FeedbackSeverity,
        PermissionIds.FeedbackStatus,
        PermissionIds.AdminFeedbackManage,
      ];

      foreach ((string scope, string permission) in pairs)
      {
        IReadOnlyList<string> expanded = AgentScopePermissionSeed.Expand([scope]);
        expanded.ShouldBe([permission]);
        foreach (string adminPermission in RolePermissionSeed.AdminPermissions)
        {
          expanded.ShouldNotContain(adminPermission);
        }

        foreach (string other in writesAndReply.Where(candidate => candidate != permission))
        {
          expanded.ShouldNotContain(other);
        }
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

    private static string SchemesOf(Type contract)
    {
      Attribute attribute = contract.GetCustomAttributes()
        .Single(candidate => candidate.GetType().Name == "EndpointAuthorizeAttribute");
      return ((string?)attribute.GetType().GetProperty("AuthenticationSchemes")!.GetValue(attribute))!;
    }
  }

} // namespace TimeWarp.Architecture.Features.Feedback
