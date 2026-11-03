#region Purpose
// Shared fakes for the feedback triage runfiles: in-memory world, principal accessor, counting directory, mail capture.
#endregion

namespace TimeWarp.Architecture.Features.Feedback
{

  using Microsoft.Extensions.Logging.Abstractions;
  using Microsoft.Extensions.Options;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features.Feedback.Application;
  using TimeWarp.Architecture.Features.Feedback.Domain;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Architecture.Mail;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using AddNoteContract = TimeWarp.Architecture.Features.Feedback.AddFeedbackNote;
  using AddNoteHandler = TimeWarp.Architecture.Features.Feedback.Application.AddFeedbackNote.Handler;
  using AdminGetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedbackForAdmin.Handler;
  using AdminListContract = TimeWarp.Architecture.Features.Feedback.ListFeedback;
  using AdminListHandler = TimeWarp.Architecture.Features.Feedback.Application.ListFeedback.Handler;
  using ChangeStatusContract = TimeWarp.Architecture.Features.Feedback.ChangeFeedbackStatus;
  using ChangeStatusHandler = TimeWarp.Architecture.Features.Feedback.Application.ChangeFeedbackStatus.Handler;
  using ContractSeverity = TimeWarp.Architecture.Features.Feedback.FeedbackSeverity;
  using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
  using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;
  using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;
  using FilerGetContract = TimeWarp.Architecture.Features.Feedback.GetFeedback;
  using FilerGetHandler = TimeWarp.Architecture.Features.Feedback.Application.GetFeedback.Handler;
  using FilerListContract = TimeWarp.Architecture.Features.Feedback.ListMyFeedback;
  using FilerListHandler = TimeWarp.Architecture.Features.Feedback.Application.ListMyFeedback.Handler;
  using FilerReplyHandler = TimeWarp.Architecture.Features.Feedback.Application.ReplyAsFiler.Handler;
  using ReplyHandler = TimeWarp.Architecture.Features.Feedback.Application.ReplyToFeedback.Handler;
  using SetSeverityContract = TimeWarp.Architecture.Features.Feedback.SetFeedbackSeverity;
  using SetSeverityHandler = TimeWarp.Architecture.Features.Feedback.Application.SetFeedbackSeverity.Handler;

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
    private readonly InMemoryPrincipalRoleStore Roles = new();
    private readonly TriageHeldAgentScopes HeldScopes = new();
    private readonly PermissionEvaluator Evaluator;

    /// <summary>Admin is an Administrator; grantProfilesRead false strips admin.profiles.read from that role.</summary>
    public TriageWorld(IEmailSender? sender = null, bool grantProfilesRead = true)
    {
      Lookup = new ProfileEmailLookup(Profiles);
      Directory = new TriageCountingDirectory(new ProfileDirectory(Profiles));
      Sender = sender ?? Mail;
      Roles.SetRoleIdsAsync(Admin, [RoleIds.Administrator]).GetAwaiter().GetResult();
      InMemoryRolePermissionStore permissions = new();
      if (!grantProfilesRead)
      {
        permissions.SetPermissionIdsForRoleAsync(
          RoleIds.Administrator,
          [.. RolePermissionSeed.DefaultGrants[RoleIds.Administrator].Where(static id => id != PermissionIds.AdminProfilesRead)])
          .GetAwaiter().GetResult();
      }

      EffectiveRolesResolver resolver = new(
        Roles,
        Options.Create(new BootstrapAdministratorOptions { BootstrapAdministratorPrincipalIds = [] }),
        new InMemoryPrincipalStore());
      Evaluator = new PermissionEvaluator(resolver, permissions, HeldScopes);
    }

    public Task AssignAdministratorAsync(PrincipalId principal) =>
      Roles.SetRoleIdsAsync(principal, [RoleIds.Administrator]);

    public void HoldAgentScopes(PrincipalId principal, params string[] scopes) =>
      HeldScopes.Hold(principal, scopes);

    public void ReleaseAgentScopes(PrincipalId principal) => HeldScopes.Release(principal);

    public static DateTimeOffset Day(int day) => new(2026, 10, day, 0, 0, 0, TimeSpan.Zero);

    public async Task<FeedbackItem> AddAsync(Guid owner, int day, DomainKind kind = DomainKind.BugReport)
    {
      FeedbackItem item = FeedbackItem.File(owner, kind, "Export failed", "Nothing came back.", Day(day));
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
      new(new TriagePrincipalAccessor(principal), Evaluator, Directory, HeldScopes);

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

    public SetSeverityHandler SetSeverity(PrincipalId? principal) =>
      new(new TriagePrincipalAccessor(principal), Feedback, Attachments, Lookup, OwnerNames(principal));

    public SetSeverityHandler SetSeverity() => SetSeverity(Admin);

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

    public async Task<SetSeverityContract.Response> SetSeverityAsync(Guid id, ContractSeverity severity) =>
      (await SetSeverity().Handle(new SetSeverityContract.Command { FeedbackItemId = id, Severity = severity }, CancellationToken.None))
        .Match(ok => ok, problem => throw new InvalidOperationException($"Expected severity success: {problem.Detail}"));

    public async Task<SharedProblemDetails> SetSeverityProblemAsync(Guid id, ContractSeverity severity) =>
      (await SetSeverity().Handle(new SetSeverityContract.Command { FeedbackItemId = id, Severity = severity }, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected severity problem"), problem => problem);

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

  internal sealed class TriageHeldAgentScopes : IAgentPermissionScopeSource
  {
    private readonly Dictionary<Guid, IReadOnlyList<string>> Scopes = [];

    public void Hold(PrincipalId principal, params string[] scopes) =>
      Scopes[principal.Value] = scopes;

    public void Release(PrincipalId principal) => Scopes.Remove(principal.Value);

    public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) =>
      Scopes.TryGetValue(principalId.Value, out IReadOnlyList<string>? scopes) ? scopes : null;
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
