#region Purpose
// Pin AvatarInitials: two words, three words, one word, case, whitespace, empty, a surrogate, and an email.
#endregion

#region Design
// The helper is pure, so these tests construct nothing. Each method is one rule from the
// initials contract. "𠮷野" must come back as the whole first code point, not a split surrogate.
// "a@b.com" is one word. Null, empty, and whitespace are the same empty result.
#endregion

namespace AvatarInitials_;

using System.Globalization;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class AvatarInitials_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AvatarInitials_Should_>();

  public static Task Two_Words_Take_The_First_Letter_Of_Each()
  {
    AvatarInitials.From("Ada Lovelace").ShouldBe("AL");
    return Task.CompletedTask;
  }

  public static Task Three_Words_Take_The_First_And_The_Last()
  {
    AvatarInitials.From("Grace Murray Hopper").ShouldBe("GH");
    return Task.CompletedTask;
  }

  public static Task A_Single_Word_Gives_One_Letter()
  {
    AvatarInitials.From("Ada").ShouldBe("A");
    return Task.CompletedTask;
  }

  public static Task Lowercase_Input_Is_Uppercased()
  {
    AvatarInitials.From("ada lovelace").ShouldBe("AL");
    return Task.CompletedTask;
  }

  public static Task Extra_Spaces_And_Tabs_Are_Ignored()
  {
    AvatarInitials.From("  ada \t lovelace  ").ShouldBe("AL");
    return Task.CompletedTask;
  }

  public static Task Null_Empty_And_Whitespace_Give_An_Empty_String()
  {
    AvatarInitials.From(null).ShouldBe("");
    AvatarInitials.From("").ShouldBe("");
    AvatarInitials.From("   ").ShouldBe("");
    AvatarInitials.From("\t\n").ShouldBe("");
    return Task.CompletedTask;
  }

  public static Task A_Surrogate_Pair_Stays_Whole()
  {
    string initials = AvatarInitials.From("𠮷野");
    initials.ShouldBe("𠮷");
    new StringInfo(initials).LengthInTextElements.ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task An_Email_Shaped_Name_Gives_Its_First_Letter()
  {
    AvatarInitials.From("a@b.com").ShouldBe("A");
    return Task.CompletedTask;
  }
}
