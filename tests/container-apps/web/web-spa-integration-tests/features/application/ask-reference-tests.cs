#region Purpose
// Ask @ references (task 345): the generated resource-type registry, the permission-filtered type
// list, @ -> types -> search -> choose at the AskReferenceState seam, and page_context resolving a
// chosen token to its typed id.
#endregion

#region Design
// C-create in-proc SPA ServiceProvider per test (no Aspire), like the command palette tests: the
// permission filter needs a controllable principal, and the SPA's own PolicyRegistration decides
// which types a principal may list. List queries go through MockWebApiService, the app's offline
// mode, which answers from each contract's GetMockResponseFactory (those honour Search), so a
// search runs the same generated query builder and item mapping the browser does. Its inner
// fall-through IApiService is a fake that is never reached. Server-side search and 403 behaviour
// are pinned by the co-located handler runfiles and web-server integration tests; the picker
// markup and caret insertion are the Playwright Ask surface test.
#endregion

namespace AskReference_;

using FakeItEasy;
using TimeWarp.Architecture.Features;
using TimeWarp.Foundation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Security.Claims;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Identity;

[TestTag("Unit")]
public class AskReference_Should_
{
  private static readonly string[] Administrator = [.. PermissionIds.All];

  private static readonly string[] Member =
  [
    PermissionIds.ProfileRead,
    PermissionIds.SettingsRead,
    PermissionIds.FeedbackFileSelf,
    PermissionIds.CredentialManageSelf,
  ];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskReference_Should_>();

  public static Task Generate_A_Registry_Entry_Per_Referenceable_List_Query()
  {
    ResourceReferenceRegistry.All.Select(static type => type.Prefix)
      .ShouldBe(["credential", "feedback", "my-feedback", "principal", "role"]);
    ResourceReferenceType principal = ResourceReferenceRegistry.Find("principal").ShouldNotBeNull();
    principal.IdTypeName.ShouldBe(nameof(PrincipalId));
    principal.Policy.ShouldBe(PermissionIds.AdminPrincipalsRead);
    ResourceReferenceRegistry.Find("credential")!.IdTypeName.ShouldBe(nameof(CredentialId));
    ResourceReferenceRegistry.Find("feedback")!.Policy.ShouldBe(PermissionIds.AdminFeedbackRead);
    ResourceReferenceRegistry.Find("my-feedback")!.Policy.ShouldBe(PermissionIds.FeedbackFileSelf);
    ResourceReferenceRegistry.Find("role")!.Policy.ShouldBe(PermissionIds.AdminRolesRead);
    ResourceReferenceRegistry.Find("nope").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static async Task Offer_A_Member_Only_Their_Own_Resource_Types()
  {
    using ReferenceSpa spa = new(Member);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    AskReferenceState state = scope.Store.GetState<AskReferenceState>();

    state.IsOpen.ShouldBeTrue();
    state.Types.Select(static type => type.Prefix).ShouldBe(["credential", "my-feedback"]);

    // A type the member was not offered cannot be searched.
    await scope.Send(new AskReferenceState.ChooseTypeActionSet.Action("principal"));
    scope.Store.GetState<AskReferenceState>().SelectedType.ShouldBeNull();
  }

  public static async Task Offer_An_Administrator_Every_Resource_Type()
  {
    using ReferenceSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());

    scope.Store.GetState<AskReferenceState>().Types.Select(static type => type.Prefix)
      .ShouldBe(["credential", "feedback", "my-feedback", "principal", "role"]);
  }

  public static async Task Offer_Signed_Out_Visitors_No_Types()
  {
    using ReferenceSpa spa = new(permissions: null);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());

