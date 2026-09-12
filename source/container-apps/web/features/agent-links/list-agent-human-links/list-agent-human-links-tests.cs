#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: ListAgentHumanLinks validator + handler (human-session and agent-token paths).
// Run standalone:  dotnet run source/container-apps/web/features/agent-links/list-agent-human-links/list-agent-human-links-tests.cs

#region Purpose
// Jaribu runfile: ListAgentHumanLinks happy-path listing for human and agent callers, plus rejection.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentLinks
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentLinks.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using ListQuery = TimeWarp.Architecture.Features.AgentLinks.ListAgentHumanLinks;
  using ListHandler = TimeWarp.Architecture.Features.AgentLinks.Application.ListAgentHumanLinks.Handler;
  using InMemoryAgentHumanLinkStore = TimeWarp.Architecture.Features.AgentLinks.Application.InMemoryAgentHumanLinkStore;

  [TestTag("Validation")]
  public class ListAgentHumanLinksValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ListAgentHumanLinksValidator_Given_>();

    public static Task EmptyQuery_Should_PassValidation()
    {
      ValidationResult result = new ListQuery.Validator().Validate(new ListQuery.Query());
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }
  }

  [TestTag("Handler")]
  public class ListAgentHumanLinksHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ListAgentHumanLinksHandler_Given_>();

    public static async Task Unauthenticated_Should_Return401()
    {
      ListHandler handler = new(new StubCurrentPrincipalAccessor(null), new InMemoryAgentHumanLinkStore());
      SharedProblemDetails problem = (await handler.Handle(new ListQuery.Query(), CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected unauthenticated problem"), p => p);
      problem.Status.ShouldBe(401);
    }

    public static async Task HumanCaller_Should_ListLinksWhereTheyAreTheHuman()
    {
      (InMemoryAgentHumanLinkStore store, PrincipalId agent, PrincipalId human, AgentHumanLink owned) =
        await SeedOwnedAndUnrelatedAsync();

      ListHandler handler = new(new StubCurrentPrincipalAccessor(human), store);
      ListQuery.Response response = (await handler.Handle(new ListQuery.Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected list success"));

      response.Items.Count.ShouldBe(1);
      response.Items[0].LinkId.ShouldBe(owned.Id.Value);
      response.Items[0].AgentPrincipalId.ShouldBe(agent.Value);
      response.Items[0].HumanPrincipalId.ShouldBe(human.Value);
      response.Items[0].Status.ShouldBe(nameof(AgentHumanLinkStatus.Pending));
    }

    public static async Task AgentCaller_Should_ListLinksWhereTheyAreTheAgent()
    {
      (InMemoryAgentHumanLinkStore store, PrincipalId agent, PrincipalId human, AgentHumanLink owned) =
        await SeedOwnedAndUnrelatedAsync();

      ListHandler handler = new(new StubCurrentPrincipalAccessor(agent), store);
      ListQuery.Response response = (await handler.Handle(new ListQuery.Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected list success"));

      response.Items.Count.ShouldBe(1);
      response.Items[0].LinkId.ShouldBe(owned.Id.Value);
      response.Items[0].AgentPrincipalId.ShouldBe(agent.Value);
      response.Items[0].HumanPrincipalId.ShouldBe(human.Value);
      response.Items[0].Status.ShouldBe(nameof(AgentHumanLinkStatus.Pending));
    }

    public static async Task OtherPrincipal_Should_NotSeeUnrelatedLinks()
    {
      (InMemoryAgentHumanLinkStore store, _, _, _) = await SeedOwnedAndUnrelatedAsync();

      ListHandler handler = new(new StubCurrentPrincipalAccessor(PrincipalId.New()), store);
      ListQuery.Response response = (await handler.Handle(new ListQuery.Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected list success"));

      response.Items.ShouldBeEmpty();
    }

    private static async Task<(InMemoryAgentHumanLinkStore Store, PrincipalId Agent, PrincipalId Human, AgentHumanLink Owned)> SeedOwnedAndUnrelatedAsync()
    {
      InMemoryAgentHumanLinkStore store = new();
      PrincipalId agent = PrincipalId.New();
      PrincipalId human = PrincipalId.New();
      AgentHumanLink owned = AgentHumanLink.Create(agent.Value, human.Value);
      await store.AddAsync(owned);
      await store.AddAsync(AgentHumanLink.Create(PrincipalId.New().Value, PrincipalId.New().Value));
      return (store, agent, human, owned);
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
