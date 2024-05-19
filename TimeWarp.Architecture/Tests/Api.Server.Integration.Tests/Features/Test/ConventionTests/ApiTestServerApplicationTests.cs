namespace ApiTestServerApplication_;

using Ardalis.GuardClauses;

[TestTag("ApiTestServerApplication")]
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

  [Skip("This test runs forever to allow me to manually test if servers are running properly.  Normally needs to be skipped as it will never completed")]
  public async Task RunForever()
  {
    await Task.Delay(int.MaxValue);
    Console.WriteLine("Wlll never get here");
  }
}
