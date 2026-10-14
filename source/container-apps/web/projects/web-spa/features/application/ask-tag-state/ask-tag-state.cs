#region Purpose
// Ask Tag state: whether pick mode is on, the elements attached to the next message (composer
// chips), and the ones already sent in the conversation.
#endregion

#region Design
// Task 346. Tag (StartPick) arms pick mode in ask-tag.ts, turns IsPicking on once it is armed
// (task 351), and waits for the answer; the chosen element
// becomes an AskTag chip (Choose runs the same Attach). Remove drops a chip before sending.
// MarkSent runs when a message carrying the chips is sent (AskTagChatClient): they move to Sent,
// which page_context.tags publishes so later turns can still refer to them. Attached and Sent
// are kept with the ConversationGeneration they belong to, so New conversation starts clean, and
// SessionChanged clears everything when the principal changes, so one user's tags never reach the
// next. Attach trusts nothing from the browser: every field is whitespace-squashed and capped, an
// element without a kind is refused, and a reference survives only when its prefix is in the
// generated ResourceReferenceRegistry and the user passes that type's list policy (an element
// for a resource the user cannot list is attached without the reference). The same chip
// (kind, label, reference) is kept once, newest last, and at most AttachedCap wait for a send.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class AskTagState : State<AskTagState>
{
  public const int AttachedCap = 5;

  public const int SentCap = 20;

  public const int KindCap = 40;

  public const int LabelCap = 80;

  public const int TextCap = 300;

  public const int DataQaCap = 80;

  public const int LandmarkCap = 120;

  public const int RouteCap = 200;

  public const int ReferenceIdCap = 80;

  /// <summary>Pick mode is armed in the browser: a click or key now picks (the panel's "picking" line).</summary>
  public bool IsPicking { get; private set; }

  /// <summary>The token of the pick in flight (arming or armed), or null; a pick applies only while its token is current.</summary>
  public string? PickToken { get; private set; }

  /// <summary>A pick is arming or armed, so Tag cannot start another.</summary>
  public bool IsPickPending => PickToken is not null;

  /// <summary>Chips waiting for the next message in conversation <see cref="Generation"/>.</summary>
  public IReadOnlyList<AskTag> Attached { get; private set; } = [];

  /// <summary>Tags already sent in conversation <see cref="Generation"/>.</summary>
  public IReadOnlyList<AskTag> Sent { get; private set; } = [];

  public int Generation { get; private set; }

  public int NextId { get; private set; }

  /// <summary>Id claim of the principal the tags belong to; empty when signed out.</summary>
  public string SessionPrincipalId { get; private set; } = "";

  public IReadOnlyList<AskTag> AttachedFor(int conversationGeneration) =>
    Generation == conversationGeneration ? Attached : [];

  public IReadOnlyList<AskTag> SentFor(int conversationGeneration) =>
    Generation == conversationGeneration ? Sent : [];

  public AskTagState() { }

  public override void Initialize()
  {
    IsPicking = false;
    PickToken = null;
    Attached = [];
    Sent = [];
    Generation = 0;
    NextId = 0;
    SessionPrincipalId = "";
  }

  /// <summary>Squashes runs of whitespace and clips to <paramref name="cap"/> characters.</summary>
  public static string Clean(string? text, int cap)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return "";
    }

    string squashed = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    return squashed.Length <= cap ? squashed : squashed[..cap];
  }

  /// <summary>
  /// Builds the chip for <paramref name="candidate"/>, or null when it has no kind. The reference is
  /// kept only for a registered type <paramref name="user"/> may list.
  /// </summary>
  public static async Task<AskTag?> DescribeAsync
  (
    int id,
    AskTagCandidate candidate,
    string route,
    ClaimsPrincipal user,
    IAuthorizationService authorizationService
  )
  {
    ArgumentNullException.ThrowIfNull(candidate);
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    string kind = Clean(candidate.Kind, KindCap);
    if (kind.Length == 0)
    {
      return null;
    }

    string text = Clean(candidate.Text, TextCap);
    string label = Clean(candidate.Label, LabelCap);
    if (label.Length == 0)
    {
      label = Clean(text, LabelCap);
    }

    string? dataQa = NullIfEmpty(Clean(candidate.DataQa, DataQaCap));
    string? landmark = NullIfEmpty(Clean(candidate.Landmark, LandmarkCap));
    ResourceReferenceItem? reference = await ResolveReferenceAsync(candidate.Reference, label, user, authorizationService);
    return new AskTag(id, kind, label, text, dataQa, landmark, Clean(route, RouteCap), reference);
  }

  private static async Task<ResourceReferenceItem?> ResolveReferenceAsync
  (
    string? token,
    string label,
    ClaimsPrincipal user,
    IAuthorizationService authorizationService
  )
  {
    if (string.IsNullOrWhiteSpace(token) || !token.StartsWith('@', StringComparison.Ordinal))
    {
      return null;
    }

    int colon = token.IndexOf(':', StringComparison.Ordinal);
    if (colon < 2)
    {
      return null;
    }

    string prefix = token[1..colon];
    string id = token[(colon + 1)..];
    if (id.Length == 0 || id.Length > ReferenceIdCap || id.Any(char.IsWhiteSpace))
    {
      return null;
    }

    ResourceReferenceType? type = ResourceReferenceRegistry.Find(prefix);
    if (type is null || user.Identity?.IsAuthenticated != true)
    {
      return null;
    }

    if (type.Policy is not null && !(await authorizationService.AuthorizeAsync(user, type.Policy)).Succeeded)
    {
      return null;
    }

    return new ResourceReferenceItem(type.Prefix, id, label, type.IdTypeName);
  }

  private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

  /// <summary>Adds <paramref name="tag"/> to the chips of <paramref name="conversationGeneration"/>.</summary>
  private void Attach(AskTag tag, int conversationGeneration)
  {
    Rebase(conversationGeneration);
    List<AskTag> attached = [.. Attached];
    attached.RemoveAll
    (
      existing => existing.ChipText == tag.ChipText && existing.Reference?.Token == tag.Reference?.Token
    );
    attached.Add(tag);
    if (attached.Count > AttachedCap)
    {
      attached.RemoveRange(0, attached.Count - AttachedCap);
    }

    Attached = attached;
  }

  /// <summary>Drops chips and sent tags of an older conversation.</summary>
  private void Rebase(int conversationGeneration)
  {
    if (Generation == conversationGeneration)
    {
      return;
    }

    Generation = conversationGeneration;
    Attached = [];
    Sent = [];
  }

  /// <summary>Builds the next chip as the signed-in user and attaches it; a candidate without a kind is dropped.</summary>
  private static async Task AttachAsync
  (
    Func<AskTagState> state,
    int conversationGeneration,
    AskTagCandidate candidate,
    string route,
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService
  )
  {
    int id = state().NextId;
    state().NextId = id + 1;
#pragma warning disable BL0013
    AuthenticationState authentication = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    AskTag? tag = await DescribeAsync(id, candidate, route, authentication.User, authorizationService);
    if (tag is not null)
    {
      state().Attach(tag, conversationGeneration);
    }
  }
}