    scope.Store.GetState<AskReferenceState>().Types.ShouldBeEmpty();
  }

  public static async Task Filter_Types_Then_Search_Then_Choose()
  {
    using ReferenceSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    // @ -> the type list, narrowed by the typed term.
    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.FilterActionSet.Action("feed"));
    AskReferenceState state = scope.Store.GetState<AskReferenceState>();
    state.VisibleTypes.Select(static type => type.Prefix).ShouldBe(["feedback", "my-feedback"]);
    state.HighlightedIndex.ShouldBe(0);
    await scope.Send(new AskReferenceState.MoveHighlightActionSet.Action(1));
    scope.Store.GetState<AskReferenceState>().HighlightedType!.Prefix.ShouldBe("my-feedback");

    // Choosing a type lists its items across the site, not the page.
    await scope.Send(new AskReferenceState.ChooseTypeActionSet.Action("feedback"));
    state = scope.Store.GetState<AskReferenceState>();
    state.SelectedType!.Prefix.ShouldBe("feedback");
    state.Items.Select(static item => item.Label).ShouldBe(["Mock bug report", "Mock feature request"]);
    state.VisibleTypes.ShouldBeEmpty();

    // Search-as-you-type runs the query with the term.
    await scope.Send(new AskReferenceState.FilterActionSet.Action("bug"));
    state = scope.Store.GetState<AskReferenceState>();
    state.IsSearching.ShouldBeFalse();
    ResourceReferenceItem bug = state.Items.ShouldHaveSingleItem();
    bug.Token.ShouldBe("@feedback:11111111-1111-1111-1111-111111111111");
    bug.IdTypeName.ShouldBe("Guid");
    state.HighlightedItem.ShouldBe(bug);

    await scope.Send(new AskReferenceState.ChooseActionSet.Action(bug));
    state = scope.Store.GetState<AskReferenceState>();
    state.IsOpen.ShouldBeFalse();
    state.SelectedType.ShouldBeNull();
    state.Chosen.ShouldBe([bug]);
  }

  public static async Task Search_A_Members_Own_Credentials_By_Display_Name()
  {
    using ReferenceSpa spa = new(Member);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.ChooseTypeActionSet.Action("credential"));
    AskReferenceState state = scope.Store.GetState<AskReferenceState>();
    // The nickname labels a renamed passkey; an unnamed one falls back to its provider label.
    state.Items.Select(static item => item.Label).ShouldBe(["Work laptop", "1Password"]);
    state.Items.ShouldAllBe(static item => item.IdTypeName == "CredentialId" && item.Token.StartsWith("@credential:"));

    await scope.Send(new AskReferenceState.FilterActionSet.Action("laptop"));
    scope.Store.GetState<AskReferenceState>().Items.ShouldHaveSingleItem().Label.ShouldBe("Work laptop");
  }

  public static async Task Choose_A_Type_By_Typing_Its_Prefix_And_Go_Back()
  {
    using ReferenceSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.FilterActionSet.Action("role:"));
    AskReferenceState state = scope.Store.GetState<AskReferenceState>();
    state.SelectedType!.Prefix.ShouldBe("role");
    state.Term.ShouldBe("");
    state.Items.Select(static item => item.Label).ShouldContain("Administrator");

    await scope.Send(new AskReferenceState.BackActionSet.Action());
    state = scope.Store.GetState<AskReferenceState>();
    state.SelectedType.ShouldBeNull();
    state.Items.ShouldBeEmpty();
    state.VisibleTypes.Count.ShouldBe(state.Types.Count);

    await scope.Send(new AskReferenceState.CloseActionSet.Action());
    scope.Store.GetState<AskReferenceState>().IsOpen.ShouldBeFalse();
  }

  public static async Task Resolve_A_Chosen_Token_To_Its_Typed_Id_In_Page_Context()
  {
    using ReferenceSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.ChooseTypeActionSet.Action("principal"));
    ResourceReferenceItem ada = scope.Store.GetState<AskReferenceState>().Items
      .Single(static item => item.Label == "Ada Lovelace");
    await scope.Send(new AskReferenceState.ChooseActionSet.Action(ada));

    // The token's id parses as the declared typed id.
    PrincipalId.Parse(ada.Id, provider: null).ShouldBe(PrincipalId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")));

    string context = PageAgentContext.Describe(scope.Store, "/");
    using (JsonDocument document = JsonDocument.Parse(context))
    {
      JsonElement reference = document.RootElement.GetProperty("references").EnumerateArray().ShouldHaveSingleItem();
      reference.GetProperty("token").GetString().ShouldBe("@principal:11111111-1111-1111-1111-111111111111");
      reference.GetProperty("type").GetString().ShouldBe("principal");
      reference.GetProperty("idType").GetString().ShouldBe(nameof(PrincipalId));
      reference.GetProperty("id").GetString().ShouldBe("11111111-1111-1111-1111-111111111111");
      reference.GetProperty("label").GetString().ShouldBe("Ada Lovelace");
    }

    AgentAskInstructions.Preface.ShouldContain("page_context.references");

    // New conversation: the earlier references no longer resolve.
    await scope.Send(new AgentSurfaceState.NewConversationActionSet.Action());
    using (JsonDocument document = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/")))
    {
      document.RootElement.GetProperty("references").GetArrayLength().ShouldBe(0);
    }
  }

  public static async Task Forget_Types_And_Chosen_References_When_The_Principal_Changes()
  {
    using ReferenceSpa spa = new(Administrator);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    string principalId = AskReferenceState.PrincipalIdOf(spa.User);
    principalId.ShouldNotBeNullOrEmpty();
    await scope.Send(new AskReferenceState.SessionChangedActionSet.Action(principalId));

    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.ChooseTypeActionSet.Action("principal"));
    ResourceReferenceItem ada = scope.Store.GetState<AskReferenceState>().Items
      .Single(static item => item.Label == "Ada Lovelace");
    await scope.Send(new AskReferenceState.ChooseActionSet.Action(ada));
    await scope.Send(new AskReferenceState.OpenActionSet.Action());

    // A change notification for the same principal keeps the conversation's references.
    await scope.Send(new AskReferenceState.SessionChangedActionSet.Action(principalId));
    AskReferenceState state = scope.Store.GetState<AskReferenceState>();
    state.Chosen.ShouldBe([ada]);
    state.IsOpen.ShouldBeTrue();

    // Sign-out: the picker closes and the previous user's types and labels are gone.
    await scope.Send(new AskReferenceState.SessionChangedActionSet.Action(AskReferenceState.PrincipalIdOf(new ClaimsPrincipal(new ClaimsIdentity()))));
    state = scope.Store.GetState<AskReferenceState>();
    state.IsOpen.ShouldBeFalse();
    state.Types.ShouldBeEmpty();
    state.Chosen.ShouldBeEmpty();
    state.SessionPrincipalId.ShouldBeEmpty();
    using (JsonDocument document = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/")))
    {
      document.RootElement.GetProperty("references").GetArrayLength().ShouldBe(0);
    }

    // Sign-in as someone else also starts clean.
    await scope.Send(new AskReferenceState.OpenActionSet.Action());
    await scope.Send(new AskReferenceState.SessionChangedActionSet.Action(Guid.NewGuid().ToString()));
    state = scope.Store.GetState<AskReferenceState>();
    state.IsOpen.ShouldBeFalse();
    state.Types.ShouldBeEmpty();
  }

  private sealed class ReferenceSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ClaimsPrincipal User { get; }

    public ReferenceSpa(string[]? permissions)
    {
      User = permissions is null
        ? new ClaimsPrincipal(new ClaimsIdentity())
        : new ClaimsPrincipal(new ClaimsIdentity(
          [
            new Claim("sub", Guid.NewGuid().ToString()),
            .. permissions.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
          ],
          authenticationType: "test"));

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
      services.AddScoped(_ => A.Fake<IJSRuntime>());
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
