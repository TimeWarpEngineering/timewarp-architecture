namespace ApiTestServerApplication_;

using Ardalis.GuardClauses;
using System.Diagnostics.CodeAnalysis;

[TestTag("ApiTestServerApplication")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class Should
{
  public Should
  (
    ApiTestServerApplication apiTestServerApplication
  )
  {
    Guard.Against.Null(apiTestServerApplication);
  }

  public void Start_Without_Exception() => true.Should().BeTrue();

  [Skip("This test runs forever to allow me to manually test if servers are running properly.  Normally needs to be skipped as it will never complete")]
  public async Task RunForever()
  {
    await Task.Delay(int.MaxValue);
    throw new Exception("Will never get here");
  }
}
