namespace TimeWarp.Blazor.Configuration
{
  using System;

  public class SampleEnvironmentCheck
  {
    public static string Description => "Sample Environment check";

    public void Check()
    {
      ;
      Console.WriteLine("All good!");
    }
  }
}
