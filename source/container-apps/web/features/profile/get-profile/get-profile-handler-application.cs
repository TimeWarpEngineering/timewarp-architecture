#region Purpose
// Server-side handler for the GetProfile query: dual-mode anonymous mock vs store-backed profile.
#endregion

#region Design
// Task 148:
//   - Anonymous (no UserId): contract mock so demo works without sign-in (D3).
//   - Authenticated: IProfileStore Find; create-if-missing with ProfileId.From(UserId) and defaults
//     Member / en-US / US / system / Notifications=false (D1). Concurrent create races throw on
//     unique PK / TryAdd → re-find. Application never takes PostgresDbContext (D4).
//   - Response: Alias ← DisplayName; Language/Region/Theme/Notifications from aggregate; Avatar
//     from multiavatar with resilient fallback (never throws on network fail; not in domain/EF) (D2, D6).
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Features.Profiles.Domain;
using static TimeWarp.Architecture.Features.Profiles.GetProfile;

public sealed partial class GetProfile
{
  public sealed partial class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private const string DefaultDisplayName = "Member";
    private const string DefaultLanguage = "en-US";
    private const string DefaultRegion = "US";
    private const string DefaultTheme = "system";

    private readonly ICurrentUserService CurrentUserService;
    private readonly IProfileStore ProfileStore;
    private readonly HttpClient HttpClient;
    private readonly ILogger<Handler> Logger;

    public Handler(
      ICurrentUserService currentUserService,
      IProfileStore profileStore,
      HttpClient httpClient,
      ILogger<Handler> logger)
    {
      CurrentUserService = currentUserService;
      ProfileStore = profileStore;
      HttpClient = httpClient;
      Logger = logger;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      Guid? userId = CurrentUserService.UserId;
      if (userId is null)
      {
        return GetMockResponseFactory()(request);
      }

      var profileId = ProfileId.From(userId.Value);
      Profile? profile = await ProfileStore.FindAsync(profileId, cancellationToken).ConfigureAwait(false);

      if (profile is null)
      {
        profile = Profile.Create(
          profileId,
          DefaultDisplayName,
          DefaultLanguage,
          DefaultRegion,
          DefaultTheme);

        try
        {
          await ProfileStore.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
          // Concurrent create won the race (unique PK / TryAdd) — re-find the winner's row.
          profile = await ProfileStore.FindAsync(profileId, cancellationToken).ConfigureAwait(false);
          if (profile is null)
          {
            throw new InvalidOperationException(
              $"Profile '{profileId}' create raced but re-find returned null.");
          }
        }
      }

      string avatar = await GetAvatarDataUriAsync(userId.Value, cancellationToken).ConfigureAwait(false);

      return new Response(
        alias: profile.DisplayName,
        language: profile.Language,
        region: profile.Region,
        theme: profile.Theme,
        notifications: profile.Notifications,
        avatar: avatar);
    }

    private async Task<string> GetAvatarDataUriAsync(Guid userId, CancellationToken cancellationToken)
    {
      try
      {
        string avatarUrl = $"https://api.multiavatar.com/{userId}.svg";
        byte[] imageBytes = await HttpClient
          .GetByteArrayAsync(avatarUrl, cancellationToken)
          .ConfigureAwait(false);
        string base64 = Convert.ToBase64String(imageBytes);
        return $"data:image/svg+xml;base64,{base64}";
      }
      catch (Exception exception) when (
        exception is HttpRequestException
        or IOException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
      {
        // Task 148 D6: never fail GetProfile because multiavatar is down / blocked / timed out.
        // Real caller cancellation (token cancelled) is not swallowed.
        LogMultiavatarFailed(Logger, exception, userId);
        return BuildFallbackAvatarDataUri(userId);
      }
    }

    /// <summary>
    /// Deterministic, network-free SVG data URI so Avatar is always a non-empty image source.
    /// </summary>
    internal static string BuildFallbackAvatarDataUri(Guid userId)
    {
      // Stable color from user id bytes so chrome does not flash a new color each request.
      byte[] bytes = userId.ToByteArray();
      int r = bytes[0];
      int g = bytes[1];
      int b = bytes[2];
      string hex = $"{r:X2}{g:X2}{b:X2}";
      const string initials = "U";
      string svg =
        $"""<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48"><rect width="48" height="48" fill="#{hex}"/><text x="24" y="30" text-anchor="middle" fill="#fff" font-size="18" font-family="sans-serif">{initials}</text></svg>""";
      string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
      return $"data:image/svg+xml;base64,{base64}";
    }

    [LoggerMessage(
      EventId = 14801,
      Level = LogLevel.Warning,
      Message = "Multiavatar fetch failed for user {UserId}; using deterministic fallback SVG.")]
    private static partial void LogMultiavatarFailed(ILogger logger, Exception exception, Guid userId);
  }
}
