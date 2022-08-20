namespace ApiTestServerApplication_;

using Dawn;
using FluentAssertions;
using TimeWarp.Architecture.Testing;
using TimeWarp.Fixie;

[TestTag("ApiTestServerApplication")]
public class Should
{
  public Should
  (
    ApiTestServerApplication aApiTestServerApplication
  )
  {
    Guard.Argument(aApiTestServerApplication).NotNull();
  }

  public void Start_Without_Exception() => true.Should().BeTrue();
}
