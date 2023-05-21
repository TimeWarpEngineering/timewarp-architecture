namespace WebTestServerApplication_;

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

  [Skip("This test runs forever to allow me to manually test if servers are running properly.  Normally needs to be skipped as it will never completed")]
  public async Task RunForever()
  {
    await Task.Delay(int.MaxValue);
    Console.WriteLine("done");
  }
}
