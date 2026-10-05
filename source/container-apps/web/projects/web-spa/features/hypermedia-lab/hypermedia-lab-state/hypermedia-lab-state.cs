#region Purpose
// SPA state for the hypermedia lab (task 275): the latest server payload for approach B (offers) and approach C (link commands), and the open tab.
#endregion

#region Design
// Evaluation-only slice: removed, or folded into Identity, once Steve decides B / C / neither.
// The page renders only from these payloads — a button exists only for an offer/command in the
// latest payload, and the Ctrl-K rows come from the same payloads (HypermediaLabContextSource), so
// anything the server did not offer has no button, no row and no other way to run. Null = not
// loaded yet. Each fetch replaces its payload wholesale; nothing is merged or computed client-side.
// ICloneable: the payloads are immutable contract objects (they hold JsonElement arguments, which a
// generic deep clone should not walk), so a clone shares them — the same reasoning as the
// string-only palette rows.
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

[StateAccess]
public sealed partial class HypermediaLabState : State<HypermediaLabState>, ICloneable
{
  public const string ApproachB = "B";
  public const string ApproachC = "C";

  /// <summary>Approach B payload; null until fetched.</summary>
  public GetCredentialOffers.Response? Offers { get; private set; }

  /// <summary>Approach C payload; null until fetched.</summary>
  public GetCredentialCommands.Response? Commands { get; private set; }

  /// <summary><see cref="ApproachB"/> or <see cref="ApproachC"/>.</summary>
  public string SelectedTab { get; private set; } = ApproachB;

  public HypermediaLabState() { }

  public override void Initialize()
  {
    Offers = null;
    Commands = null;
    SelectedTab = ApproachB;
  }

  public object Clone() => MemberwiseClone();
}
