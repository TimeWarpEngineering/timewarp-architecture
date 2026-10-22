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
#endregion

namespace TimeWarp.Architecture.Components;

internal static class RovingSelection
{
  internal readonly record struct MoveResult(int Index, string Item, bool Raise);

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
    if (items is null || items.Count == 0)
    {
      return null;
    }

    int index = key switch
    {
      "ArrowRight" => Next(items.Count, IndexOf(items, value)),
      "ArrowLeft" => Previous(items.Count, IndexOf(items, value)),
      "Home" => Home(items.Count),
      "End" => End(items.Count),
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
