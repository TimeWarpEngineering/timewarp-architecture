#region Purpose
// FeedbackPagePath picks the page a filing records: the current page, or the newest page visited
// before the feedback pages, path only (task 521).
#endregion

#region Design
// Pure function, so no renderer or services. The visited list is newest first, as
// RouteState.Routes enumerates it. Compiles only with the postgres flag, like the feedback slice
// (template.json lists this file).
#endregion

namespace FeedbackPagePath_;

using TimeWarp.Architecture.Features.Feedback;

[TestTag("Unit")]
public class FeedbackPagePath_Should_
{
  private const string Base = "https://app.example/";
  private static readonly Uri BaseUri = new(Base);

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackPagePath_Should_>();

  public static Task Use_The_Current_Page_Outside_Feedback()
  {
    FeedbackPagePath.From(BaseUri, new Uri(Base + "Counter"), [Base + "Counter", Base]).ShouldBe("/Counter");
    return Task.CompletedTask;
  }

  public static Task Use_The_Page_Before_The_Feedback_Pages()
  {
    const string itemPage = Base + "Feedback/11111111-1111-1111-1111-111111111111";
    FeedbackPagePath.From(BaseUri, new Uri(Base + "Feedback"), [Base + "Feedback", itemPage, Base + "Weather?city=x", Base])
      .ShouldBe("/Weather");
    return Task.CompletedTask;
  }

  public static Task Drop_The_Query_And_Fragment()
  {
    FeedbackPagePath.From(BaseUri, new Uri(Base + "Profile?token=secret#top"), []).ShouldBe("/Profile");
    FeedbackPagePath.From(BaseUri, new Uri(Base + "?code=abc"), []).ShouldBe("/");
    return Task.CompletedTask;
  }

  public static Task Return_Null_When_Only_Feedback_Was_Visited()
  {
    FeedbackPagePath.From(BaseUri, new Uri(Base + "feedback"), [Base + "feedback"]).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Keep_The_Path_Under_A_Path_Base()
  {
    Uri hosted = new("https://app.example/tenant/");
    FeedbackPagePath.From(hosted, new Uri("https://app.example/tenant/Counter?x=1"), []).ShouldBe("/Counter");
    return Task.CompletedTask;
  }

  public static Task Return_Null_For_A_Path_The_Server_Would_Refuse()
  {
    FeedbackPagePath.From(BaseUri, new Uri(Base + new string('a', SubmitFeedback.MaxPagePathLength)), []).ShouldBeNull();
    FeedbackPagePath.From(BaseUri, new Uri("https://other.example/Counter"), []).ShouldBeNull();
    return Task.CompletedTask;
  }
}
