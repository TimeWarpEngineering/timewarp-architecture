#region Purpose
// Ask Tag (task 346): pick mode, composer chips, the semantic payload a message carries, the 345
// resource mapping of a picked row, and the privacy rules on what can be attached.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (no Aspire), like the @ reference tests: a
// controllable principal decides which resource references survive. The browser is a FakeItEasy
// IJSRuntime whose ask-tag.js module arms on Start (or when the test says, task 351) and answers
// from the test (a candidate's JSON, "" for a cancel, or a pending task the test completes after
// CancelPick). Choose is driven directly for
// the chip and payload rules. The chat client is exercised over a recording inner client, so the
// payload test sees exactly what would reach the model. Privacy has two halves: the state refuses
// a reference the user may not list, and AuthorizeView content the user cannot see is never
// rendered, so the mark ask-tag.ts looks for is absent (Section rendered with HtmlRenderer).
// The pointer and keyboard overlay is the Playwright Ask sign-in test.
#endregion

namespace AskTag_;

using FakeItEasy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation;

[TestTag("Unit")]
public class AskTag_Should_
{
  private const string FeedbackId = "11111111-1111-1111-1111-111111111111";

  private const string Route = "/Admin/Feedback";

  private static readonly string[] Administrator = [.. PermissionIds.All];

  private static readonly string[] Member =
  [
    PermissionIds.ProfileRead,
    PermissionIds.SettingsRead,
    PermissionIds.FeedbackFileSelf,
    PermissionIds.CredentialManageSelf,
  ];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskTag_Should_>();

  public static async Task Enter_Pick_Mode_And_Attach_The_Chosen_Element()
  {
    TaskCompletionSource<string?> answer = new();
    using TagSpa spa = new(Administrator, () => new ValueTask<string?>(answer.Task));
    using SpaTestScope scope = SpaTestScope.Create(spa);

    Task pick = scope.Send(new AskTagState.StartPickActionSet.Action(Route));
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.IsPicking.ShouldBeTrue();
    pick.IsCompleted.ShouldBeFalse();

    answer.SetResult(RowJson(ListFeedback.ReferencePrefix));
    await pick;

    state = scope.Store.GetState<AskTagState>();
    state.IsPicking.ShouldBeFalse();
    AskTag tag = state.AttachedFor(0).ShouldHaveSingleItem();
    tag.ChipText.ShouldBe("Feedback row: Dark theme broken");
    tag.Route.ShouldBe(Route);
    tag.DataQa.ShouldBe("AdminFeedbackRow");
    tag.Landmark.ShouldBe("Card: Feedback");
  }

  public static async Task Attach_Nothing_When_Pick_Mode_Is_Cancelled()
  {
    // Esc in the browser: Pick answers "".
    using (TagSpa spa = new(Administrator, () => new ValueTask<string?>("")))
    using (SpaTestScope scope = SpaTestScope.Create(spa))
    {
      await scope.Send(new AskTagState.StartPickActionSet.Action(Route));
      AskTagState state = scope.Store.GetState<AskTagState>();
      state.IsPicking.ShouldBeFalse();
      state.Attached.ShouldBeEmpty();
    }

    // The panel's Cancel: an answer that arrives after CancelPick is stale and is dropped.
    TaskCompletionSource<string?> late = new();
    using (TagSpa spa = new(Administrator, () => new ValueTask<string?>(late.Task)))
    using (SpaTestScope scope = SpaTestScope.Create(spa))
    {
      Task pick = scope.Send(new AskTagState.StartPickActionSet.Action(Route));
      await scope.Send(new AskTagState.CancelPickActionSet.Action());
      scope.Store.GetState<AskTagState>().IsPicking.ShouldBeFalse();
      late.SetResult(RowJson(ListFeedback.ReferencePrefix));
      await pick;
      AskTagState state = scope.Store.GetState<AskTagState>();
      state.IsPicking.ShouldBeFalse();
      state.Attached.ShouldBeEmpty();
    }
  }

