#region Purpose
// Pin RovingSelection: next, previous, wrap, Home, End, an empty list, and the cancel script.
#endregion

#region Design
// The helper is pure, so these tests construct nothing. A missing selection is index -1, which
// is also what IndexOf returns for a null value. Left and Right step from the first item in
// that case, because that item is the roving tab stop. FromKey reports Raise only when the
// landing item differs from the current value. Every HandledKeys entry is a move, and the
// cancel script names those keys inside the unmodified-modifier guard.
#endregion

namespace RovingSelection_;

using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class RovingSelection_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RovingSelection_Should_>();

  public static Task Next_Moves_To_The_Following_Item()
  {
    RovingSelection.Next(3, 0).ShouldBe(1);
    RovingSelection.Next(4, -1).ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task Previous_Moves_To_The_Preceding_Item()
  {
    RovingSelection.Previous(3, 2).ShouldBe(1);
    RovingSelection.Previous(4, -1).ShouldBe(3);
    return Task.CompletedTask;
  }

  public static Task Next_Wraps_From_The_Last_Item()
  {
    RovingSelection.Next(3, 2).ShouldBe(0);
    RovingSelection.Next(1, 0).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Previous_Wraps_From_The_First_Item()
  {
    RovingSelection.Previous(3, 0).ShouldBe(2);
    RovingSelection.Previous(1, 0).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Home_Selects_The_First_Item()
  {
    RovingSelection.Home(3).ShouldBe(0);
    RovingSelection.FromKey(["End", "Home"], "End", "Home").ShouldBe(new RovingSelection.MoveResult(0, "End", false));
    return Task.CompletedTask;
  }

  public static Task End_Selects_The_Last_Item()
  {
    RovingSelection.End(3).ShouldBe(2);
    RovingSelection.FromKey(["End", "Home"], null, "End").ShouldBe(new RovingSelection.MoveResult(1, "Home", true));
    return Task.CompletedTask;
  }

  public static Task Empty_List_Has_No_Index()
  {
    RovingSelection.Next(0, -1).ShouldBe(-1);
    RovingSelection.Previous(0, 0).ShouldBe(-1);
    RovingSelection.Home(0).ShouldBe(-1);
    RovingSelection.End(0).ShouldBe(-1);
    RovingSelection.TabStop(0, -1).ShouldBe(-1);
    RovingSelection.FromKey([], null, "ArrowRight").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Tab_Stop_Is_The_Selection_Or_The_First_Item()
  {
    RovingSelection.TabStop(3, 1).ShouldBe(1);
    RovingSelection.TabStop(3, -1).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task FromKey_Does_Not_Raise_When_The_Item_Is_Already_Current()
  {
    RovingSelection.FromKey(["Only"], "Only", "ArrowRight").ShouldBe(new RovingSelection.MoveResult(0, "Only", false));
    RovingSelection.FromKey(["A", "B"], "A", "ArrowRight").ShouldBe(new RovingSelection.MoveResult(1, "B", true));
    RovingSelection.FromKey(["A", "B"], "A", "Enter").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task FromKey_Handles_Each_Cancelled_Key_And_The_Script_Guards_Modifiers()
  {
    string[] items = ["a", "b", "c"];
    foreach (string key in RovingSelection.HandledKeys)
    {
      RovingSelection.FromKey(items, "b", key).ShouldNotBeNull();
      RovingSelection.CancelScript.ShouldContain("event.key==='" + key + "'");
    }

    RovingSelection.CancelScript.ShouldContain("!event.altKey&&!event.ctrlKey&&!event.metaKey&&!event.shiftKey");
    RovingSelection.CancelScript.ShouldContain("preventDefault()");
    return Task.CompletedTask;
  }
}
