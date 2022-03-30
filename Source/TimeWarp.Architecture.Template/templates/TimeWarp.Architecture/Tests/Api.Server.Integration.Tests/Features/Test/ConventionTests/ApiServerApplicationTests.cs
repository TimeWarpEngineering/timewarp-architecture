namespace TimeWarpBlazorServerApplication_;

using Dawn;
using FluentAssertions;
using TimeWarp.Architecture.Testing;

[TestTag("ApiServerApplication")]
public class Should
{
  public Should
  (
    ApiServerApplication aApiServerApplication
  )
  {
    Guard.Argument(aApiServerApplication).NotNull();
  }

  public void Start_Without_Exception() => true.Should().BeTrue();
}
