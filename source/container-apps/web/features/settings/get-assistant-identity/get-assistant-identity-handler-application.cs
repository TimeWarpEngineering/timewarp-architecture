#region Purpose
// Server-side handler for GetAssistantIdentity: the stored assistant name and its Multiavatar.
#endregion

#region Design
// Reads ISiteSettingsStore (seed-on-read, task 254). A null read means the table is not migrated
// yet; the label then falls back to the default name rather than failing every page's Ask button.
// AvatarFor is the one place the avatar is made: MultiavatarGenerator.Generate seeded by the name,
// as GetProfile seeds by user id (task 149), so the same name always gets the same face.
#endregion

namespace TimeWarp.Architecture.Features.Settings.Application;

using System.Text;
using TimeWarp.Identity;
using TimeWarp.Multiavatar;
using static TimeWarp.Architecture.Features.Settings.GetAssistantIdentity;

public sealed class GetAssistantIdentity
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ISiteSettingsStore SiteSettingsStore;

    public Handler(ISiteSettingsStore siteSettingsStore)
    {
      SiteSettingsStore = siteSettingsStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      _ = request;
      SiteSettings? settings = await SiteSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
      string name = settings?.AssistantName ?? AssistantNameRules.Default;
      return new Response(name, AvatarFor(name));
    }
  }

  /// <summary>Deterministic Multiavatar SVG data URI seeded by the assistant name.</summary>
  public static string AvatarFor(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    string svg = MultiavatarGenerator.Generate(name);
    return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
  }
}
