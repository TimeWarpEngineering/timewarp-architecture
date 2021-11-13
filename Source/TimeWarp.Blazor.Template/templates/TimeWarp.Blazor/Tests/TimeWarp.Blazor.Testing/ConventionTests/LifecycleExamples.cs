namespace ConventionTest_
{
  using FluentAssertions;
  using System;
  using TimeWarp.Blazor.Testing;

  public class LifecycleExamples
  {
    public void AlwaysPass() => true.Should().BeTrue();

    [Input(5, 3, 2)]
    [Input(8, 5, 3)]
    public void Subtract(int aX, int aY, int aExpectedDifference)
    {
      // Will run lifecycles around each Input
      int result = aX - aY;
      result.Should().Be(aExpectedDifference);
    }

    public void Setup() => Console.WriteLine("Sample Setup");
    public void Cleanup() => Console.WriteLine("Sample Cleanup");
  }
}
