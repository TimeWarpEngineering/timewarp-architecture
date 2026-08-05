#region Purpose
// Server-side handler for the GetProfile query: dual-mode anonymous mock vs store-backed profile.
#endregion

#region Design
// Task 148:
//   - Anonymous (no UserId): contract mock so demo works without sign-in (D3).
//   - Authenticated: IProfileStore Find; create-if-missing with ProfileId.From(UserId) and defaults
//     Member / en-US / US / system / Notifications=false (D1). Concurrent create races throw on
//     unique PK / TryAdd → re-find. Application never takes PostgresDbContext (D4).
//   - Response: Alias ← DisplayName; Language/Region/Theme/Notifications from aggregate.
// Task 149: Avatar is local TimeWarp.Multiavatar (MultiavatarGenerator.Generate), seeded by
// user id — no network, no HttpClient, no fallback SVG path. Not domain/EF.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using System.Text;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Multiavatar;
using static TimeWarp.Architecture.Features.Profiles.GetProfile;

public sealed class GetProfile
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private const string DefaultDisplayName = "Member";
    private const string DefaultLanguage = "en-US";
    private const string DefaultRegion = "US";
    private const string DefaultTheme = "system";

    private readonly ICurrentUserService CurrentUserService;
    private readonly IProfileStore ProfileStore;

    public Handler(
      ICurrentUserService currentUserService,
      IProfileStore profileStore)
    {
      CurrentUserService = currentUserService;
      ProfileStore = profileStore;
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

      string avatar = GetAvatarDataUri(userId.Value);

      return new Response(
        alias: profile.DisplayName,
        language: profile.Language,
        region: profile.Region,
        theme: profile.Theme,
        notifications: profile.Notifications,
        avatar: avatar);
    }

    /// <summary>
    /// Deterministic Multiavatar SVG data URI for the user id (task 149). Local generation only.
    /// </summary>
    internal static string GetAvatarDataUri(Guid userId)
    {
      string svg = MultiavatarGenerator.Generate(userId.ToString("D"));
      string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
      return $"data:image/svg+xml;base64,{base64}";
    }
  }
}
