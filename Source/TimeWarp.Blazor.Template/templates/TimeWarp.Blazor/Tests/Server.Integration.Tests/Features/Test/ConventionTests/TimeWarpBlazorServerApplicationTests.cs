namespace TimeWarpBlazorServerApplication_
{
  using Dawn;
  using FluentAssertions;
  using TimeWarp.Architecture.Testing;

  [TestTag("TimeWarpBlazorServerApplication")]
  public class Should
  {
    public Should
    (
      TimeWarpBlazorServerApplication aTimeWarpBlazorServerApplication
    )
    {
      Guard.Argument(aTimeWarpBlazorServerApplication).NotNull();
    }

    public void Start_Without_Exception() => true.Should().BeTrue();
  }
}
