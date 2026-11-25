#region Purpose
// NavigationHistory keeps every visited address newest first, without RouteState's truncation (task 521).
#endregion

#region Design
// No services: a TestNavigationManager raises LocationChanged on NavigateTo, and the history is
// built over it directly. The cases are the ones the feedback page path relies on: the start
// address is recorded, a repeat visit keeps the pages between, a same-page navigation is not
// added twice, and the cap drops the oldest entry.
#endregion

namespace NavigationHistory_;

[TestTag("Unit")]
public class NavigationHistory_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<NavigationHistory_Should_>();

  public static Task Record_The_Start_Address()
  {
    TestNavigationManager navigation = new();
    using NavigationHistory history = new(navigation);
    history.Visits.ShouldBe(["http://localhost/"]);
    return Task.CompletedTask;
  }

  public static Task Keep_The_Pages_Between_Two_Visits_To_One_Page()
  {
    TestNavigationManager navigation = new();
    using NavigationHistory history = new(navigation);
    navigation.NavigateTo("/Counter");
    navigation.NavigateTo("/Feedback");
    navigation.NavigateTo("/Weather");
    navigation.NavigateTo("/Feedback");

    history.Visits.ShouldBe(
    [
      "http://localhost/Feedback",
      "http://localhost/Weather",
      "http://localhost/Feedback",
      "http://localhost/Counter",
      "http://localhost/",
    ]);
    return Task.CompletedTask;
  }

  public static Task Not_Add_A_Same_Page_Navigation_Twice()
  {
    TestNavigationManager navigation = new();
    using NavigationHistory history = new(navigation);
    navigation.NavigateTo("/Counter");
    navigation.NavigateTo("/Counter");
    history.Visits.ShouldBe(["http://localhost/Counter", "http://localhost/"]);
    return Task.CompletedTask;
  }

  public static Task Drop_The_Oldest_Past_The_Cap()
  {
    TestNavigationManager navigation = new();
    using NavigationHistory history = new(navigation);
    for (int page = 0; page < NavigationHistory.MaxVisits; page++)
    {
      navigation.NavigateTo($"/Page{page}");
    }

    history.Visits.Count().ShouldBe(NavigationHistory.MaxVisits);
    history.Visits.First().ShouldBe($"http://localhost/Page{NavigationHistory.MaxVisits - 1}");
    history.Visits.ShouldNotContain("http://localhost/");
    return Task.CompletedTask;
  }

  public static Task Stop_Recording_When_Disposed()
  {
    TestNavigationManager navigation = new();
    NavigationHistory history = new(navigation);
    history.Dispose();
    navigation.NavigateTo("/Counter");
    history.Visits.ShouldBe(["http://localhost/"]);
    return Task.CompletedTask;
  }
}