  public static async Task Say_Picking_Only_Once_The_Browser_Has_Armed_The_Pick()
  {
    // Task 351: the "picking" line means a click or key is caught, so it waits for Start.
    TaskCompletionSource<bool> start = new();
    TaskCompletionSource<string?> answer = new();
    using TagSpa spa = new(Administrator, () => new ValueTask<string?>(answer.Task), () => new ValueTask<bool>(start.Task));
    using SpaTestScope scope = SpaTestScope.Create(spa);

    Task pick = scope.Send(new AskTagState.StartPickActionSet.Action(Route));
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.IsPicking.ShouldBeFalse();
    state.IsPickPending.ShouldBeTrue();

    start.SetResult(true);
    scope.Store.GetState<AskTagState>().IsPicking.ShouldBeTrue();
    answer.SetResult(RowJson(ListFeedback.ReferencePrefix));
    await pick;
    state = scope.Store.GetState<AskTagState>();
    state.IsPicking.ShouldBeFalse();
    state.IsPickPending.ShouldBeFalse();
    state.AttachedFor(0).ShouldHaveSingleItem();
  }

  public static async Task Cancel_A_Pick_That_Is_Still_Arming()
  {
    // Task 351: Cancel (or a location change) while Start is on its way names that pick, so the
    // browser refuses or ends it, and the panel never says "picking" for it.
    TaskCompletionSource<bool> start = new();
    using TagSpa spa = new(Administrator, () => new ValueTask<string?>(RowJson(ListFeedback.ReferencePrefix)), () => new ValueTask<bool>(start.Task));
    using SpaTestScope scope = SpaTestScope.Create(spa);

    Task pick = scope.Send(new AskTagState.StartPickActionSet.Action(Route));
    string? token = scope.Store.GetState<AskTagState>().PickToken;
    token.ShouldNotBeNull();
    await scope.Send(new AskTagState.CancelPickActionSet.Action());
    spa.ModuleCalls("CancelPick").ShouldBe([token]);

    // Start lands after the cancel and arms anyway (a reordered call): the stale pick disarms itself.
    start.SetResult(true);
    await pick;
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.IsPicking.ShouldBeFalse();
    state.IsPickPending.ShouldBeFalse();
    state.Attached.ShouldBeEmpty();
    spa.ModuleCalls("CancelPick").ShouldBe([token, token]);
    spa.ModuleCalls("Answer").ShouldBeEmpty();
  }

  public static async Task Add_And_Remove_Chips_Per_Conversation()
  {
    using TagSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Your filings"), "/Feedback"));
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Error bar", "OriginMismatch"), "/Feedback"));
    // The same element picked twice is kept once, newest last.
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Your filings"), "/Feedback"));
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.Attached.Select(static tag => tag.ChipText).ShouldBe(["Error bar: OriginMismatch", "Card: Your filings"]);

    await scope.Send(new AskTagState.RemoveActionSet.Action(state.Attached[0].Id));
    state = scope.Store.GetState<AskTagState>();
    state.Attached.ShouldHaveSingleItem().ChipText.ShouldBe("Card: Your filings");

    // At most AttachedCap chips wait for a send; the oldest goes first.
    for (int index = 0; index < AskTagState.AttachedCap + 2; index++)
    {
      await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Card " + index), "/"));
    }

    state = scope.Store.GetState<AskTagState>();
    state.Attached.Count.ShouldBe(AskTagState.AttachedCap);
    state.Attached[^1].Label.ShouldBe("Card " + (AskTagState.AttachedCap + 1));

    // New conversation: the chips belong to the old one.
    await scope.Send(new AgentSurfaceState.NewConversationActionSet.Action());
    int generation = scope.Store.GetState<AgentSurfaceState>().ConversationGeneration;
    scope.Store.GetState<AskTagState>().AttachedFor(generation).ShouldBeEmpty();
  }

