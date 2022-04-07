namespace WebServerApplication_;

using Dawn;
using FluentAssertions;
using TimeWarp.Architecture.Testing;

[TestTag("WebServerApplication")]
public class Should
{
  public Should
  (
    WebServerApplication aWebServerApplication
  )
  {
    Guard.Argument(aWebServerApplication).NotNull();
  }

  /// <summary>
  /// This will test that the injected WebServerApplication can be created and disposed.
  /// </summary>
  public void Start_Without_Exception() => true.Should().BeTrue();
}
