#region Purpose
// OperationAttribute.Group is optional purpose metadata. Blank means ungrouped.
#endregion

namespace OperationAttribute_;

using TimeWarp.Contracts;

public class Group_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Group_Given_>();

  public static Task A_Value_Should_Keep_The_Group_And_The_Name()
  {
    OperationAttribute attribute = new("get_symbol") { Group = "symbols" };

    attribute.Group.ShouldBe("symbols");
    attribute.Name.ShouldBe("get_symbol");
    return Task.CompletedTask;
  }

  public static Task Dotted_Or_Spaced_Segments_Should_Stay_As_Written()
  {
    OperationAttribute dotted = new("rename_symbol") { Group = "edit.symbol" };
    OperationAttribute spaced = new("rename_parameter") { Group = "edit symbol" };

    dotted.Group.ShouldBe("edit.symbol");
    dotted.Name.ShouldBe("rename_symbol");
    spaced.Group.ShouldBe("edit symbol");
    spaced.Name.ShouldBe("rename_parameter");
    return Task.CompletedTask;
  }

  public static Task Omitted_Should_Be_Ungrouped()
  {
    OperationAttribute attribute = new("multi_query");

    attribute.Group.ShouldBeNull();
    attribute.Name.ShouldBe("multi_query");
    return Task.CompletedTask;
  }

  public static Task Whitespace_Should_Be_Ungrouped()
  {
    OperationAttribute spaces = new("get_symbol") { Group = "  " };
    OperationAttribute empty = new("get_symbol") { Group = "" };
    OperationAttribute tabs = new("get_symbol") { Group = " \t " };

    spaces.Group.ShouldBeNull();
    empty.Group.ShouldBeNull();
    tabs.Group.ShouldBeNull();
    spaces.Name.ShouldBe("get_symbol");
    return Task.CompletedTask;
  }
}
