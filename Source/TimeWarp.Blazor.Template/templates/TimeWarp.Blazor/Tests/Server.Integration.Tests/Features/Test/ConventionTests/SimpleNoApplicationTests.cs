namespace ConventionTest_
{
  using FluentAssertions;
  using TimeWarp.Blazor.Testing;

  [TestTag("Fast")]
  public class SimpleNoApplicationTest_Should_
  {
    public void AlwaysPass() => true.Should().BeTrue();

    [Skip("Demonstrates skip attribute")]
    public void AlwaysFail() => true.Should().BeFalse();
  }
}
