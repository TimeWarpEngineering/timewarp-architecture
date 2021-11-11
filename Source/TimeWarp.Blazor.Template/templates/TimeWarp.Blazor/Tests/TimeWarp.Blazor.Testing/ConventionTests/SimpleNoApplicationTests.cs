namespace ConventionTest_;

using FluentAssertions;
using TimeWarp.Blazor.Testing;

[TestTag(TestTags.Fast)]
public class SimpleNoApplicationTest_Should_
{
  public void AlwaysPass() => true.Should().BeTrue();

  [Skip("Demonstrates skip attribute")]
  public void AlwaysFail() => true.Should().BeFalse();

  [TestTag(TestTags.Fast)]
  public void TagExample() => true.Should().BeTrue();

  [Input(5, 3, 2)]
  [Input(8, 5, 3)]
  public void Subtract(int x, int y, int expectedDifference)
  {
    int result = x - y;
    result.Should().Be(expectedDifference);
  }
}

