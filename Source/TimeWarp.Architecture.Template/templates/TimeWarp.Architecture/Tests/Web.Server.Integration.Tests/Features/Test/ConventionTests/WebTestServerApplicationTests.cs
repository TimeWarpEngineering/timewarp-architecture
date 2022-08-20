namespace WebTestServerApplication_;

using Dawn;
using FluentAssertions;
using TimeWarp.Architecture.Testing;
using TimeWarp.Fixie;

[TestTag("WebTestServerApplication")]
public class Should
{
  public Should
  (
    WebTestServerApplication aWebTestServerApplication
  )
  {
    Guard.Argument(aWebTestServerApplication).NotNull();
  }

  /// <summary>
  /// This will test that the injected WebTestServerApplication can be created and disposed.
  /// </summary>
  public void Start_Without_Exception() => true.Should().BeTrue();
}
