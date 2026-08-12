#region Purpose
// LoginPage.GetSafeReturnUrl open-redirect and redirect-loop guards (task 153).
#endregion

#region Design
// Pure-function tests — no SPA host fixture needed. The sanitizer is the security seam of the
// login redirect flow: anything it lets through becomes a post-sign-in NavigateTo target, so
// absolute URLs, protocol-relative URLs, backslash tricks, and /Login self-loops must all
// collapse to "/".
#endregion

namespace LoginPage_;

using TimeWarp.Architecture.Features.Account;

[TestTag("Unit")]
public class GetSafeReturnUrl_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetSafeReturnUrl_Should>();

  public static Task Pass_through_local_paths()
  {
    LoginPage.GetSafeReturnUrl("/Settings").ShouldBe("/Settings");
    LoginPage.GetSafeReturnUrl("/Users/Current/Profile").ShouldBe("/Users/Current/Profile");
    LoginPage.GetSafeReturnUrl("/Counter?start=5").ShouldBe("/Counter?start=5");
    return Task.CompletedTask;
  }

  public static Task Fall_back_to_home_when_missing()
  {
    LoginPage.GetSafeReturnUrl(null).ShouldBe("/");
    LoginPage.GetSafeReturnUrl(string.Empty).ShouldBe("/");
    return Task.CompletedTask;
  }

  public static Task Reject_absolute_and_protocol_relative_urls()
  {
    // Open-redirect guard: nothing that can leave the origin may pass.
    LoginPage.GetSafeReturnUrl("https://evil.example/phish").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("http://evil.example").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("//evil.example/phish").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("/\\evil.example").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("javascript:alert(1)").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("Settings").ShouldBe("/");
    return Task.CompletedTask;
  }

  public static Task Reject_login_page_itself()
  {
    // Redirect-loop guard — but only the exact /Login path, not pages that merely start with it.
    LoginPage.GetSafeReturnUrl("/Login").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("/login").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("/Login/").ShouldBe("/");
    LoginPage.GetSafeReturnUrl("/Login?returnUrl=%2F").ShouldBe("/");
    return Task.CompletedTask;
  }
}
