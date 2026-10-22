#region Purpose
// Index math for Tabs and SegmentedControl: next, previous, wrap, Home, End, and an empty list.
#endregion

#region Design
// The component never stores the selection. This type only answers which index a key lands on
// and whether that item differs from the current value, so the component can raise ValueChanged
// without assigning Value. A null or unknown value selects nothing; the first item is still the
// roving tab stop (tabindex 0). Left and Right start at that tab stop and then step, so the move
// matches the focused item. Home and End ignore the current index. An empty list has no index.
// Landing on the current item does not raise. Comparison is ordinal. Unknown keys are not moves.
// HandledKeys is the only key list. CancelScript is built from it, and FromKey returns a move
// only for a key on that list, so the browser cancel and the C# handler stay the same set.
// The script also requires Alt, Ctrl, Meta, and Shift to be up, matching OnKeyDownAsync.
#endregion

namespace TimeWarp.Architecture.Components;

internal static class RovingSelection
{
  internal readonly record struct MoveResult(int Index, string Item, bool Raise);

  internal const string ArrowLeftKey = "ArrowLeft";
  internal const string ArrowRightKey = "ArrowRight";
  internal const string HomeKey = "Home";
  internal const string EndKey = "End";

  internal static readonly IReadOnlyList<string> HandledKeys =
  [
    ArrowLeftKey,
    ArrowRightKey,
    HomeKey,
    EndKey
  ];

  internal static readonly string CancelScript = BuildCancelScript();

  public static int Next(int count, int current) => Step(count, current, 1);

  public static int Previous(int count, int current) => Step(count, current, -1);

  public static int Home(int count) => count <= 0 ? -1 : 0;

  public static int End(int count) => count <= 0 ? -1 : count - 1;

  public static int IndexOf(IReadOnlyList<string>? items, string? value)
  {
    if (items is null || value is null)
    {
      return -1;
    }

    for (int index = 0; index < items.Count; index++)
    {
      if (string.Equals(items[index], value, StringComparison.Ordinal))
      {
        return index;
      }
    }

    return -1;
  }

  public static int TabStop(int count, int selected)
  {
    if (count <= 0)
    {
      return -1;
    }

    if ((uint)selected < (uint)count)
    {
      return selected;
    }

    return 0;
  }

  public static MoveResult? FromKey(IReadOnlyList<string>? items, string? value, string? key)
  {
    if (items is null || items.Count == 0 || !IsHandled(key))
    {
      return null;
    }

    int index = key switch
    {
      ArrowRightKey => Next(items.Count, IndexOf(items, value)),
      ArrowLeftKey => Previous(items.Count, IndexOf(items, value)),
      HomeKey => Home(items.Count),
      EndKey => End(items.Count),
      _ => -1
    };

    if (index < 0)
    {
      return null;
    }

    string item = items[index];
    bool raise = !string.Equals(item, value, StringComparison.Ordinal);
    return new MoveResult(index, item, raise);
  }

  private static bool IsHandled(string? key)
  {
    if (key is null)
    {
      return false;
    }

    for (int index = 0; index < HandledKeys.Count; index++)
    {
      if (string.Equals(HandledKeys[index], key, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static string BuildCancelScript()
  {
    string[] clauses = new string[HandledKeys.Count];
    for (int index = 0; index < HandledKeys.Count; index++)
    {
      clauses[index] = "event.key==='" + HandledKeys[index] + "'";
    }

    string keys = string.Join("||", clauses);
    return "if(!event.altKey&&!event.ctrlKey&&!event.metaKey&&!event.shiftKey&&(" + keys + "))event.preventDefault()";
  }

  private static int Step(int count, int current, int delta)
  {
    if (count <= 0)
    {
      return -1;
    }

    int start = (uint)current < (uint)count ? current : 0;
    int next = start + delta;
    if (next >= count)
    {
      return 0;
    }

    if (next < 0)
    {
      return count - 1;
    }

    return next;
  }
}
