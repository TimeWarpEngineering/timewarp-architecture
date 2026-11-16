#region Purpose
// Source-generated JSON type info for every web-spa TimeWarp.State state, so a State DevTools
// snapshot of any of them serializes without reflection.
#endregion

#region Design
// Task 484: analyzer TWS0013 (TimeWarp.State 12.0.0-beta.17) requires each state that telemetry
// snapshots serialize to be listed on a JsonSerializerContext in this project. web-spa references
// TimeWarp.State.Telemetry, so every state is listed here. A new state adds one [JsonSerializable]
// line here; the analyzer's code fix writes it. A state in a template-flag feature folder sits in
// the same template symbol as that folder's template.json exclude (grpc: Superheros, api: WeatherForecasts).
//
// The context is public so web-server (prerender and InteractiveServer stores) can wire it too.
// Both DevTools snapshot registrations (Program.AddStateDevToolsExportAsync and
// WebServerStateDevTools.AddExport) use StateJsonContext.Default first and fall back to the
// reflection resolver only for a state not listed here, such as a TimeWarp.State.Plus state.
// Persistence is not wired to this context: no web-spa state is [PersistentState].
#endregion

namespace TimeWarp.Architecture.Web.Spa;

using System.Text.Json.Serialization;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Ai;
using TimeWarp.Architecture.Features.Admin.Feedback;
using TimeWarp.Architecture.Features.Admin.Principals;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Authorization;
using TimeWarp.Architecture.Features.Chat;
using TimeWarp.Architecture.Features.Counters;
using TimeWarp.Architecture.Features.DesignSystem;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Architecture.Features.Settings;
#if grpc
using TimeWarp.Architecture.Features.Superheros;
#endif
#if api
using TimeWarp.Architecture.Features.WeatherForecasts;
#endif

/// <summary>JSON type info for the web-spa TimeWarp.State states (DevTools snapshots).</summary>
[JsonSerializable(typeof(AgentLinksState))]
[JsonSerializable(typeof(AgentSurfaceState))]
[JsonSerializable(typeof(AnalyticsState))]
[JsonSerializable(typeof(ApplicationState))]
[JsonSerializable(typeof(AskReferenceState))]
[JsonSerializable(typeof(AskTagState))]
[JsonSerializable(typeof(AuthorizationState))]
[JsonSerializable(typeof(ChatState))]
[JsonSerializable(typeof(CommandPaletteState))]
[JsonSerializable(typeof(CounterState))]
[JsonSerializable(typeof(CredentialsState))]
[JsonSerializable(typeof(DesignSystemState))]
[JsonSerializable(typeof(FeedbackAdminState))]
[JsonSerializable(typeof(FeedbackState))]
[JsonSerializable(typeof(NotificationState))]
[JsonSerializable(typeof(PrincipalState))]
[JsonSerializable(typeof(ProfileState))]
[JsonSerializable(typeof(RoleState))]
[JsonSerializable(typeof(ScreenshotState))]
[JsonSerializable(typeof(SignInState))]
[JsonSerializable(typeof(SiteSettingsState))]
#if grpc
[JsonSerializable(typeof(SuperheroState))]
#endif
[JsonSerializable(typeof(UserXaiApiKeyState))]
#if api
[JsonSerializable(typeof(WeatherForecastsState))]
#endif
[JsonSerializable(typeof(XaiApiKeyState))]
// Feedback contracts nest same-named types (Response, Item) that the states expose. Each one gets its
// own TypeInfoPropertyName, or source generation keeps only the first (SYSLIB1031).
[JsonSerializable(typeof(GetFeedback.Response), TypeInfoPropertyName = "GetFeedbackResponse")]
[JsonSerializable(typeof(SubmitFeedback.Response), TypeInfoPropertyName = "SubmitFeedbackResponse")]
[JsonSerializable(typeof(ListFeedback.Item), TypeInfoPropertyName = "ListFeedbackItem")]
[JsonSerializable(typeof(ListMyFeedback.Item), TypeInfoPropertyName = "ListMyFeedbackItem")]
[JsonSerializable(typeof(IReadOnlyList<ListFeedback.Item>), TypeInfoPropertyName = "ListFeedbackItems")]
[JsonSerializable(typeof(IReadOnlyList<ListMyFeedback.Item>), TypeInfoPropertyName = "ListMyFeedbackItems")]
public sealed partial class StateJsonContext : JsonSerializerContext;
