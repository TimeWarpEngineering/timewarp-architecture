#region Purpose
// ProfileState keeps one GetProfile.Response. Fetch stores it. Update rebuilds it and keeps the avatar.
#endregion

#region Design
// In-proc SPA with a scripted IWebServerApiService, the same shape as the assistant-identity
// facts. No Aspire host: the store is what these facts observe. The working copy is
// UpdateProfile.Command.From on the stored response, and a DevTools snapshot of the populated
// state goes through StateJsonContext. Re-read the store after Send (clone-on-dispatch).
// A prefill stores JSON and an id in FormSuggestionState under the submit action's key.
// Confirm asks through IConfirmationService. The question
// says the draft replaces unsaved changes on the form. Apply copies only the supplied
// fields and does not move the baseline. An empty argument list and a set of unknown keys
// are not a draft.
#endregion

namespace ProfileState_;

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;

[TestTag("Integration")]
public class FetchAndUpdate_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FetchAndUpdate_Should_>();

  public static async Task Fetch_Should_StoreTheWholeResponse()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new ProfileState.FetchProfileDataActionSet.Action());

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe(spa.Api.Fetched.DisplayName);
    stored.Email.ShouldBeNull();
    stored.Language.ShouldBe("en-US");
    stored.Region.ShouldBe("US");
    stored.Theme.ShouldBe("system");
    stored.Notifications.ShouldBeFalse();
    stored.Avatar.ShouldBe(ScriptedProfileApi.FetchedAvatar);

    UpdateProfile.Command copy = UpdateProfile.Command.From(stored);
    copy.DisplayName.ShouldBe("Ada");
    copy.Language.ShouldBe("en-US");
    copy.DisplayName = "draft";
    scope.Store.GetState<ProfileState>().Profile!.DisplayName.ShouldBe("Ada");

    string json = JsonSerializer.Serialize(
      scope.Store.GetState<ProfileState>(),
      StateJsonContext.Default.ProfileState);
    json.ShouldContain("Ada");
  }

  public static async Task Update_Should_KeepTheFetchedAvatar()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new ProfileState.FetchProfileDataActionSet.Action());

    await scope.Send(new ProfileState.UpdateProfileActionSet.Action(new UpdateProfile.Command
    {
      DisplayName = "typed-name",
      Email = "typed@example.com",
      Language = "en-US",
      Region = "US",
      Theme = "system",
      Notifications = false,
    }));

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe("Ada Lovelace");
    stored.Email.ShouldBe("ada@example.com");
    stored.Language.ShouldBe("fr-FR");
    stored.Region.ShouldBe("FR");
    stored.Theme.ShouldBe("dark");
    stored.Notifications.ShouldBeTrue();
    stored.Avatar.ShouldBe(ScriptedProfileApi.FetchedAvatar);
  }

  public static async Task Update_WithNoSnapshot_Should_UseTheFallbackAvatar()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.Store.GetState<ProfileState>().Profile.ShouldBeNull();

    await scope.Send(new ProfileState.UpdateProfileActionSet.Action(new UpdateProfile.Command
    {
      DisplayName = "typed-name",
      Language = "en-US",
      Region = "US",
      Theme = "system",
    }));

    GetProfile.Response stored = scope.Store.GetState<ProfileState>().Profile.ShouldNotBeNull();
    stored.DisplayName.ShouldBe("Ada Lovelace");
    stored.Avatar.ShouldBe(ProfileState.FallbackAvatar);
  }

  public static async Task Suggestion_Should_StoreClearAndAsk()
  {
    using ProfileSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    string form = FormSuggestionState.KeyFor(typeof(ProfileState.UpdateProfileActionSet.Action));

    await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, """{"displayName":"Ada"}"""));
    FormSuggestion stored = scope.Store.GetState<FormSuggestionState>().For(form).ShouldNotBeNull();
    stored.Json.ShouldContain("Ada");
    string firstId = stored.Id.ShouldNotBeNull();
    stored.Accepted.ShouldBeFalse();
    scope.Store.GetState<ProfileState>().Profile.ShouldBeNull();

    spa.Confirmation.Answer = true;
    await scope.Send(new FormSuggestionState.ConfirmFormSuggestionActionSet.Action(form, "profile"));
    stored = scope.Store.GetState<FormSuggestionState>().For(form).ShouldNotBeNull();
    stored.Accepted.ShouldBeTrue();
    stored.Id.ShouldBe(firstId);
    spa.Confirmation.Title.ShouldBe("Replace unsaved edits?");
    spa.Confirmation.Message.ShouldBe(
      "An agent drafted new profile values. Applying them replaces unsaved changes on this form.");
    spa.Confirmation.ConfirmLabel.ShouldBe("Apply draft");
    spa.Confirmation.CancelLabel.ShouldBe("Keep mine");

    await scope.Send(new FormSuggestionState.ClearFormSuggestionActionSet.Action(form, firstId));
    scope.Store.GetState<FormSuggestionState>().For(form).ShouldBeNull();

    await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, """{"displayName":"Grace"}"""));
    spa.Confirmation.Answer = false;
    await scope.Send(new FormSuggestionState.ConfirmFormSuggestionActionSet.Action(form, "profile"));
    scope.Store.GetState<FormSuggestionState>().For(form).ShouldBeNull();

    await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, """{"displayName":"Ada"}"""));
    firstId = scope.Store.GetState<FormSuggestionState>().For(form).ShouldNotBeNull().Id;
    spa.Confirmation.Answer = true;
    spa.Confirmation.During = async () =>
      await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, """{"displayName":"Grace"}"""));
    await scope.Send(new FormSuggestionState.ConfirmFormSuggestionActionSet.Action(form, "profile"));
    stored = scope.Store.GetState<FormSuggestionState>().For(form).ShouldNotBeNull();
    stored.Accepted.ShouldBeFalse();
    stored.Json.ShouldContain("Grace");
    stored.Id.ShouldNotBe(firstId);

    // Clearing an older id leaves the newer draft; another form's draft is separate.
    await scope.Send(new FormSuggestionState.ClearFormSuggestionActionSet.Action(form, firstId));
    scope.Store.GetState<FormSuggestionState>().For(form).ShouldNotBeNull().Json.ShouldContain("Grace");
    await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action("Other.Form", """{"name":"x"}"""));
    await scope.Send(new FormSuggestionState.ClearFormSuggestionActionSet.Action(form, stored.Id));
    scope.Store.GetState<FormSuggestionState>().For(form).ShouldBeNull();
    scope.Store.GetState<FormSuggestionState>().For("Other.Form").ShouldNotBeNull();
  }

  public static Task Apply_Should_CopyOnlySuppliedFields_And_LeaveTheBaseline()
  {
    UpdateProfile.Command command = new()
    {
      DisplayName = "Ada",
      Email = "ada@example.com",
      Language = "en-US",
      Region = "US",
      Theme = "system",
      Notifications = true,
    };
    string baseline = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
    Dictionary<string, string> filled = new(StringComparer.Ordinal);

    FormPrefill.TryNormalize(
      typeof(IProfileDetails),
      """{"displayName":"Grace","email":null,"nope":1}""",
      out string suggestion,
      out string? error,
      out IReadOnlyList<string> fields).ShouldBeTrue(error);
    fields.ShouldBe(["displayName", "email"]);
    suggestion.ShouldNotContain("nope");

    FormPrefill.Apply(command, suggestion, filled);
    command.DisplayName.ShouldBe("Grace");
    command.Email.ShouldBeNull();
    command.Notifications.ShouldBeTrue();
    command.Language.ShouldBe("en-US");
    JsonSerializer.Serialize(command, ContractSerializationDefaults.Options).ShouldNotBe(baseline);
    baseline.ShouldContain("Ada");
    FormPrefill.Matches(command, nameof(UpdateProfile.Command.DisplayName), filled["DisplayName"]).ShouldBeTrue();
    command.DisplayName = "Ada";
    FormPrefill.Matches(command, nameof(UpdateProfile.Command.DisplayName), filled["DisplayName"]).ShouldBeFalse();

    FormPrefill.TryNormalize(
      typeof(IProfileDetails),
      """{"displayName":null}""",
      out _,
      out error,
      out _).ShouldBeFalse();
    error.ShouldNotBeNull().ShouldContain("cannot be null");

    FormPrefill.TryNormalize(
      typeof(IProfileDetails),
      """{"notifications":"yes"}""",
      out _,
      out error,
      out _).ShouldBeFalse();
    error.ShouldNotBeNull().ShouldContain("boolean");

    FormPrefill.TryNormalize(
      typeof(IProfileDetails),
      "",
      out suggestion,
      out error,
      out fields).ShouldBeFalse();
    suggestion.ShouldBeEmpty();
    error.ShouldNotBeNull().ShouldContain("Accepted keys");
    error.ShouldContain("displayName");
    error.ShouldContain("email");
    error.ShouldContain("language");
    error.ShouldContain("region");
    error.ShouldContain("theme");
    error.ShouldContain("notifications");
    fields.ShouldBeEmpty();

    FormPrefill.TryNormalize(
      typeof(IProfileDetails),
      """{"nope":1}""",
      out suggestion,
      out error,
      out fields).ShouldBeFalse();
    suggestion.ShouldBeEmpty();
    error.ShouldNotBeNull().ShouldContain("Accepted keys");
    fields.ShouldBeEmpty();
    return Task.CompletedTask;
  }

  private sealed class ProfileSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedProfileApi Api { get; } = new();
    public RecordingConfirmation Confirmation { get; } = new();

    public ProfileSpa()
    {
      ServiceCollection services = new();
      services.AddLogging();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.RethrowHandlerExceptions = true;
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddSingleton<IWebServerApiService>(Api);
      services.AddSingleton<IConfirmationService>(Confirmation);
      services.AddScoped<NavigationManager, IdleNavigationManager>();
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

  private sealed class ScriptedProfileApi : IWebServerApiService
  {
    public const string FetchedAvatar = "data:image/svg+xml;base64,fetched";

    public GetProfile.Response Fetched { get; } = new(
      displayName: "Ada",
      email: null,
      language: "en-US",
      region: "US",
      theme: "system",
      notifications: false,
      avatar: FetchedAvatar);

    public UpdateProfile.Response Updated { get; } = new(
      displayName: "Ada Lovelace",
      email: "ada@example.com",
      language: "fr-FR",
      region: "FR",
      theme: "dark",
      notifications: true);

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      TrackEvent.Response tracked = new();
      if (tracked is TResponse trackedResponse)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(trackedResponse);
      }

      object body = request is UpdateProfile.Command ? Updated : Fetched;
      if (body is TResponse response)
      {
        return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(response);
      }

      SharedProblemDetails problem = new()
      {
        Status = 500,
        Title = "No scripted response for " + typeof(TResponse).Name + ".",
      };
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(problem);
    }
  }

  private sealed class RecordingConfirmation : IConfirmationService
  {
    public bool Answer { get; set; } = true;

    public Func<Task>? During { get; set; }

    public string? Title { get; private set; }

    public string? Message { get; private set; }

    public string? ConfirmLabel { get; private set; }

    public string? CancelLabel { get; private set; }

    public async Task<bool> ConfirmAsync
    (
      string title,
      string message,
      string confirmLabel,
      string cancelLabel,
      CancellationToken cancellationToken
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      Title = title;
      Message = message;
      ConfirmLabel = confirmLabel;
      CancelLabel = cancelLabel;
      if (During is not null)
      {
        await During();
      }

      return Answer;
    }
  }

  private sealed class IdleNavigationManager : NavigationManager
  {
    public IdleNavigationManager()
    {
      Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
  }
}