  public static async Task Send_A_Semantic_Payload_With_The_Message_And_Keep_It_In_Page_Context()
  {
    using TagSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    AskTagCandidate noisy = new
    (
      Kind: "  Feedback   row ",
      Label: "Dark theme broken",
      Text: "Dark theme broken\n\n   Bug   Open " + new string('x', 1_000),
      DataQa: "AdminFeedbackRow",
      Landmark: "Card: Feedback",
      Reference: "@" + ListFeedback.ReferencePrefix + ":" + FeedbackId
    );
    await scope.Send(new AskTagState.ChooseActionSet.Action(noisy, Route));
    IReadOnlyList<AskTag> attached = scope.Store.GetState<AskTagState>().Attached;
    AskTag tag = attached.ShouldHaveSingleItem();
    tag.Kind.ShouldBe("Feedback row");
    tag.Text.ShouldStartWith("Dark theme broken Bug Open x");
    tag.Text.Length.ShouldBe(AskTagState.TextCap);

    // The chat client appends the payload to a copy of the person's message and reports it sent.
    RecordingChatClient inner = new();
    int marked = 0;
    using AskTagChatClient client = new
    (
      inner,
      () => scope.Store.GetState<AskTagState>().AttachedFor(scope.Store.GetState<AgentSurfaceState>().ConversationGeneration),
      async ids =>
      {
        marked++;
        await scope.Send(new AskTagState.MarkSentActionSet.Action(ids));
      },
      () => "instructions " + scope.Store.GetState<AskTagState>().Sent.Count
    );
    ChatMessage typed = new(ChatRole.User, "What is wrong with this?");
    ChatOptions requested = new() { Instructions = "stale" };
    await foreach (ChatResponseUpdate _ in client.GetStreamingResponseAsync([typed], requested))
    {
    }

    // Instructions are re-rendered per request on a clone, so the caller's options are untouched.
    requested.Instructions.ShouldBe("stale");
    inner.Options.ShouldHaveSingleItem()!.Instructions.ShouldBe("instructions 0");

    marked.ShouldBe(1);
    typed.Contents.Count.ShouldBe(1);
    ChatMessage sent = inner.Requests.ShouldHaveSingleItem()[^1];
    sent.Text.ShouldStartWith("What is wrong with this?");
    string payloadText = sent.Contents.OfType<TextContent>().Last().Text;
    payloadText.ShouldStartWith(AskTagPayload.MessageHeading);
    using (JsonDocument payload = JsonDocument.Parse(payloadText[AskTagPayload.MessageHeading.Length..]))
    {
      JsonElement entry = payload.RootElement.EnumerateArray().ShouldHaveSingleItem();
      entry.GetProperty("kind").GetString().ShouldBe("Feedback row");
      entry.GetProperty("label").GetString().ShouldBe("Dark theme broken");
      entry.GetProperty("dataQa").GetString().ShouldBe("AdminFeedbackRow");
      entry.GetProperty("landmark").GetString().ShouldBe("Card: Feedback");
      entry.GetProperty("route").GetString().ShouldBe(Route);
      entry.GetProperty("reference").GetProperty("token").GetString().ShouldBe("@feedback:" + FeedbackId);
      entry.EnumerateObject().Select(static property => property.Name)
        .ShouldBe(["kind", "label", "text", "dataQa", "landmark", "route", "reference"]);
    }

    // Sent once: the chips moved to Sent, so a tool round trip in the same turn adds nothing.
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.Attached.ShouldBeEmpty();
    state.Sent.ShouldHaveSingleItem().ShouldBe(tag);
    ChatMessage toolResult = new(ChatRole.Tool, [new FunctionResultContent("call-1", "{}")]);
    await client.GetResponseAsync([typed, toolResult], requested);
    marked.ShouldBe(1);
    // The next request sees the tag that was sent.
    inner.Options[^1]!.Instructions.ShouldBe("instructions 1");
    inner.Requests[^1][^1].ShouldBeSameAs(toolResult);

    // page_context.tags keeps the sent tag for later turns, until New conversation.
    using (JsonDocument context = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, Route)))
    {
      JsonElement entry = context.RootElement.GetProperty("tags").EnumerateArray().ShouldHaveSingleItem();
      entry.GetProperty("reference").GetProperty("id").GetString().ShouldBe(FeedbackId);
    }

    AgentAskInstructions.Preface.ShouldContain("page_context.tags");
    await scope.Send(new AgentSurfaceState.NewConversationActionSet.Action());
    using (JsonDocument context = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, Route)))
    {
      context.RootElement.GetProperty("tags").GetArrayLength().ShouldBe(0);
    }
  }

  public static async Task Keep_Chips_Attached_When_The_Request_Fails()
  {
    using TagSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Your filings"), "/Feedback"));

    RecordingChatClient inner = new() { Fail = true };
    int marked = 0;
    using AskTagChatClient client = new
    (
      inner,
      () => scope.Store.GetState<AskTagState>().AttachedFor(0),
      async ids =>
      {
        marked++;
        await scope.Send(new AskTagState.MarkSentActionSet.Action(ids));
      }
    );
    ChatMessage typed = new(ChatRole.User, "Why?");
    await Should.ThrowAsync<InvalidOperationException>(() => client.GetResponseAsync([typed]));
    await Should.ThrowAsync<InvalidOperationException>(async () =>
    {
      await foreach (ChatResponseUpdate _ in client.GetStreamingResponseAsync([typed]))
      {
      }
    });
    marked.ShouldBe(0);
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.Attached.ShouldHaveSingleItem();
    state.Sent.ShouldBeEmpty();

    // Only the chips that went out move: one attached after the snapshot stays for the next message.
    AskTag first = state.Attached[0];
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Late"), "/Feedback"));
    await scope.Send(new AskTagState.MarkSentActionSet.Action([first.Id]));
    state = scope.Store.GetState<AskTagState>();
    state.Sent.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
    state.Attached.ShouldHaveSingleItem().Label.ShouldBe("Late");
  }

  public static async Task Resolve_A_Picked_Feedback_Row_To_Its_Reference()
  {
    using TagSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    // The admin grid marks each row with the contract's own prefix.
    IReadOnlyDictionary<string, object> mark = AskTagMark.Resource
    (
      AskTagMark.FeedbackRowKind,
      ListFeedback.ReferencePrefix,
      FeedbackId,
      "Dark theme broken"
    );
    string token = mark[AskTagMark.ReferenceAttribute].ShouldBeOfType<string>();
    token.ShouldBe("@feedback:" + FeedbackId);

    await scope.Send(new AskTagState.ChooseActionSet.Action(Row(token), Route));
    ResourceReferenceItem reference = scope.Store.GetState<AskTagState>().Attached.ShouldHaveSingleItem().Reference.ShouldNotBeNull();
    reference.Token.ShouldBe("@feedback:" + FeedbackId);
    reference.Prefix.ShouldBe(ListFeedback.ReferencePrefix);
    reference.Id.ShouldBe(FeedbackId);
    reference.IdTypeName.ShouldBe(ResourceReferenceRegistry.Find(ListFeedback.ReferencePrefix)!.IdTypeName);
    reference.Label.ShouldBe("Dark theme broken");

    // Every prefix a page marks rows with is a registered @ type.
    ResourceReferenceRegistry.Find(ListFeedback.ReferencePrefix).ShouldNotBeNull();
    ResourceReferenceRegistry.Find(ListMyFeedback.ReferencePrefix).ShouldNotBeNull();
  }

  public static async Task Never_Attach_What_The_User_Cannot_See()
  {
    using TagSpa spa = new(Member);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    // A member may not list admin feedback: the element is attached without the reference.
    await scope.Send(new AskTagState.ChooseActionSet.Action(Row("@feedback:" + FeedbackId), "/Feedback"));
    scope.Store.GetState<AskTagState>().Attached.ShouldHaveSingleItem().Reference.ShouldBeNull();

    // Their own filings resolve.
    await scope.Send(new AskTagState.ChooseActionSet.Action(Row("@my-feedback:" + FeedbackId) with { Label = "Mine" }, "/Feedback"));
    scope.Store.GetState<AskTagState>().Attached[^1].Reference.ShouldNotBeNull().Token.ShouldBe("@my-feedback:" + FeedbackId);

    // An unknown prefix, a malformed token, or an element with no kind is not trusted.
    await scope.Send(new AskTagState.ChooseActionSet.Action(Row("@nope:" + FeedbackId) with { Label = "Nope" }, "/"));
    await scope.Send(new AskTagState.ChooseActionSet.Action(Row("feedback " + FeedbackId) with { Label = "Bad" }, "/"));
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("   ", "No kind"), "/"));
    AskTagState state = scope.Store.GetState<AskTagState>();
    state.Attached.Count.ShouldBe(4);
    state.Attached.Single(static tag => tag.Label == "Nope").Reference.ShouldBeNull();
    state.Attached.Single(static tag => tag.Label == "Bad").Reference.ShouldBeNull();
    state.Attached.ShouldNotContain(static tag => tag.Label == "No kind");

    // A different principal never sees the previous user's chips.
    await scope.Send(new AskTagState.SessionChangedActionSet.Action(AskReferenceState.PrincipalIdOf(spa.User)));
    await scope.Send(new AskTagState.ChooseActionSet.Action(Candidate("Card", "Kept"), "/"));
    await scope.Send(new AskTagState.SessionChangedActionSet.Action(AskReferenceState.PrincipalIdOf(spa.User)));
    scope.Store.GetState<AskTagState>().Attached.ShouldNotBeEmpty();
    await scope.Send(new AskTagState.SessionChangedActionSet.Action(Guid.NewGuid().ToString()));
    state = scope.Store.GetState<AskTagState>();
    state.Attached.ShouldBeEmpty();
    state.Sent.ShouldBeEmpty();
  }

  public static async Task Not_Render_A_Pickable_Mark_Inside_An_Unauthorized_Section()
  {
    string member = await RenderAdminSectionAsync(Member);
    member.ShouldNotContain(AskTagMark.KindAttribute);
    member.ShouldNotContain("Secret admin text");

    string administrator = await RenderAdminSectionAsync(Administrator);
    administrator.ShouldContain(AskTagMark.KindAttribute + "=\"" + AskTagMark.CardKind + "\"");
    administrator.ShouldContain(AskTagMark.LabelAttribute + "=\"Admin only\"");
    administrator.ShouldContain("Secret admin text");
  }

  private static async Task<string> RenderAdminSectionAsync(string[] permissions)
  {
    using TagSpa spa = new(permissions);
    using IServiceScope scope = spa.ServiceProvider.CreateScope();
    await using HtmlRenderer renderer = new(scope.ServiceProvider, NullLoggerFactory.Instance);
    return await renderer.Dispatcher.InvokeAsync(async () =>
    {
      RenderFragment section = builder =>
      {
        builder.OpenComponent<Section>(0);
        builder.AddComponentParameter(1, nameof(Section.Title), "Admin only");
        builder.AddComponentParameter(2, nameof(Section.Policy), PermissionIds.AdminFeedbackRead);
        builder.AddComponentParameter(3, nameof(Section.ChildContent), (RenderFragment)(content => content.AddContent(0, "Secret admin text")));
        builder.CloseComponent();
      };
      ParameterView parameters = ParameterView.FromDictionary
      (
        new Dictionary<string, object?> { [nameof(CascadingAuthenticationState.ChildContent)] = section }
      );
      Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent root = await renderer.RenderComponentAsync<CascadingAuthenticationState>(parameters);
      await root.QuiescenceTask;
      return root.ToHtmlString();
    });
  }

  private static AskTagCandidate Candidate(string kind, string label) =>
    new(kind, label, label + " text", DataQa: null, Landmark: null, Reference: null);

  private static AskTagCandidate Row(string reference) =>
    new("Feedback row", "Dark theme broken", "Dark theme broken Bug Open", "AdminFeedbackRow", "Card: Feedback", reference);

  private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

  private static string RowJson(string prefix) => JsonSerializer.Serialize(Row("@" + prefix + ":" + FeedbackId), WebJson);

  private sealed class RecordingChatClient : IChatClient
  {
    public List<List<ChatMessage>> Requests { get; } = [];

    public List<ChatOptions?> Options { get; } = [];

    public bool Fail { get; init; }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      if (Fail)
      {
        throw new InvalidOperationException("upstream failed");
      }

      Requests.Add([.. messages]);
      Options.Add(options);
      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
      {
        yield return update;
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }

  private sealed class TagSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal User { get; }

    // The fake ask-tag.js module, kept only to read its recorded calls.
    private readonly object ModuleFake;

    /// <summary>The first argument of every ask-tag.js call to <paramref name="identifier"/>, in order.</summary>
    public IReadOnlyList<object?> ModuleCalls(string identifier) =>
    [
      .. Fake.GetCalls(ModuleFake)
        .Where(call => Equals(call.Arguments.FirstOrDefault(), identifier))
        .Select(static call => call.Arguments.OfType<object?[]>().FirstOrDefault()?.FirstOrDefault()),
    ];

    public TagSpa(string[]? permissions, Func<ValueTask<string?>>? pick = null, Func<ValueTask<bool>>? start = null)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
          [
            new Claim("sub", Guid.NewGuid().ToString()),
            .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
          ],
          authenticationType: "test"));

      // ask-tag.js: Start arms when the test says (at once by default), Answer answers what the test
      // says; every other module call is a no-op and is recorded.
      IJSObjectReference module = A.Fake<IJSObjectReference>();
      ModuleFake = module;
      Func<ValueTask<string?>> answer = pick ?? (static () => new ValueTask<string?>(""));
      Func<ValueTask<bool>> armed = start ?? (static () => new ValueTask<bool>(true));
      A.CallTo(module).WithReturnType<ValueTask<string?>>().ReturnsLazily(answer);
      A.CallTo(module).WithReturnType<ValueTask<bool>>().ReturnsLazily(armed);
      IJSRuntime jsRuntime = A.Fake<IJSRuntime>();
      A.CallTo(jsRuntime).WithReturnType<ValueTask<IJSObjectReference>>().Returns(new ValueTask<IJSObjectReference>(module));

      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(User));
      services.AddScoped(_ => jsRuntime);
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<IWebServerApiService>(provider => new MockWebApiService
      (
        A.Fake<IApiService>(),
        provider.GetRequiredService<ILogger<MockWebApiService>>(),
        provider
      ));

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }
}
