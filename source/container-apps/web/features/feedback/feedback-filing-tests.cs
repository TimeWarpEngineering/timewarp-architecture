#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: feedback filing receipt, owner-only read, and optional email copy.
// Run standalone:  dotnet run source/container-apps/web/features/feedback/feedback-filing-tests.cs

#region Purpose
// Jaribu runfile for requirement 7: id and permalink, owner read, other-user 404, mail only when
// opted in with an email on the profile, submit success when there is no email, a receipt even
// when the mail send throws, and CR/LF rejection in mail headers.
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
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using ContractKind = TimeWarp.Architecture.Features.Feedback.FeedbackKind;
  using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;
  using GetContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;
  using GetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedback.Handler;
  using ListContract = TimeWarp.Architecture.Features.Feedback.ListMyFeedback;
  using ListHandler = TimeWarp.Architecture.Features.Feedback.Application.ListMyFeedback.Handler;
  using SubmitContract = TimeWarp.Architecture.Features.Feedback.SubmitFeedback;
  using SubmitHandler = TimeWarp.Architecture.Features.Feedback.Application.SubmitFeedback.Handler;

  [TestTag("Handler")]
  public class FeedbackFiling_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<FeedbackFiling_Given_>();

    public static async Task Submit_Should_ReturnIdAndPermalink()
    {
      World world = new();
      SubmitContract.Response response = await world.SubmitAsync(
        world.Owner,
        new SubmitContract.Command
        {
          Kind = ContractKind.Complaint,
          Title = "  The export failed  ",
          Body = "  Nothing came back.  ",
          EmailCopy = false,
        });

      response.FeedbackItemId.ShouldNotBe(Guid.Empty);
      response.Permalink.ShouldBe(FeedbackPermalink.For(response.FeedbackItemId));
      response.Permalink.ShouldBe($"/Feedback/{response.FeedbackItemId:D}");
      response.Kind.ShouldBe(ContractKind.Complaint);
      response.Title.ShouldBe("The export failed");
      response.Body.ShouldBe("Nothing came back.");
      response.EmailCopySent.ShouldBeFalse();
      world.Mail.Messages.ShouldBeEmpty();
      Console.WriteLine($"FILING-PROOF id={response.FeedbackItemId:D} permalink={response.Permalink}");
    }

    public static async Task TwoSubmits_Should_ReceiveDistinctIds()
    {
      World world = new();
      SubmitContract.Response first = await world.SubmitAsync(world.Owner, Complaint("First"));
      SubmitContract.Response second = await world.SubmitAsync(world.Owner, Complaint("Second"));
      second.FeedbackItemId.ShouldNotBe(first.FeedbackItemId);
      second.Permalink.ShouldNotBe(first.Permalink);
    }

    public static async Task OwnerGet_Should_ReturnTheFiledItem()
    {
      World world = new();
      SubmitContract.Response filed = await world.SubmitAsync(world.Owner, Complaint("Receipt"));
      GetContract.Response opened = await world.GetAsync(world.Owner, filed.FeedbackItemId);
      opened.FeedbackItemId.ShouldBe(filed.FeedbackItemId);
      opened.Permalink.ShouldBe(filed.Permalink);
      opened.Kind.ShouldBe(ContractKind.Complaint);
      opened.Title.ShouldBe("Receipt");
      opened.Body.ShouldBe("Body for Receipt");
    }

    public static async Task OtherPrincipal_Should_Receive404()
    {
      World world = new();
      SubmitContract.Response filed = await world.SubmitAsync(world.Owner, Complaint("Private"));
      SharedProblemDetails problem = await world.GetProblemAsync(PrincipalId.New(), filed.FeedbackItemId);
      problem.Status.ShouldBe(404);
    }

    public static async Task MissingId_Should_Receive404()
    {
      World world = new();
      SharedProblemDetails problem = await world.GetProblemAsync(world.Owner, Guid.NewGuid());
      problem.Status.ShouldBe(404);
    }

    public static async Task List_Should_ReturnOnlyTheOwnersItemsNewestFirst()
    {
      World world = new();
      PrincipalId other = PrincipalId.New();
      FeedbackItem older = FeedbackItem.File(
        world.Owner.Value,
        DomainKind.Complaint,
        "Older",
        "older body",
        new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
      FeedbackItem newer = FeedbackItem.File(
        world.Owner.Value,
        DomainKind.BugReport,
        "Newer",
        "newer body",
        new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero));
      FeedbackItem foreign = FeedbackItem.File(
        other.Value,
        DomainKind.Other,
        "Foreign",
        "foreign body",
        new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero));
      await world.Feedback.AddAsync(older);
      await world.Feedback.AddAsync(newer);
      await world.Feedback.AddAsync(foreign);

      ListContract.Response list = await world.ListAsync(world.Owner);
      list.Items.Count.ShouldBe(2);
      list.Items[0].FeedbackItemId.ShouldBe(newer.Id.Value);
      list.Items[0].Permalink.ShouldBe(FeedbackPermalink.For(newer.Id.Value));
      list.Items[0].Kind.ShouldBe(ContractKind.BugReport);
      list.Items[1].FeedbackItemId.ShouldBe(older.Id.Value);
      list.Items.ShouldNotContain(item => item.FeedbackItemId == foreign.Id.Value);
      list.EmailCopyAvailable.ShouldBeFalse();
    }

    public static async Task EmailCopy_Should_SendOnceWhenOptedInAndEmailIsOnFile()
    {
      World world = new();
      await world.SetEmailAsync("ada@example.com");
      SubmitContract.Response response = await world.SubmitAsync(
        world.Owner,
        new SubmitContract.Command
        {
          Kind = ContractKind.FeatureRequest,
          Title = "Export receipt",
          Body = "Include the id.",
          EmailCopy = true,
        });

      response.EmailCopySent.ShouldBeTrue();
      world.Mail.Messages.Count.ShouldBe(1);
      EmailMessage message = world.Mail.Messages[0];
      message.To.ShouldBe("ada@example.com");
      message.Subject.ShouldBe($"Feedback {response.FeedbackItemId:D}");
      message.Body.ShouldContain(response.FeedbackItemId.ToString("D"));
      message.Body.ShouldContain("FeatureRequest");
      message.Body.ShouldContain("Export receipt");
      message.Body.ShouldContain("Include the id.");
      message.Body.ShouldContain($"https://app.example{response.Permalink}");

      ListContract.Response list = await world.ListAsync(world.Owner);
      list.EmailCopyAvailable.ShouldBeTrue();
      Console.WriteLine(
        $"FILING-PROOF id={response.FeedbackItemId:D} permalink={response.Permalink} emailCopySent=true");
    }

    public static async Task FailingMailSend_Should_StillReturnTheReceipt()
    {
      World world = new(new ThrowingEmailSender());
      await world.SetEmailAsync("ada@example.com");
      SubmitContract.Command command = Complaint("Mail is down");
      command.EmailCopy = true;
      SubmitContract.Response response = await world.SubmitAsync(world.Owner, command);

      response.FeedbackItemId.ShouldNotBe(Guid.Empty);
      response.Permalink.ShouldBe(FeedbackPermalink.For(response.FeedbackItemId));
      response.EmailCopySent.ShouldBeFalse();
      GetContract.Response stored = await world.GetAsync(world.Owner, response.FeedbackItemId);
      stored.Title.ShouldBe("Mail is down");
      Console.WriteLine(
        $"FILING-PROOF id={response.FeedbackItemId:D} permalink={response.Permalink} emailCopySent=false (sender threw)");
    }

    public static Task EmailMessage_Should_RejectLineBreaksInHeaders()
    {
      Should.Throw<ArgumentException>(() => new EmailMessage("a@b.example\r\nBcc: x@y.example", "Subject", "Body"));
      Should.Throw<ArgumentException>(() => new EmailMessage("a@b.example", "Subject\nBcc: x@y.example", "Body"));
      Should.NotThrow(() => new EmailMessage("a@b.example", "Subject", "Line one\r\nLine two"));
      return Task.CompletedTask;
    }

    public static async Task UncheckedEmailCopy_Should_SendNothing()
    {
      World world = new();
      await world.SetEmailAsync("ada@example.com");
      SubmitContract.Response response = await world.SubmitAsync(world.Owner, Complaint("No copy"));
      response.EmailCopySent.ShouldBeFalse();
      response.FeedbackItemId.ShouldNotBe(Guid.Empty);
      world.Mail.Messages.ShouldBeEmpty();
    }

    public static async Task OptedInWithoutEmail_Should_SucceedAndSendNothing()
    {
      World world = new();
      await world.AddProfileAsync(email: null);
      SubmitContract.Command command = Complaint("Still filed");
      command.EmailCopy = true;
      SubmitContract.Response response = await world.SubmitAsync(world.Owner, command);
      response.EmailCopySent.ShouldBeFalse();
      response.Permalink.ShouldBe(FeedbackPermalink.For(response.FeedbackItemId));
      world.Mail.Messages.ShouldBeEmpty();
    }

    public static async Task NoProfile_Should_SucceedAndSendNothing()
    {
      World world = new();
      SubmitContract.Command command = Complaint("No profile");
      command.EmailCopy = true;
      SubmitContract.Response response = await world.SubmitAsync(world.Owner, command);
      response.EmailCopySent.ShouldBeFalse();
      world.Mail.Messages.ShouldBeEmpty();
      (await world.ListAsync(world.Owner)).EmailCopyAvailable.ShouldBeFalse();
    }

    public static async Task UnsignedSubmit_Should_Return401()
    {
      World world = new();
      SharedProblemDetails problem = (await world.SubmitAs(null).Handle(Complaint("Nope"), CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected unauthenticated problem"), details => details);
      problem.Status.ShouldBe(401);
      world.Mail.Messages.ShouldBeEmpty();
    }

    public static async Task UnsignedGetAndList_Should_Return401()
    {
      World world = new();
      SharedProblemDetails get = (await world.GetAs(null).Handle(
          new GetContract.Query { FeedbackItemId = Guid.NewGuid() },
          CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected unauthenticated problem"), details => details);
      SharedProblemDetails list = (await world.ListAs(null).Handle(new ListContract.Query(), CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected unauthenticated problem"), details => details);
      get.Status.ShouldBe(401);
      list.Status.ShouldBe(401);
    }

    public static Task KindNames_Should_MatchBetweenContractAndDomain()
    {
      Enum.GetNames<ContractKind>().ShouldBe(Enum.GetNames<DomainKind>());
      return Task.CompletedTask;
    }

    public static Task CommandAndResponse_Should_RoundTripAsPascalCaseEnumStrings()
    {
      SubmitContract.Command command = new()
      {
        Kind = ContractKind.Complaint,
        Title = "Export",
        Body = "Failed",
        EmailCopy = true,
      };
      string commandJson = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      commandJson.ShouldContain("\"kind\":\"Complaint\"");
      commandJson.ShouldNotContain("\"kind\":2");
      SubmitContract.Command commandRoundTrip = JsonSerializer.Deserialize<SubmitContract.Command>(
        commandJson,
        ContractSerializationDefaults.Options).ShouldNotBeNull();
      commandRoundTrip.Kind.ShouldBe(ContractKind.Complaint);
      commandRoundTrip.Title.ShouldBe("Export");
      commandRoundTrip.EmailCopy.ShouldBeTrue();

      SubmitContract.Response response = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "/Feedback/11111111-1111-1111-1111-111111111111",
        ContractKind.Complaint,
        "Export",
        "Failed",
        emailCopySent: false);
      string responseJson = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      responseJson.ShouldContain("11111111-1111-1111-1111-111111111111");
      responseJson.ShouldContain("\"permalink\":\"/Feedback/11111111-1111-1111-1111-111111111111\"");
      SubmitContract.Response responseRoundTrip = JsonSerializer.Deserialize<SubmitContract.Response>(
        responseJson,
        ContractSerializationDefaults.Options).ShouldNotBeNull();
      responseRoundTrip.FeedbackItemId.ShouldBe(response.FeedbackItemId);
      responseRoundTrip.Permalink.ShouldBe(response.Permalink);
      responseRoundTrip.EmailCopySent.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Validator_Should_RejectAnEmptyTitle()
    {
      ValidationResult result = new SubmitContract.Validator().Validate(new SubmitContract.Command
      {
        Kind = ContractKind.Other,
        Title = "  ",
        Body = "Details",
      });
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    private static SubmitContract.Command Complaint(string title) => new()
    {
      Kind = ContractKind.Complaint,
      Title = title,
      Body = $"Body for {title}",
    };

    private sealed class World
    {
      public InMemoryFeedbackStore Feedback { get; } = new();
      public InMemoryProfileStore Profiles { get; } = new();
      public ProfileEmailLookup Lookup { get; }
      public RecordingEmailSender Mail { get; } = new();
      public PrincipalId Owner { get; } = PrincipalId.New();
      private readonly IEmailSender Sender;

      public World(IEmailSender? sender = null)
      {
        Lookup = new ProfileEmailLookup(Profiles);
        Sender = sender ?? Mail;
      }

      public SubmitHandler SubmitAs(PrincipalId? principal) =>
        new(
          new StubCurrentPrincipalAccessor(principal),
          Feedback,
          Lookup,
          Sender,
          new FixedBaseUrl(new Uri("https://app.example")),
          NullLogger<SubmitHandler>.Instance);

      public GetHandler GetAs(PrincipalId? principal) =>
        new(new StubCurrentPrincipalAccessor(principal), Feedback);

      public ListHandler ListAs(PrincipalId? principal) =>
        new(new StubCurrentPrincipalAccessor(principal), Feedback, Lookup);

      public async Task<SubmitContract.Response> SubmitAsync(PrincipalId principal, SubmitContract.Command command) =>
        (await SubmitAs(principal).Handle(command, CancellationToken.None))
          .Match(ok => ok, _ => throw new InvalidOperationException("Expected submit success"));

      public async Task<GetContract.Response> GetAsync(PrincipalId principal, Guid feedbackItemId) =>
        (await GetAs(principal).Handle(new GetContract.Query { FeedbackItemId = feedbackItemId }, CancellationToken.None))
          .Match(ok => ok, _ => throw new InvalidOperationException("Expected get success"));

      public async Task<SharedProblemDetails> GetProblemAsync(PrincipalId principal, Guid feedbackItemId) =>
        (await GetAs(principal).Handle(new GetContract.Query { FeedbackItemId = feedbackItemId }, CancellationToken.None))
          .Match(_ => throw new InvalidOperationException("Expected get problem"), problem => problem);

      public async Task<ListContract.Response> ListAsync(PrincipalId principal) =>
        (await ListAs(principal).Handle(new ListContract.Query(), CancellationToken.None))
          .Match(ok => ok, _ => throw new InvalidOperationException("Expected list success"));

      public async Task SetEmailAsync(string email)
      {
        await AddProfileAsync(email);
      }

      public async Task AddProfileAsync(string? email)
      {
        Profile profile = Profile.Create(ProfileId.From(Owner.Value), "Ada Lovelace", "en-US", "US", "dark");
        profile.SetEmail(email);
        await Profiles.AddAsync(profile);
      }
    }

    private sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? PrincipalId;

      public StubCurrentPrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PrincipalId);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
      public List<EmailMessage> Messages { get; } = [];

      public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
      {
        cancellationToken.ThrowIfCancellationRequested();
        Messages.Add(message);
        return Task.CompletedTask;
      }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
      public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Mail transport is down");
    }

    private sealed class FixedBaseUrl : IAppBaseUrlAccessor
    {
      private readonly Uri? BaseUrl;

      public FixedBaseUrl(Uri? baseUrl) => BaseUrl = baseUrl;

      public Uri? GetBaseUrl() => BaseUrl;
    }
  }

} // namespace TimeWarp.Architecture.Features.Feedback
