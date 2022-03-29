namespace TimeWarpBlazorServerApplication_;

using Dawn;
using FluentAssertions;
using TimeWarp.Architecture.Testing;

[TestTag("WebServerApplication")]
public class Should
{
  public Should
  (
    WebServerApplication aTimeWarpBlazorServerApplication
  )
  {
    Guard.Argument(aTimeWarpBlazorServerApplication).NotNull();
  }

  public void Start_Without_Exception() => true.Should().BeTrue();
}
