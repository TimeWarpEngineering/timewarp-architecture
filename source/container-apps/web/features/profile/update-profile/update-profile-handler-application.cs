#region Purpose
// Server-side handler for UpdateProfile: create-if-missing then apply named mutations. Never a register/session gate.
#endregion

#region Design
// Task 205: progressive profile after the principal exists. Same create-if-missing defaults as
// GetProfile so a first PUT on a principal with no profile row still succeeds (the kernel never
// required a profile to register or issue a session). Application takes IProfileStore, not
// PostgresDbContext. Identity from ICurrentPrincipalAccessor. Does not write
// TimeWarp.Identity.Principal.DisplayName — identity kernel stays credentials/trust.
#endregion

namespace TimeWarp.Architecture.Features.Profiles.Application;

using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features.Profiles.Domain;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Profiles.UpdateProfile;

public sealed class UpdateProfile
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private const string DefaultDisplayName = "Member";
    private const string DefaultLanguage = "en-US";
    private const string DefaultRegion = "US";
    private const string DefaultTheme = "system";

    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IProfileStore ProfileStore;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IProfileStore profileStore)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      ProfileStore = profileStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command command,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken).ConfigureAwait(false);
      if (principalId is null)
      {
        return ProfileProblems.Unauthenticated();
      }

      var profileId = ProfileId.From(principalId.Value.Value);
      Profile? profile = await ProfileStore.FindAsync(profileId, cancellationToken).ConfigureAwait(false);
      bool created = false;
      if (profile is null)
      {
        profile = Profile.Create(
          profileId,
          DefaultDisplayName,
          DefaultLanguage,
          DefaultRegion,
          DefaultTheme);
        created = true;
      }

      profile.Rename(command.Alias);
      profile.SetEmail(command.Email);
      profile.SetLanguage(command.Language);
      profile.SetRegion(command.Region);
      profile.SetTheme(command.Theme);
      if (command.Notifications)
      {
        profile.EnableNotifications();
      }
      else
      {
        profile.DisableNotifications();
      }

      if (created)
      {
        try
        {
          await ProfileStore.AddAsync(profile, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
          Profile? winner = await ProfileStore.FindAsync(profileId, cancellationToken).ConfigureAwait(false);
          if (winner is null)
          {
            throw new InvalidOperationException(
              $"Profile '{profileId}' create raced but re-find returned null.");
          }

          winner.Rename(command.Alias);
          winner.SetEmail(command.Email);
          winner.SetLanguage(command.Language);
          winner.SetRegion(command.Region);
          winner.SetTheme(command.Theme);
          if (command.Notifications)
          {
            winner.EnableNotifications();
          }
          else
          {
            winner.DisableNotifications();
          }

          await ProfileStore.UpdateAsync(winner, cancellationToken).ConfigureAwait(false);
          return ToResponse(winner);
        }
      }
      else
      {
        await ProfileStore.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
      }

      return ToResponse(profile);
    }

    private static Response ToResponse(Profile profile) =>
      new(
        alias: profile.DisplayName,
        email: profile.Email,
        language: profile.Language,
        region: profile.Region,
        theme: profile.Theme,
        notifications: profile.Notifications);
  }
}
