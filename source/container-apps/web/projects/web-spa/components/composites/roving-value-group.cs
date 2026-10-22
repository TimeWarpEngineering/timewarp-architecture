#region Purpose
// Shared value, focus, and keyboard behavior for Tabs and SegmentedControl.
#endregion

#region Design
// The caller owns Value. This base never assigns it and never navigates. Choosing the current
// item does not raise ValueChanged. OptionText changes the label only. A null value selects
// nothing; the first item keeps the roving tab stop. Left and Right wrap, Home and End jump,
// and a move raises ValueChanged (automatic activation). RovingSelection owns the index math.
// Focus follows that item on the next render: ElementReference.FocusAsync is presentational
// DOM state, the same kind of call the command palette uses. Blazor picks preventDefault when
// the frame renders, before it knows the key, and a button cannot carry both a delegate and a
// script on onkeydown. Each root binds RovingSelection.CancelScript, which cancels ArrowLeft,
// ArrowRight, Home, and End only when Alt, Ctrl, Meta, and Shift are all up. Those are the
// same four flags OnKeyDownAsync ignores, so Alt+Left (browser Back) and Ctrl+Home (page
// scroll) still reach the browser. Tab still leaves the group. Derived markup owns the root
// class and the ARIA roles.
#endregion

namespace TimeWarp.Architecture.Components;

public abstract class RovingValueGroup : ComponentBase
{
  [Parameter, EditorRequired] public IReadOnlyList<string> Items { get; set; } = [];
  [Parameter] public Func<string, string>? OptionText { get; set; }
  [Parameter] public string? Value { get; set; }
  [Parameter] public EventCallback<string> ValueChanged { get; set; }
  [Parameter] public string? AriaLabel { get; set; }
  [Parameter] public string? Class { get; set; }
  [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

  private ElementReference[] ItemReferenceSlots = [];
  private int? PendingFocus;
  private int TabStopSlot;

  protected ElementReference[] ItemReferences => ItemReferenceSlots;
  protected int TabStopIndex => TabStopSlot;
  protected IReadOnlyList<string> Rows => Items ?? [];

  protected override void OnParametersSet()
  {
    IReadOnlyList<string> rows = Rows;
    if (ItemReferenceSlots.Length != rows.Count)
    {
      ItemReferenceSlots = new ElementReference[rows.Count];
    }

    TabStopSlot = RovingSelection.TabStop(rows.Count, RovingSelection.IndexOf(rows, Value));
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
    if (PendingFocus is not int slot)
    {
      return;
    }

    PendingFocus = null;
    if ((uint)slot >= (uint)ItemReferenceSlots.Length)
    {
      return;
    }

    await ItemReferenceSlots[slot].FocusAsync();
  }

  protected string Text(string item) => OptionText is null ? item : OptionText(item);

  protected Task ChooseAsync(string item)
  {
    if (string.Equals(item, Value, StringComparison.Ordinal))
    {
      return Task.CompletedTask;
    }

    return ValueChanged.InvokeAsync(item);
  }

  protected async Task OnKeyDownAsync(KeyboardEventArgs keyboard)
  {
    if (keyboard.AltKey || keyboard.CtrlKey || keyboard.MetaKey || keyboard.ShiftKey)
    {
      return;
    }

    if (RovingSelection.FromKey(Rows, Value, keyboard.Key) is not RovingSelection.MoveResult move)
    {
      return;
    }

    PendingFocus = move.Index;
    if (move.Raise)
    {
      await ValueChanged.InvokeAsync(move.Item);
    }
  }
}
