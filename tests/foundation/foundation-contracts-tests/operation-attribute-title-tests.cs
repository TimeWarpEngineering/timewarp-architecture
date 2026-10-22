#region Purpose
// OperationAttribute.Title is an optional human-readable name. Blank means no title.
#endregion

namespace OperationAttribute_;

using TimeWarp.Contracts;

public class Title_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Title_Given_>();

  public static Task A_Value_Should_Keep_The_Title_And_The_Name()
  {
    OperationAttribute attribute = new("rename_symbol") { Title = "Rename symbol" };

    attribute.Title.ShouldBe("Rename symbol");
    attribute.Name.ShouldBe("rename_symbol");
    return Task.CompletedTask;
  }

  public static Task Omitted_Should_Have_No_Title()
  {
    OperationAttribute attribute = new("multi_query");

    attribute.Title.ShouldBeNull();
    attribute.Name.ShouldBe("multi_query");
    return Task.CompletedTask;
  }

  public static Task Whitespace_Should_Have_No_Title()
  {
    OperationAttribute spaces = new("get_symbol") { Title = "  " };
    OperationAttribute empty = new("get_symbol") { Title = "" };
    OperationAttribute tabs = new("get_symbol") { Title = " \t " };

    spaces.Title.ShouldBeNull();
    empty.Title.ShouldBeNull();
    tabs.Title.ShouldBeNull();
    spaces.Name.ShouldBe("get_symbol");
    return Task.CompletedTask;
  }
}
