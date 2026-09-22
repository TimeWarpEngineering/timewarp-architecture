#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for agent-human links + humanUx (task 205).
// Run standalone:  dotnet run source/container-apps/web/features/agent-links/agent-human-link-tests.cs

#region Purpose
// Jaribu runfile: AgentHumanLink store, request/approve/deny, and humanUx document.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentLinks
{

  using System;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using FluentValidation.Results;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentLinks.Application;
  using TimeWarp.Architecture.Features.AgentLinks.Domain;
  using RequestHandler = TimeWarp.Architecture.Features.AgentLinks.Application.RequestAgentHumanLink.Handler;
  using ApproveHandler = TimeWarp.Architecture.Features.AgentLinks.Application.ApproveAgentHumanLink.Handler;
  using DenyHandler = TimeWarp.Architecture.Features.AgentLinks.Application.DenyAgentHumanLink.Handler;
  using HumanUxHandler = TimeWarp.Architecture.Features.AgentLinks.Application.GetHumanUx.Handler;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class HumanUx_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<HumanUx_Given_>();

    public static Task Document_Should_RoundTripThroughJson()
    {
      GetHumanUx.Response response = new(
        title: "Linked human",
        summary: "Present this to your operator.",
        link: new HumanUxLink(
          Guid.Parse("11111111-2222-3333-4444-555555555555"),
          "Approved",
          Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
          Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff")),
        human: new HumanUxHuman("Ada", "ada@example.com"),
        actions: [new HumanUxAction("open-profile", "Open profile", "/Profile")]);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      GetHumanUx.Response? parsed = JsonSerializer.Deserialize<GetHumanUx.Response>(
        json,
        ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Spec.ShouldBe(GetHumanUx.Response.SpecId);
      parsed.Kind.ShouldBe(GetHumanUx.Response.KindId);
      parsed.Link.Status.ShouldBe("Approved");
      parsed.Human.ShouldNotBeNull();
      parsed.Human.DisplayName.ShouldBe("Ada");
      parsed.Actions.Count.ShouldBe(1);
      json.ShouldContain("timewarp.humanUx/v1");
      return Task.CompletedTask;
    }

    public static Task Empty_HumanPrincipalId_Should_FailValidation()
    {
      RequestAgentHumanLink.Command command = new() { HumanPrincipalId = Guid.Empty };
      ValidationResult result = new RequestAgentHumanLink.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }
  }

  [TestTag("Domain")]
  public class AgentHumanLink_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<AgentHumanLink_Given_>();

    public static Task Create_Should_StartPending_WithDistinctIds()
    {
      Guid agent = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      Guid human = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
      AgentHumanLink link = AgentHumanLink.Create(agent, human);
      link.Status.ShouldBe(AgentHumanLinkStatus.Pending);
      link.DecidedAt.ShouldBeNull();
      link.AgentPrincipalId.ShouldBe(agent);
      link.HumanPrincipalId.ShouldBe(human);
      return Task.CompletedTask;
    }

    public static Task Create_same_ids_Should_Throw()
    {
      Guid id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      Should.Throw<ArgumentException>(() => AgentHumanLink.Create(id, id));
      return Task.CompletedTask;
    }

    public static Task Approve_twice_Should_Throw()
    {
      AgentHumanLink link = AgentHumanLink.Create(
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"));
      link.Approve();
      Should.Throw<InvalidOperationException>(() => link.Approve());
      return Task.CompletedTask;
    }
  }

  [TestTag("Store")]
  public class InMemoryAgentHumanLinkStore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InMemoryAgentHumanLinkStore_Given_>();

    public static async Task FindOpen_Should_IgnoreDenied()
    {
      InMemoryAgentHumanLinkStore store = new();
      PrincipalId agent = PrincipalId.New();
      PrincipalId human = PrincipalId.New();
      AgentHumanLink link = AgentHumanLink.Create(agent.Value, human.Value);
      await store.AddAsync(link);
      link.Deny();
      await store.UpdateAsync(link);

      AgentHumanLink? open = await store.FindOpenAsync(agent, human);
      open.ShouldBeNull();
    }

    public static async Task Add_SecondOpenPair_Should_Throw_And_SucceedAfterDeny()
    {
      InMemoryAgentHumanLinkStore store = new();
      PrincipalId agent = PrincipalId.New();
      PrincipalId human = PrincipalId.New();
      AgentHumanLink first = AgentHumanLink.Create(agent.Value, human.Value);
      await store.AddAsync(first);

      AgentHumanLink duplicate = AgentHumanLink.Create(agent.Value, human.Value);
      await Should.ThrowAsync<InvalidOperationException>(async () => await store.AddAsync(duplicate));

      first.Deny();
      await store.UpdateAsync(first);

      AgentHumanLink retry = AgentHumanLink.Create(agent.Value, human.Value);
      await store.AddAsync(retry);
      AgentHumanLink? open = await store.FindOpenAsync(agent, human);
      open.ShouldNotBeNull();
      open.Id.ShouldBe(retry.Id);
    }
  }

  [TestTag("Handler")]
  public class RequestApproveHumanUx_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<RequestApproveHumanUx_Given_>();

    public static async Task Agent_Should_Request_And_Human_Should_Approve_Then_HumanUx()
    {
      InMemoryPrincipalStore principals = new();
      InMemoryAgentHumanLinkStore links = new();
      Principal agent = Principal.Create(PrincipalKind.Agent);
      Principal human = Principal.Create(PrincipalKind.Human);
      human.SetDisplayName("Ada");
      await principals.AddPrincipalAsync(agent);
      await principals.AddPrincipalAsync(human);

      RequestHandler request = new(
        new StubCurrentPrincipalAccessor(agent.Id),
        principals,
        links);
      OneOf<RequestAgentHumanLink.Response, SharedProblemDetails> requested =
        await request.Handle(
          new RequestAgentHumanLink.Command { HumanPrincipalId = human.Id.Value },
          CancellationToken.None);
      RequestAgentHumanLink.Response created = requested.Match(
        ok => ok,
        _ => throw new InvalidOperationException("Expected request success"));
      created.Status.ShouldBe(nameof(AgentHumanLinkStatus.Pending));

      ApproveHandler approve = new(
        new StubCurrentPrincipalAccessor(human.Id),
        principals,
        links);
      ApproveAgentHumanLink.Command approveCommand = new();
      approveCommand.LinkId = created.LinkId;
      OneOf<ApproveAgentHumanLink.Response, SharedProblemDetails> approved =
        await approve.Handle(approveCommand, CancellationToken.None);
      approved.Match(ok => ok.Status, _ => throw new InvalidOperationException("Expected approve"))
        .ShouldBe(nameof(AgentHumanLinkStatus.Approved));

      HumanUxHandler humanUx = new(
        new StubCurrentPrincipalAccessor(agent.Id),
        principals,
        links);
      GetHumanUx.Query humanUxQuery = new();
      humanUxQuery.LinkId = created.LinkId;
      GetHumanUx.Response document = (await humanUx.Handle(humanUxQuery, CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected humanUx"));
      document.Spec.ShouldBe("timewarp.humanUx/v1");
      document.Link.Status.ShouldBe(nameof(AgentHumanLinkStatus.Approved));
      document.Human.ShouldNotBeNull();
      document.Human.DisplayName.ShouldBe("Ada");
    }

    public static async Task Human_Should_Not_Request_Link()
    {
      InMemoryPrincipalStore principals = new();
      InMemoryAgentHumanLinkStore links = new();
      Principal human = Principal.Create(PrincipalKind.Human);
      Principal other = Principal.Create(PrincipalKind.Human);
      await principals.AddPrincipalAsync(human);
      await principals.AddPrincipalAsync(other);

      RequestHandler request = new(
        new StubCurrentPrincipalAccessor(human.Id),
        principals,
        links);
      SharedProblemDetails problem = (await request.Handle(
          new RequestAgentHumanLink.Command { HumanPrincipalId = other.Id.Value },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected problem"), p => p);
      problem.Status.ShouldBe(403);
    }

    public static async Task Duplicate_Open_Link_Should_Conflict()
    {
      InMemoryPrincipalStore principals = new();
      InMemoryAgentHumanLinkStore links = new();
      Principal agent = Principal.Create(PrincipalKind.Agent);
      Principal human = Principal.Create(PrincipalKind.Human);
      await principals.AddPrincipalAsync(agent);
      await principals.AddPrincipalAsync(human);

      RequestHandler request = new(
        new StubCurrentPrincipalAccessor(agent.Id),
        principals,
        links);
      RequestAgentHumanLink.Command command = new() { HumanPrincipalId = human.Id.Value };
      (await request.Handle(command, CancellationToken.None)).IsT0.ShouldBeTrue();
      SharedProblemDetails problem = (await request.Handle(command, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected problem"), p => p);
      problem.Status.ShouldBe(409);
    }

    public static async Task Deny_Should_Allow_New_Request()
    {
      InMemoryPrincipalStore principals = new();
      InMemoryAgentHumanLinkStore links = new();
      Principal agent = Principal.Create(PrincipalKind.Agent);
      Principal human = Principal.Create(PrincipalKind.Human);
      await principals.AddPrincipalAsync(agent);
      await principals.AddPrincipalAsync(human);

      RequestHandler request = new(
        new StubCurrentPrincipalAccessor(agent.Id),
        principals,
        links);
      RequestAgentHumanLink.Response created = (await request.Handle(
          new RequestAgentHumanLink.Command { HumanPrincipalId = human.Id.Value },
          CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected request"));

      DenyHandler deny = new(
        new StubCurrentPrincipalAccessor(human.Id),
        principals,
        links);
      DenyAgentHumanLink.Command denyCommand = new();
      denyCommand.LinkId = created.LinkId;
      (await deny.Handle(denyCommand, CancellationToken.None)).IsT0.ShouldBeTrue();

      (await request.Handle(
          new RequestAgentHumanLink.Command { HumanPrincipalId = human.Id.Value },
          CancellationToken.None))
        .IsT0.ShouldBeTrue();
    }

    private sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? PrincipalId;

      public StubCurrentPrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PrincipalId);
    }
  }

} // namespace TimeWarp.Architecture.Features.AgentLinks
