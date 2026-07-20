namespace EntityVersion_;

public class Next
{
  public void Increments_from_zero() => EntityVersion.Next(0).ShouldBe(1);

  public void Increments_from_nonzero() => EntityVersion.Next(41).ShouldBe(42);

  public void Never_returns_the_original_value()
  {
    long original = 7;
    EntityVersion.Next(original).ShouldNotBe(original);
  }
}
