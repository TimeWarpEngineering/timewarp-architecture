#region Purpose
// Server-side handler for GetAssistantIdentity: the stored assistant name and its Multiavatar.
#endregion

#region Design
// Reads ISiteSettingsStore (seed-on-read, task 254). A null read means the table is not migrated
// yet; the label then falls back to the default name rather than failing every page's Ask button.
// AvatarFor is the one place the avatar is made: MultiavatarGenerator.Generate seeded by the name,
// as GetProfile seeds by user id (task 149), so the same name always gets the same face.
// The endpoint is anonymous and called on every app load, and the avatar is a pure function of the
// name, so AvatarFor memoizes the last result. There is only one current name, so a single-entry
// cache suffices: an immutable (name, avatar) record in a static field, replaced with one atomic
// reference write. Concurrent misses may both generate, but they produce the same value, so either
// write is correct and no lock is needed.
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

  private static AvatarEntry? LastAvatar;

  /// <summary>Deterministic Multiavatar SVG data URI seeded by the assistant name.</summary>
  public static string AvatarFor(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    AvatarEntry? last = Volatile.Read(ref LastAvatar);
    if (last is not null && string.Equals(last.Name, name, StringComparison.Ordinal))
    {
      return last.Avatar;
    }

    string svg = MultiavatarGenerator.Generate(name);
    string avatar = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    Volatile.Write(ref LastAvatar, new AvatarEntry(name, avatar));
    return avatar;
  }

  /// <summary>One memoized avatar: the name it was made from and its data URI.</summary>
  private sealed record AvatarEntry(string Name, string Avatar);
}
